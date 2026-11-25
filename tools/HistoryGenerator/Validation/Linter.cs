using System.Text.RegularExpressions;
using HistoryGenerator.Model;
using HistoryGenerator.Scenarios;

namespace HistoryGenerator.Validation;

/// <summary>Consistency checks between scenarios, rendered files, duration model and commit messages.</summary>
public static partial class Linter
{
    private static readonly string[] SkipMarkers = ["[skip ci]", "[ci skip]", "[no ci]", "[skip actions]", "[actions skip]", "skip-checks"];
    private static readonly string[] RevealingWords = ["slow", "regress", "degrad", "perf", "latency", "scenario", "anomal", "synthetic"];

    [GeneratedRegex(@"(?<![A-Za-z0-9_.])\d+(\.\d+)?[eE][-+]?\d+(?![A-Za-z0-9_])")]
    private static partial Regex ScientificNumber();

    [GeneratedRegex(@"#\d")]
    private static partial Regex IssueReference();

    public static List<string> Run()
    {
        var errors = new List<string>();
        var snapshots = new Snapshot[History.Commits + 1];
        for (var c = 1; c <= History.Commits; c++)
        {
            try { snapshots[c] = History.Render(c); }
            catch (Exception ex) { errors.Add($"commit {c}: render failed: {ex.Message}"); }
        }
        if (errors.Count > 0) return errors;

        CheckFiles(snapshots, errors);
        CheckDiffOwnership(snapshots, errors);
        CheckModel(errors);
        CheckReverts(snapshots, errors);
        CheckTests(errors);
        CheckMessages(errors);
        return errors;
    }

