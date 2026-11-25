using System.Globalization;
using System.Text;
using HistoryGenerator.Model;
using HistoryGenerator.Scenarios;

namespace HistoryGenerator.Validation;

public sealed record Measurement(string Sha, string Target, int Attempt, string RunId, string Project, string Test, string Outcome, double DurationMs);

/// <summary>
/// Compares measured durations (durations.csv rows from CI or a local replay) with the duration model, including the
/// seeded draws each run makes. Because realized expectations are exact per run, residuals ln(measured/expected)
/// should be flat; the validator flags outliers and shifts in residuals (unmodelled behaviour) per series.
/// </summary>
public static class Validator
{
    private const double AbsTolMs = 2.5;
    private const double CpuRelTol = 0.30;

    public static int Run(IReadOnlyList<string> shas, IEnumerable<string> csvFiles, IReadOnlyCollection<Target>? onlyTargets, string? reportPath)
    {
        var index = shas.Select((s, i) => (s, i + 1)).ToDictionary(x => x.s, x => x.Item2, StringComparer.OrdinalIgnoreCase);
        var rows = csvFiles.SelectMany(ReadCsv).ToList();
        var report = new StringBuilder();
        var failures = 0;
        var warnings = 0;

        void Line(string text) { report.AppendLine(text); Console.WriteLine(text); }

        var unknown = rows.Where(r => !index.ContainsKey(r.Sha)).Select(r => r.Sha).Distinct().ToList();
        if (unknown.Count > 0) Line($"NOTE: {unknown.Count} commit(s) in the measurements are not part of the generated history (ignored): {string.Join(", ", unknown.Take(5))}");
        rows = rows.Where(r => index.ContainsKey(r.Sha)).ToList();
        var local = rows.Count(r => string.Equals(r.RunId, "local", StringComparison.Ordinal));
        if (local > 0)
        {
            // Plain local runs are not seeded (no GITHUB_SHA), so their draws and machine factor cannot be reproduced.
            Line($"NOTE: {local} row(s) from plain local runs (run_id=local) ignored; use tools/replay-history.sh instead.");
            rows = rows.Where(r => !string.Equals(r.RunId, "local", StringComparison.Ordinal)).ToList();
        }
        if (onlyTargets is not null) rows = rows.Where(r => onlyTargets.Contains(Targets.Parse(r.Target))).ToList();

        var tests = Catalog.All.SelectMany(s => s.AllTests().Select(t => (Scenario: s, Test: t))).ToDictionary(x => x.Test.TrxName, x => x);

        // 1. Structure: every run has exactly the modelled tests, all passed (or skipped when modelled as skipped).
        var runs = rows.GroupBy(r => (r.Sha, r.Target, r.Attempt, r.RunId)).ToList();
        var coverage = runs.GroupBy(r => r.Key.Target).ToDictionary(g => g.Key, g => g.Select(r => index[r.Key.Sha]).Distinct().Count());
        Line($"Runs: {runs.Count}; commits covered per target: {string.Join(", ", coverage.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}"))}");
        foreach (var target in coverage.Keys)
        {
            var missing = Enumerable.Range(1, History.Commits).Except(runs.Where(r => r.Key.Target == target).Select(r => index[r.Key.Sha])).ToList();
            if (missing.Count > 0) { Line($"WARN {target}: no measurements for commits {Ranges(missing)}"); warnings++; }
        }
        foreach (var run in runs)
        {
            var c = index[run.Key.Sha];
            var target = Targets.Parse(run.Key.Target);
            var expected = Catalog.All.SelectMany(s => s.Tests(c)).Select(t => t.TrxName).ToHashSet(StringComparer.Ordinal);
            var actual = run.Select(r => r.Test).ToHashSet(StringComparer.Ordinal);
            foreach (var name in expected.Except(actual)) { Line($"FAIL structure: commit {c} {target.Id()} run {run.Key.RunId}: missing test {name}"); failures++; }
            foreach (var name in actual.Except(expected)) { Line($"FAIL structure: commit {c} {target.Id()} run {run.Key.RunId}: unexpected test {name}"); failures++; }
            foreach (var r in run.Where(r => tests.ContainsKey(r.Test)))
            {
                var (scenario, test) = tests[r.Test];
                var dist = scenario.Expected(test, target, c);
                var expectedOutcome = dist is Dist.Skipped ? "NotExecuted" : "Passed";
                if (!string.Equals(r.Outcome, expectedOutcome, StringComparison.OrdinalIgnoreCase))
                { Line($"FAIL outcome: commit {c} {target.Id()} {r.Test}: {r.Outcome} (expected {expectedOutcome})"); failures++; }
            }
        }

        // 2. Noise per target from residuals of modelled tests with expected >= 20 ms.
        var residuals = new List<(Scenario Scenario, TestRef Test, Target Target, int Attempt, string RunId, int Commit, double Measured, double Expected, Dist Dist)>();
        foreach (var r in rows.Where(r => tests.ContainsKey(r.Test)))
        {
            var (scenario, test) = tests[r.Test];
            var target = Targets.Parse(r.Target);
            var c = index[r.Sha];
            var dist = scenario.Expected(test, target, c);
            var expected = Seeds.Realize(dist, shas[c - 1], target, r.Attempt);
            residuals.Add((scenario, test, target, r.Attempt, r.RunId, c, r.DurationMs, expected ?? double.NaN, dist));
        }
        var relTol = new Dictionary<Target, double>();
        foreach (var g in residuals.Where(x => !double.IsNaN(x.Expected) && x.Expected >= 20).GroupBy(x => x.Target))
        {
            var rs = g.Select(x => Math.Log(x.Measured / x.Expected)).ToList();
            var med = Median(rs);
            var mad = 1.4826 * Median(rs.Select(v => Math.Abs(v - med)).ToList());
            relTol[g.Key] = Math.Clamp(4 * mad, 0.08, 0.30);
            Line($"Noise {g.Key.Id()}: median residual {Pct(med)}, robust sigma {Pct(mad)}, tolerance {Pct(relTol[g.Key])} or {Fmt.N(AbsTolMs)} ms");
        }

        // 3. Per series checks.
        var results = new List<(string Scenario, string Target, string Status, string Detail)>();
        var overheads = new List<(string Series, double Overhead)>();
        foreach (var series in residuals.GroupBy(x => (x.Scenario.Id, x.Test.TrxName, x.Target, x.Attempt, x.RunId.StartsWith("replay", StringComparison.Ordinal) ? "replay" : "ci")))
        {
            var points = series.OrderBy(x => x.Commit).ToList();
            var target = series.Key.Target;
            var tol = relTol.GetValueOrDefault(target, 0.15);
            var status = "PASS";
            var details = new List<string>();

            var modelled = points.Where(p => !double.IsNaN(p.Expected) && p.Dist is not Dist.Skipped).ToList();
            var cpu = points.Where(p => p.Dist is Dist.CpuBound).ToList();
            var micro = points.Where(p => p.Dist is Dist.Micro).ToList();

            if (modelled.Count > 0)
            {
                var overhead = Check(modelled.Select(p => (p.Commit, p.Measured, p.Expected, p.Dist)).ToList(), tol, AbsTolMs, ref status, details);
                if (overhead > 0.5) overheads.Add(($"{series.Key.TrxName} [{target.Id()}/a{series.Key.Attempt}]", overhead));
                CheckDeclaredChanges(points[0].Scenario, points[0].Test, target,
                    modelled.Select(p => (p.Commit, p.Measured - overhead, p.Expected)).ToList(), tol, ref status, details);
            }
            if (cpu.Count > 0)
            {
                var k = Median(cpu.Select(p => p.Measured / ((Dist.CpuBound)p.Dist).Units).ToList());
                Check(cpu.Select(p => (p.Commit, p.Measured, k * ((Dist.CpuBound)p.Dist).Units, p.Dist)).ToList(), CpuRelTol, 3, ref status, details, allowOverhead: false);
                var segments = cpu.GroupBy(p => ((Dist.CpuBound)p.Dist).Units).OrderBy(g => g.Key).ToList();
                if (segments.Count > 1)
                {
                    var ratio = Median(segments[^1].Select(p => p.Measured).ToList()) / Median(segments[0].Select(p => p.Measured).ToList());
                    var model = segments[^1].Key / segments[0].Key;
                    details.Add($"cpu ratio {ratio:0.00} (model {model:0.00})");
                    if (Math.Abs(Math.Log(ratio / model)) > 0.2) { status = Worst(status, "FAIL"); details.Add("cpu ratio off"); }
                }
            }
            if (micro.Count > 0)
            {
                var p95 = Quantile(micro.Select(p => p.Measured).ToList(), 0.95);
                if (p95 > 5) { status = Worst(status, "WARN"); details.Add($"micro p95 {p95:0.00} ms"); }
            }
            results.Add((series.Key.Id, $"{target.Id()}/a{series.Key.Attempt}/{series.Key.Item5}", status, $"{series.Key.TrxName}: {string.Join("; ", details)}"));
        }

        Line("");
        Line("Per scenario (worst series per target):");
        foreach (var g in results.GroupBy(r => (r.Scenario, r.Target)).OrderBy(g => g.Key.Scenario).ThenBy(g => g.Key.Target))
        {
            var worst = g.Select(r => r.Status).Aggregate("PASS", Worst);
            if (worst == "FAIL") failures++;
            if (worst == "WARN") warnings++;
            var detail = worst == "PASS" ? "" : " — " + string.Join(" | ", g.Where(r => r.Status == worst).Select(r => r.Detail).Take(3));
            Line($"{worst,-4} {g.Key.Scenario,-26} {g.Key.Target,-22}{detail}");
        }
        Line("");
        Line($"Constant overhead removed (> 0.5 ms; {overheads.Count} series):");
        foreach (var (name, value) in overheads.OrderByDescending(o => o.Overhead).Take(40))
            Line(string.Create(CultureInfo.InvariantCulture, $"  +{value:0.00} ms  {name}"));
        Line("");
        Line($"Summary: {failures} failure(s), {warnings} warning(s), {results.Count} series checked.");
        if (reportPath is not null) File.WriteAllText(reportPath, report.ToString());
        return failures == 0 ? 0 : 1;
    }

    /// <summary>Per-point and shift checks. Returns the constant overhead that was removed.</summary>
    private static double Check(List<(int Commit, double Measured, double Expected, Dist Dist)> input, double relTol, double absTol, ref string status, List<string> details, bool allowOverhead = true)
    {
        // A constant per-test overhead (JIT, first-call costs inside the timed region) is not a change. It is only
        // removed when it is consistent across the model segments and small relative to the smallest modelled value.
        var overhead = 0.0;
        if (allowOverhead)
        {
            var perSegment = input.GroupBy(p => p.Dist).Where(g => g.Count() >= 3)
                .Select(g => Median(g.Select(p => p.Measured - p.Expected).ToList())).ToList();
            if (perSegment.Count > 0)
            {
                var minExpected = input.Min(p => p.Expected);
                var cap = Math.Min(10, Math.Max(0.5, relTol * minExpected));
                if (perSegment.Max() - perSegment.Min() > Math.Max(absTol, relTol * minExpected))
                    details.Add($"inconsistent overhead across segments ({perSegment.Min():0.0}..{perSegment.Max():0.0} ms)");
                else
                    overhead = Math.Clamp(Median(input.Select(p => p.Measured - p.Expected).ToList()), 0, cap);
                if (overhead > absTol) { details.Add($"constant overhead +{overhead:0.0} ms"); status = Worst(status, "WARN"); }
            }
        }
        var points = input.Select(p => (p.Commit, Measured: Math.Max(p.Measured - overhead, 0.05), p.Expected)).ToList();
        bool Within(double m, double e) => Math.Abs(Math.Log(m / e)) <= relTol || Math.Abs(m - e) <= absTol;
        var outliers = points.Where(p => !Within(p.Measured, p.Expected)).ToList();
        if (outliers.Count > 0)
        {
            var worst = outliers.MaxBy(p => Math.Abs(Math.Log(p.Measured / p.Expected)));
            details.Add($"{outliers.Count}/{points.Count} outside tolerance (worst @{worst.Commit}: {worst.Measured:0.0} vs {worst.Expected:0.0} ms)");
            var limit = Math.Max(2, (int)Math.Ceiling(points.Count * 0.05));
            status = Worst(status, outliers.Count > limit ? "FAIL" : "WARN");
        }

        const int w = 6;
        if (points.Count < 2 * w) return overhead;
        var r = points.Select(p => Math.Log(p.Measured / p.Expected)).ToList();
        double worstShift = 0;
        var worstAt = 0;
        double worstThreshold = 0;
        for (var k = w; k <= r.Count - w; k++)
        {
            var d = Median(r.GetRange(k, w)) - Median(r.GetRange(k - w, w));
            var e = Median(points.GetRange(k - w, 2 * w).Select(p => p.Expected).ToList());
            var threshold = Math.Max(relTol * 0.75, Math.Log(1 + 2 * absTol / e));
            if (Math.Abs(d) / threshold > (worstThreshold == 0 ? 0 : Math.Abs(worstShift) / worstThreshold))
            {
                worstShift = d;
                worstAt = points[k].Commit;
                worstThreshold = threshold;
            }
        }
        if (worstThreshold > 0 && Math.Abs(worstShift) > worstThreshold)
        {
            details.Add($"unmodelled shift {Pct(worstShift)} at commit {worstAt}");
            status = Worst(status, Math.Abs(worstShift) > 2 * worstThreshold ? "FAIL" : "WARN");
        }
        return overhead;
    }

    /// <summary>
    /// For every declared change of the scenario, compares the measured change between the model segment before it and
    /// the one after it with the modelled change, so that a step missing from the code (or too small/large) fails.
    /// </summary>
    private static void CheckDeclaredChanges(Scenario scenario, TestRef test, Target target,
        List<(int Commit, double Measured, double Expected)> points, double relTol, ref string status, List<string> details)
    {
        var changePoints = Enumerable.Range(2, History.Commits - 1)
            .Where(c => scenario.Expected(test, target, c) != scenario.Expected(test, target, c - 1))
            .ToList();
        var checks = new List<(int From, int To, string Tier, string Label)>();
        foreach (var change in scenario.Changes.Where(ch => ch.IsPrimary && ch.Kind != Kinds.Decoy && !Kinds.IsLifecycle(ch.Kind)))
        {
            if (!changePoints.Contains(change.Commit)) continue;
            var tier = scenario.TierFor(change, test, target) ?? change.Tier;
            checks.Add((change.Commit, change.Commit, tier, $"{change.Kind}@{change.Commit}"));
        }
        if (scenario.Ramp is { } ramp && changePoints.Any(c => c >= ramp.Start && c <= ramp.End))
            checks.Add((ramp.Start, ramp.End, scenario.Changes.First(ch => ch.IsRampStep).Tier, $"ramp@{ramp.Start}-{ramp.End}"));

        foreach (var (from, to, tier, label) in checks)
        {
            // The model segment right before/after the change; segments without any observation (a skipped test)
            // are stepped over, segments with only one or two observations make the change "not checkable".
            var bounds = changePoints.Prepend(1).Append(History.Commits + 1).Distinct().OrderBy(c => c).ToList();
            var before = new List<(int Commit, double Measured, double Expected)>();
            for (var i = bounds.FindLastIndex(b => b < from); i >= 0; i--)
            {
                var end = Math.Min(bounds[i + 1], from);
                before = points.Where(p => p.Commit >= bounds[i] && p.Commit < end).ToList();
                if (before.Count > 0) break;
            }
            var after = new List<(int Commit, double Measured, double Expected)>();
            for (var i = bounds.FindIndex(b => b >= to); i >= 0 && i < bounds.Count - 1; i++)
            {
                after = points.Where(p => p.Commit >= Math.Max(bounds[i], to) && p.Commit < bounds[i + 1]).ToList();
                if (after.Count > 0) break;
            }
            if (before.Count < 3 || after.Count < 3) continue; // not checkable (e.g. a one-commit hotfix)

            var realized = Median(after.Select(p => Math.Log(p.Measured)).ToList()) - Median(before.Select(p => Math.Log(p.Measured)).ToList());
            var modelled = Median(after.Select(p => Math.Log(p.Expected)).ToList()) - Median(before.Select(p => Math.Log(p.Expected)).ToList());
            double Mad(List<(int Commit, double Measured, double Expected)> seg)
            {
                var r = seg.Select(p => Math.Log(p.Measured / p.Expected)).ToList();
                var m = Median(r);
                return 1.4826 * Median(r.Select(v => Math.Abs(v - m)).ToList());
            }
            var se = 1.2533 * Math.Sqrt(Math.Pow(Mad(before), 2) / before.Count + Math.Pow(Mad(after), 2) / after.Count);
            var smallest = Math.Min(before.Min(p => p.Expected), after.Min(p => p.Expected));
            var floor = Math.Max(relTol / 2, Math.Log(1 + 2 * AbsTolMs / smallest));
            var realizedMs = Median(after.Select(p => p.Measured).ToList()) - Median(before.Select(p => p.Measured).ToList());
            var modelledMs = Median(after.Select(p => p.Expected).ToList()) - Median(before.Select(p => p.Expected).ToList());
            var absoluteMatch = Math.Abs(realizedMs - modelledMs) <= Math.Max(2 * AbsTolMs, relTol * Math.Abs(modelledMs));
            if (Math.Abs(realized - modelled) > Math.Max(4 * se, floor) && !absoluteMatch)
            {
                details.Add($"{label}: measured {Pct(realized)} vs model {Pct(modelled)}");
                status = Worst(status, tier == Tiers.Optional ? "WARN" : "FAIL");
            }
            else if (Math.Abs(modelled) > Math.Max(4 * se, floor) && (Math.Abs(realized) < Math.Abs(modelled) / 2 || Math.Sign(realized) != Math.Sign(modelled)))
            {
                details.Add($"{label}: change missing (measured {Pct(realized)}, model {Pct(modelled)})");
                status = Worst(status, tier == Tiers.Optional ? "WARN" : "FAIL");
            }
        }
    }

    private static string Worst(string a, string b)
    {
        static int Rank(string s) => s switch { "FAIL" => 2, "WARN" => 1, _ => 0 };
        return Rank(a) >= Rank(b) ? a : b;
    }

    private static double Median(List<double> values) => Quantile(values, 0.5);

    private static double Quantile(List<double> values, double q)
    {
        if (values.Count == 0) return double.NaN;
        var sorted = values.OrderBy(v => v).ToList();
        var pos = q * (sorted.Count - 1);
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (pos - lo);
    }

    private static string Pct(double logRatio) => (Math.Exp(logRatio) - 1).ToString("+0.0%;-0.0%;0.0%", CultureInfo.InvariantCulture);

    private static string Ranges(List<int> values)
    {
        var parts = new List<string>();
        for (var i = 0; i < values.Count;)
        {
            var j = i;
            while (j + 1 < values.Count && values[j + 1] == values[j] + 1) j++;
            parts.Add(i == j ? $"{values[i]}" : $"{values[i]}-{values[j]}");
            i = j + 1;
        }
        return string.Join(",", parts);
    }

    public static IEnumerable<Measurement> ReadCsv(string path)
    {
        using var reader = new StreamReader(path);
        var header = ParseLine(reader.ReadLine() ?? throw new InvalidDataException($"{path} is empty"));
        int Col(string name) => header.IndexOf(name) is var i and >= 0 ? i : throw new InvalidDataException($"{path}: no column {name}");
        int sha = Col("commit_sha"), target = Col("target"), attempt = Col("run_attempt"), run = Col("run_id"),
            project = Col("project"), test = Col("test"), outcome = Col("outcome"), duration = Col("duration_ms");
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            var f = ParseLine(line);
            if (f[sha] == "commit_sha") continue; // repeated header from naive concatenation
            yield return new Measurement(f[sha], f[target], int.Parse(f[attempt], CultureInfo.InvariantCulture), f[run], f[project], f[test], f[outcome],
                double.Parse(f[duration], CultureInfo.InvariantCulture));
        }
    }

    private static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else sb.Append(ch);
            }
            else if (ch == '"') quoted = true;
            else if (ch == ',') { fields.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(ch);
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
