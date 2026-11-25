using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HistoryGenerator.Model;
using HistoryGenerator.Scenarios;

namespace HistoryGenerator.Output;

public sealed record AtomicEvent
{
    public required string EventId { get; init; }
    public required string ScenarioId { get; init; }
    public required string Category { get; init; }
    public required string Project { get; init; }
    public required string Test { get; init; }
    public required string Target { get; init; }
    public required int Commit { get; init; }
    public required string Sha { get; init; }
    public required string Kind { get; init; }
    public required string Direction { get; init; }
    public required string Tier { get; init; }
    public required string Before { get; init; }
    public required string After { get; init; }
    public double? BeforeMedianMs { get; init; }
    public double? AfterMedianMs { get; init; }
    public double? RelativeChange { get; init; }
    public double? AbsoluteChangeMs { get; init; }
    public required int WindowFrom { get; init; }
    public required int WindowTo { get; init; }
    public string? Link { get; init; }
    public int? LinkCommit { get; init; }
    public string? LinkSha { get; init; }
    public required string CauseGroup { get; init; }
    public required IReadOnlyList<string> CulpritPaths { get; init; }
    public required string Subject { get; init; }
    public string? Note { get; init; }
}

public static class GroundTruth
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions JsonLine = new(Json) { WriteIndented = false };

    public static IReadOnlyList<AtomicEvent> Events(IReadOnlyList<string> shas)
    {
        string ShaOf(int c) => shas[c - 1];
        var events = new List<AtomicEvent>();
        var snapshots = new Dictionary<int, Snapshot>();
        Snapshot Snap(int c) => snapshots.TryGetValue(c, out var s) ? s : snapshots[c] = History.Render(c);

        foreach (var scenario in Catalog.All)
        {
            var tests = scenario.AllTests();
            var counter = 0;

            if (scenario.Ramp is { } ramp)
            {
                var rampChange = scenario.Changes.First(ch => ch.IsRampStep);
                foreach (var test in tests)
                foreach (var target in Targets.All)
                {
                    var before = scenario.Expected(test, target, ramp.Start - 1);
                    var after = scenario.Expected(test, target, ramp.End);
                    if (before == after) continue;
                    events.Add(Make(scenario, ++counter, test, target, ramp.Start, ShaOf, Kinds.RampStep == rampChange.Kind ? "ramp" : rampChange.Kind,
                        rampChange.Direction ?? DirectionOf(before, after), rampChange.Tier, before, after,
                        ramp.Start, Math.Min(History.Commits, ramp.End + 5), null, $"{scenario.Id}@ramp",
                        scenario.Changes.Where(ch => ch.IsRampStep).SelectMany(ch => CulpritPaths(scenario, ch.Commit, Snap)).Distinct().ToList(),
                        $"{scenario.Changes.Count(ch => ch.IsRampStep)} gradual steps between {ramp.Start} and {ramp.End}",
                        "Gradual change; any alert inside the window counts."));
                }
            }

            foreach (var change in scenario.Changes.Where(ch => ch.IsPrimary))
            {
                var c = change.Commit;
                var paths = CulpritPaths(scenario, c, Snap);
                foreach (var test in tests)
                foreach (var target in Targets.All)
                {
                    var before = scenario.Expected(test, target, c - 1);
                    var after = scenario.Expected(test, target, c);
                    if (before is Dist.Absent && after is Dist.Absent) continue;
                    if (change.Kind == Kinds.TestUnskipped &&
                        scenario.Changes.LastOrDefault(x => x.Kind == Kinds.TestSkipped && x.Commit < c) is { } skip &&
                        scenario.Expected(test, target, skip.Commit - 1) != after)
                    {
                        // The test returns at a different level; that change is the hidden step's event, not a no-op.
                        continue;
                    }
                    var unchanged = before == after;
                    var kind = unchanged && change.Kind != Kinds.Decoy ? "none" : change.Kind;
                    var direction = unchanged ? Directions.Neutral
                        : Kinds.IsLifecycle(change.Kind) || change.Kind == Kinds.TestScope ? change.Direction ?? Directions.Neutral
                        : change.Direction ?? DirectionOf(before, after);
                    var tier = unchanged ? Tiers.MustNot : scenario.TierFor(change, test, target) ?? change.Tier;
                    var (from, to) = Window(scenario, change);
                    events.Add(Make(scenario, ++counter, test, target, c, ShaOf, kind, direction, tier, before, after, from, to,
                        change, $"{scenario.Id}@{c}", paths, SubjectFor(change, ShaOf), change.Note));
                }
            }
        }
        return events;
    }

    /// <summary>A must_not point must never lie inside the acceptance window of a positive event of the same series.</summary>
    public static List<string> Overlaps(IReadOnlyList<AtomicEvent> events)
    {
        var errors = new List<string>();
        foreach (var group in events.GroupBy(e => (e.Test, e.Target)))
        {
            var positives = group.Where(e => e.Tier != Tiers.MustNot).ToList();
            foreach (var negative in group.Where(e => e.Tier == Tiers.MustNot))
            foreach (var positive in positives.Where(p => negative.Commit >= p.WindowFrom && negative.Commit <= p.WindowTo))
                errors.Add($"{negative.EventId} ({negative.Kind}@{negative.Commit}) is inside the window [{positive.WindowFrom},{positive.WindowTo}] of {positive.EventId} for {group.Key.Test} on {group.Key.Target}");
        }
        return errors;
    }

    private static (int From, int To) Window(Scenario scenario, Change change)
    {
        var c = change.Commit;
        return change.Kind switch
        {
            Kinds.Step or Kinds.TestScope => (Math.Max(1, c - 2), Math.Min(History.Commits, c + 2)),
            Kinds.Variance or Kinds.Modality or Kinds.Tail => (c, Math.Min(History.Commits, c + 10)),
            Kinds.HiddenStep => (c, Math.Min(History.Commits,
                (scenario.Changes.FirstOrDefault(x => x.Commit > c && x.Kind == Kinds.TestUnskipped)?.Commit ?? c) + 2)),
            _ => (c, c),
        };
    }

    private static string SubjectFor(Change change, Func<int, string> shaOf) =>
        History.SubjectOf(change.Commit, shaOf);

    private static List<string> CulpritPaths(Scenario scenario, int c, Func<int, Snapshot> snap)
    {
        if (c < 2) return [];
        var before = snap(c - 1).Files;
        var after = snap(c).Files;
        return before.Keys.Union(after.Keys)
            .Where(p => (after.GetValueOrDefault(p) ?? before[p]).Owner == scenario.Id)
            .Where(p => before.GetValueOrDefault(p)?.Content != after.GetValueOrDefault(p)?.Content)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    private static string DirectionOf(Dist before, Dist after)
    {
        var b = Unwrap(before).Mean;
        var a = Unwrap(after).Mean;
        if (before is Dist.CpuBound cb && after is Dist.CpuBound ca) return ca.Units > cb.Units ? Directions.Regression : Directions.Improvement;
        if (b is null || a is null) return Directions.Neutral;
        return a > b ? Directions.Regression : a < b ? Directions.Improvement : Directions.Regression;
    }

    private static Dist Unwrap(Dist d) => d is Dist.Skipped s ? s.Underlying : d;

    private static AtomicEvent Make(Scenario scenario, int n, TestRef test, Target target, int commit, Func<int, string> shaOf,
        string kind, string direction, string tier, Dist before, Dist after, int from, int to, Change? change, string causeGroup,
        IReadOnlyList<string> paths, string subject, string? note)
    {
        var bm = Unwrap(before).Median;
        var am = Unwrap(after).Median;
        double? rel = bm is > 0 && am is not null ? am / bm - 1 : before is Dist.CpuBound cb && after is Dist.CpuBound ca ? ca.Units / cb.Units - 1 : null;
        return new AtomicEvent
        {
            EventId = $"{scenario.Id}#{n:000}",
            ScenarioId = scenario.Id,
            Category = scenario.Category,
            Project = test.Project,
            Test = test.TrxName,
            Target = target.Id(),
            Commit = commit,
            Sha = shaOf(commit),
            Kind = kind,
            Direction = direction,
            Tier = tier,
            Before = before.Describe(),
            After = after.Describe(),
            BeforeMedianMs = bm,
            AfterMedianMs = am,
            RelativeChange = rel is null ? null : Math.Round(rel.Value, 4),
            AbsoluteChangeMs = bm is not null && am is not null ? Math.Round(am.Value - bm.Value, 3) : null,
            WindowFrom = from,
            WindowTo = to,
            Link = change?.Link,
            LinkCommit = change?.LinkCommit,
            LinkSha = change?.LinkCommit is int lc ? shaOf(lc) : null,
            CauseGroup = causeGroup,
            CulpritPaths = paths,
            Subject = subject,
            Note = note,
        };
    }

    public static void Write(string outDir, IReadOnlyList<string> shas)
    {
        Directory.CreateDirectory(outDir);
        string ShaOf(int c) => shas[c - 1];
        var events = Events(shas);

        File.WriteAllText(Path.Combine(outDir, "events.json"), JsonSerializer.Serialize(events, Json) + "\n");

        var commits = History.Plan.Select(p => new
        {
            Index = p.Index,
            Sha = ShaOf(p.Index),
            Date = History.DateOf(p.Index).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
            Subject = History.SubjectOf(p.Index, ShaOf),
            Changes = p.Changes.Select(x => $"{x.Scenario.Id}:{x.Change.Kind}").ToList(),
            Filler = p.Filler is not null,
        }).ToList();
        File.WriteAllText(Path.Combine(outDir, "commits.json"), JsonSerializer.Serialize(new
        {
            Schema = 1,
            BaseSha = History.BaseSha,
            Ordering = "topological (index); never order by date",
            Targets = Targets.All.Select(t => t.Id()),
            Commits = commits,
        }, Json) + "\n");

        var scenarios = Catalog.All.Select(s => new
        {
            s.Id,
            s.Category,
            s.Summary,
            s.Notes,
            Tests = s.AllTests().Select(t => t.TrxName),
            Ramp = s.Ramp is { } r ? new { r.Start, r.End } : null,
            Changes = s.Changes.Where(ch => ch.IsPrimary).Select(ch => new
            {
                ch.Commit,
                Sha = ShaOf(ch.Commit),
                ch.Kind,
                ch.Tier,
                ch.Direction,
                ch.Link,
                ch.LinkCommit,
                ch.RevertOf,
                Subject = History.SubjectOf(ch.Commit, ShaOf),
            }),
        });
        File.WriteAllText(Path.Combine(outDir, "scenarios.json"), JsonSerializer.Serialize(scenarios, Json) + "\n");

        WriteSegments(Path.Combine(outDir, "segments.jsonl"));
        WriteExpected(Path.Combine(outDir, "expected-durations.csv"), shas);
        WriteBadRunners(Path.Combine(outDir, "bad-runners.json"), shas);
        WriteNegativeSeries(Path.Combine(outDir, "negative-series.json"));
        File.WriteAllText(Path.Combine(outDir, "GROUND_TRUTH.md"), Markdown(events, shas));
    }

    private static void WriteSegments(string path)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false)) { NewLine = "\n" };
        foreach (var scenario in Catalog.All)
        foreach (var test in scenario.AllTests())
        foreach (var target in Targets.All)
        {
            var segments = new List<object>();
            var start = 1;
            var current = scenario.Expected(test, target, 1);
            for (var c = 2; c <= History.Commits + 1; c++)
            {
                var next = c <= History.Commits ? scenario.Expected(test, target, c) : null;
                if (next == current) continue;
                if (current is not Dist.Absent)
                    segments.Add(new { From = start, To = c - 1, Kind = current.Kind, Dist = current.Describe(), MedianMs = current.Median, MeanMs = current.Mean });
                start = c;
                current = next!;
            }
            writer.WriteLine(JsonSerializer.Serialize(new { Scenario = scenario.Id, test.Project, Test = test.TrxName, Target = target.Id(), Segments = segments }, JsonLine));
        }
    }

    private static void WriteExpected(string path, IReadOnlyList<string> shas)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false)) { NewLine = "\n" };
        writer.WriteLine("commit_index,commit_sha,target,project,test,scenario,dist_kind,dist,model_median_ms,expected_ms_attempt1,bad_runner_attempt1");
        for (var c = 1; c <= History.Commits; c++)
        {
            var sha = shas[c - 1];
            foreach (var target in Targets.All)
            {
                var bad = Seeds.IsBadRunner(sha, target, 1);
                foreach (var scenario in Catalog.All)
                foreach (var test in scenario.Tests(c))
                {
                    var dist = scenario.Expected(test, target, c);
                    var realized = Seeds.Realize(dist, sha, target, 1);
                    writer.WriteLine(string.Join(',',
                        c.ToString(CultureInfo.InvariantCulture), sha, target.Id(), test.Project, Csv(test.TrxName), scenario.Id,
                        dist.Kind, Csv(dist.Describe()),
                        dist.Median is { } m ? Fmt.N(Math.Round(m, 3)) : "",
                        realized is { } r ? Fmt.N(Math.Round(r, 3)) : "",
                        bad ? "true" : "false"));
                }
            }
        }
    }

    private static void WriteBadRunners(string path, IReadOnlyList<string> shas)
    {
        var runs = new List<object>();
        for (var c = 1; c <= History.Commits; c++)
        foreach (var target in Targets.All)
        for (var attempt = 1; attempt <= 3; attempt++)
            if (Seeds.IsBadRunner(shas[c - 1], target, attempt))
                runs.Add(new { Commit = c, Sha = shas[c - 1], Target = target.Id(), Attempt = attempt, Factor = Seeds.BadRunnerFactor });
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            Description = "Runs in which every simulated cost is multiplied by the factor (a noisy CI machine). No per-test alert is expected; a run-level anomaly may be reported.",
            Runs = runs,
        }, Json) + "\n");
    }

    private static void WriteNegativeSeries(string path)
    {
        var series = new List<object>();
        foreach (var scenario in Catalog.All)
        foreach (var test in scenario.AllTests())
        foreach (var target in Targets.All)
        {
            var dists = Enumerable.Range(1, History.Commits).Select(c => scenario.Expected(test, target, c)).Where(d => d is not Dist.Absent).Distinct().ToList();
            if (dists.Count == 1) series.Add(new { Scenario = scenario.Id, Test = test.TrxName, Target = target.Id(), Dist = dists[0].Describe() });
        }
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            Description = "(test, target) series whose duration model never changes. Every alert on them is a false positive.",
            Series = series,
        }, Json) + "\n");
    }

    private static string Csv(string value) =>
        value.IndexOfAny([',', '"', '\n']) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;

    private static string Markdown(IReadOnlyList<AtomicEvent> events, IReadOnlyList<string> shas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Ground truth");
        sb.AppendLine();
        sb.AppendLine("Generated by `tools/HistoryGenerator`. Commit numbers are 1-based positions in");
        sb.AppendLine($"`git rev-list --reverse --first-parent {History.BaseSha[..7]}..master` (topological order; never order by date).");
        sb.AppendLine("Targets: " + string.Join(", ", Targets.All.Select(t => $"`{t.Id()}`")) + ".");
        sb.AppendLine();
        sb.AppendLine("Machine-readable files: `events.json` (atomic events with tiers and windows), `segments.jsonl`,");
        sb.AppendLine("`expected-durations.csv` (model + realized seeded draws for attempt 1), `commits.json`, `scenarios.json`,");
        sb.AppendLine("`bad-runners.json`, `negative-series.json`.");
        sb.AppendLine();
        sb.AppendLine("Tiers: `must` (a competent detector should find it), `should`, `optional` (at or below the noise floor),");
        sb.AppendLine("`must_not` (no change happened for this test/target; any alert is a false positive).");
        sb.AppendLine();
        sb.AppendLine("Measurement notes:");
        sb.AppendLine();
        sb.AppendLine("- Model values exclude harness overhead. Measured durations are typically 0-2 ms above the model, and every");
        sb.AppendLine("  simulated call has about 3% multiplicative jitter.");
        sb.AppendLine("- Tests that start in the first parallel batch of a test run (the first ~2 tests of each project) can be 1-4 ms");
        sb.AppendLine("  slower because of start-up contention. Which tests start first varies from run to run; it is not a change.");
        sb.AppendLine("- `cpu×N` (real CPU work) has no absolute value; only the ratio between segments is part of the ground truth.");
        sb.AppendLine("  Its duration also depends on the runner's CPU (`cpu_model` column), which differs between runs; dividing by");
        sb.AppendLine("  `CatalogHasherTests.Hashes_catalog_export` from the same run removes most of that.");
        sb.AppendLine("- Seeded draws (uniform, mixture, tail) are exact per run: `expected-durations.csv` has the realized value for run");
        sb.AppendLine("  attempt 1, including the noisy-machine factor from `bad-runners.json`.");
        sb.AppendLine();
        foreach (var group in Catalog.All.GroupBy(s => s.Category))
        {
            sb.AppendLine($"## {char.ToUpperInvariant(group.Key[0])}{group.Key[1..]}");
            sb.AppendLine();
            foreach (var scenario in group)
            {
                sb.AppendLine($"### {scenario.Id}");
                sb.AppendLine();
                sb.AppendLine(scenario.Summary);
                if (scenario.Notes is { } notes) { sb.AppendLine(); sb.AppendLine(notes); }
                sb.AppendLine();
                var tests = scenario.AllTests();
                sb.AppendLine("Tests: " + string.Join(", ", tests.Take(8).Select(t => $"`{t.TrxName}`")) + (tests.Count > 8 ? $" and {tests.Count - 8} more" : ""));
                sb.AppendLine();
                var own = events.Where(e => e.ScenarioId == scenario.Id && e.Tier != Tiers.MustNot).ToList();
                if (own.Count > 0)
                {
                    sb.AppendLine("| Commit | SHA | Kind | Direction | Tier | Test | Targets | Before → After |");
                    sb.AppendLine("|---|---|---|---|---|---|---|---|");
                    foreach (var row in own.GroupBy(e => (e.Commit, e.Kind, e.Direction, e.Tier, e.Test, e.Before, e.After)))
                    {
                        var targets = string.Join(" ", row.Select(e => e.Target));
                        sb.AppendLine($"| {row.Key.Commit} | `{shas[row.Key.Commit - 1][..7]}` | {row.Key.Kind} | {row.Key.Direction} | {row.Key.Tier} | `{ShortName(row.Key.Test)}` | {targets} | {row.Key.Before} → {row.Key.After} |");
                    }
                    sb.AppendLine();
                }
                var negatives = events.Where(e => e.ScenarioId == scenario.Id && e.Tier == Tiers.MustNot && e.Kind != "decoy").ToList();
                if (negatives.Count > 0)
                    sb.AppendLine($"Explicit no-change points (must_not): {negatives.Count} (see events.json).").AppendLine();
            }
        }
        return sb.ToString();
    }

    private static string ShortName(string trxName)
    {
        var paren = trxName.IndexOf('(');
        var head = paren < 0 ? trxName : trxName[..paren];
        var parts = head.Split('.');
        var shortHead = parts.Length >= 2 ? $"{parts[^2]}.{parts[^1]}" : head;
        return paren < 0 ? shortHead : shortHead + trxName[paren..];
    }
}