    private static void CheckFiles(Snapshot[] snapshots, List<string> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var c = 1; c <= History.Commits; c++)
        {
            var lower = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (path, entry) in snapshots[c].Files)
            {
                var key = path.ToLowerInvariant();
                if (lower.TryGetValue(key, out var other)) errors.Add($"commit {c}: case-insensitive collision {path} / {other}");
                lower[key] = path;
                if (!seen.Add(path + "\u0000" + entry.Content)) continue;
                if (entry.Content.Contains('\r')) errors.Add($"{path}: contains CR");
                if (entry.Content.StartsWith('﻿')) errors.Add($"{path}: has BOM");
                if (entry.Content.Contains("//?", StringComparison.Ordinal)) errors.Add($"{path}: leftover template marker");
                if (path.EndsWith(".cs", StringComparison.Ordinal) && ScientificNumber().IsMatch(entry.Content))
                    errors.Add($"{path}: number in scientific notation");
                if (path.EndsWith(".cs", StringComparison.Ordinal) && entry.Content.Contains("\t", StringComparison.Ordinal))
                    errors.Add($"{path}: contains a tab");
                if (entry.Owner != Snapshot.StaticOwner && entry.Owner != Snapshot.FillerArea)
                {
                    foreach (var id in Catalog.All.Select(s => s.Id))
                        if (entry.Content.Contains(id, StringComparison.Ordinal)) errors.Add($"{path}: reveals scenario id {id}");
                }
            }
        }
    }

    private static IEnumerable<string> ChangedPaths(Snapshot before, Snapshot after) =>
        before.Files.Keys.Union(after.Files.Keys).Where(p =>
            !before.Files.TryGetValue(p, out var a) || !after.Files.TryGetValue(p, out var b) || a.Content != b.Content || a.Executable != b.Executable);

    private static void CheckDiffOwnership(Snapshot[] snapshots, List<string> errors)
    {
        for (var c = 2; c <= History.Commits; c++)
        {
            var plan = History.Plan[c - 1];
            var changed = ChangedPaths(snapshots[c - 1], snapshots[c]).ToList();
            if (changed.Count == 0) errors.Add($"commit {c}: empty diff");
            var declared = plan.Changes.Select(x => x.Scenario.Id).ToHashSet(StringComparer.Ordinal);
            var touchedOwners = new HashSet<string>(StringComparer.Ordinal);
            foreach (var path in changed)
            {
                var owner = (snapshots[c].Files.GetValueOrDefault(path) ?? snapshots[c - 1].Files[path]).Owner;
                touchedOwners.Add(owner);
                if (owner == Snapshot.StaticOwner) errors.Add($"commit {c}: static file {path} changed");
                else if (owner == Snapshot.FillerArea && plan.Filler is null) errors.Add($"commit {c}: filler-area file {path} changed without a filler");
                else if (owner != Snapshot.FillerArea && !declared.Contains(owner)) errors.Add($"commit {c}: {path} owned by {owner} changed but {owner} declares no change");
            }
            foreach (var id in declared.Where(id => !touchedOwners.Contains(id)))
                errors.Add($"commit {c}: {id} declares a change but none of its files changed");
            if (plan.Filler is not null && !touchedOwners.Contains(Snapshot.FillerArea))
                errors.Add($"commit {c}: filler '{plan.Filler.Subject}' changed nothing");
            if (plan.Primary.Count() > 1 && !History.Squashes.ContainsKey(c))
                errors.Add($"commit {c}: {plan.Primary.Count()} primary changes ({string.Join(", ", plan.Primary.Select(p => p.Scenario.Id))})");
        }
    }

    private static void CheckModel(List<string> errors)
    {
        foreach (var scenario in Catalog.All)
        {
            var tests = scenario.AllTests();
            for (var c = 2; c <= History.Commits; c++)
            {
                var changesHere = scenario.ChangesAt(c).ToList();
                var modelChanged = false;
                foreach (var test in tests)
                foreach (var target in Targets.All)
                {
                    var before = scenario.Expected(test, target, c - 1);
                    var after = scenario.Expected(test, target, c);
                    if (before == after) continue;
                    modelChanged = true;
                    if (changesHere.Count == 0)
                        errors.Add($"{scenario.Id}: model of {test} on {target.Id()} changes at {c} without a declared change");
                }
                foreach (var change in changesHere)
                {
                    if (change.Kind == Kinds.Decoy && modelChanged) errors.Add($"{scenario.Id}: decoy at {c} changes the model");
                    if (change.Kind != Kinds.Decoy && !modelChanged) errors.Add($"{scenario.Id}: change '{change.Subject}' at {c} does not change the model");
                }
            }
            for (var c = 1; c <= History.Commits; c++)
            {
                var existing = scenario.Tests(c).ToHashSet();
                foreach (var test in tests)
                foreach (var target in Targets.All)
                {
                    var dist = scenario.Expected(test, target, c);
                    if (existing.Contains(test) == dist is Dist.Absent)
                        errors.Add($"{scenario.Id}: {test} at {c} is {(existing.Contains(test) ? "listed but Absent" : "not listed but modelled")}");
                }
            }
        }
    }

    private static void CheckReverts(Snapshot[] snapshots, List<string> errors)
    {
        foreach (var scenario in Catalog.All)
        foreach (var change in scenario.Changes.Where(ch => ch.RevertOf is not null))
        {
            var c = change.Commit;
            var x = change.RevertOf!.Value;
            var paths = Enumerable.Range(1, History.Commits)
                .SelectMany(i => snapshots[i].Files.Where(f => f.Value.Owner == scenario.Id).Select(f => f.Key))
                .Distinct().ToList();
            foreach (var path in paths)
            {
                string? At(int i, string p) => i < 1 ? null : snapshots[i].Files.GetValueOrDefault(p)?.Content;
                if (At(c, path) != At(x - 1, path) || At(c - 1, path) != At(x, path))
                    errors.Add($"{scenario.Id}: revert at {c} of {x} is not an exact inverse for {path}");
            }
        }
    }

    private static void CheckTests(List<string> errors)
    {
        for (var c = 1; c <= History.Commits; c++)
        {
            var names = Catalog.All.SelectMany(s => s.Tests(c)).Select(t => t.TrxName).ToList();
            foreach (var dup in names.GroupBy(n => n).Where(g => g.Count() > 1))
                errors.Add($"commit {c}: duplicate test {dup.Key}");
            foreach (var name in names.Where(n => n.Any(ch => ch > 127)))
                errors.Add($"commit {c}: non-ASCII test name {name}");
        }
    }

    private static void CheckMessages(List<string> errors)
    {
        static string FakeSha(int i) => i.ToString("x40");
        var subjects = new HashSet<string>(StringComparer.Ordinal);
        var ids = Catalog.All.Select(s => s.Id).ToList();
        foreach (var plan in History.Plan)
        {
            var message = History.Message(plan, FakeSha);
            var lines = message.Split('\n');
            var subject = lines[0];
            var withoutTrailer = string.Join('\n', lines.Where(l => !l.StartsWith("Co-Authored-By:", StringComparison.Ordinal)));
            if (!subjects.Add(subject)) errors.Add($"commit {plan.Index}: duplicate subject '{subject}'");
            if (subject.Length > 72) errors.Add($"commit {plan.Index}: subject longer than 72 chars: '{subject}'");
            if (subject.EndsWith('.')) errors.Add($"commit {plan.Index}: subject ends with a period");
            foreach (var line in lines.Skip(1).Where(l => l.Length > 90))
                errors.Add($"commit {plan.Index}: body line longer than 90 chars: '{line}'");
            foreach (var marker in SkipMarkers.Where(m => message.Contains(m, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"commit {plan.Index}: contains '{marker}'");
            if (IssueReference().IsMatch(message)) errors.Add($"commit {plan.Index}: contains an issue reference");
            if (withoutTrailer.Contains('@')) errors.Add($"commit {plan.Index}: contains '@'");
            foreach (var id in ids.Where(id => message.Contains(id, StringComparison.Ordinal)))
                errors.Add($"commit {plan.Index}: reveals scenario id {id}");
            foreach (var word in RevealingWords.Where(w => withoutTrailer.Contains(w, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"commit {plan.Index}: contains revealing word '{word}'");
        }
    }
}
