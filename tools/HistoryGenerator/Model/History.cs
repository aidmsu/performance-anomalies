using System.Text;
using HistoryGenerator.Scenarios;

namespace HistoryGenerator.Model;

public sealed record Filler(string Subject, string Body, Action<Snapshot> Apply);

public sealed record PlannedChange(Scenario Scenario, Change Change);

public sealed record CommitPlan(int Index, IReadOnlyList<PlannedChange> Changes, Filler? Filler)
{
    public IEnumerable<PlannedChange> Primary => Changes.Where(c => c.Change.IsPrimary);
    public IEnumerable<PlannedChange> RampSteps => Changes.Where(c => c.Change.IsRampStep);
}

/// <summary>The commit timeline: computed from the scenario catalog plus fillers, never written by hand.</summary>
public static class History
{
    public const int Commits = 120;
    public const string BaseSha = "8b937a72375cf7ea3c2ca0d91c97127464a8dc8f";
    public static readonly DateTimeOffset BaseDate = DateTimeOffset.Parse("2026-11-25T21:22:42+01:00");
    public const string AuthorName = "aidmsu";
    public const string AuthorEmail = "4264267+aidmsu@users.noreply.github.com";
    public const string Trailer = "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>";

    public static DateTimeOffset DateOf(int commit) => BaseDate.AddMinutes(1).AddSeconds(45 * (commit - 1));

    /// <summary>Commits that intentionally carry several unrelated primary changes (a squash merge).</summary>
    public static readonly IReadOnlyDictionary<int, (string Subject, string Body)> Squashes =
        new Dictionary<int, (string Subject, string Body)>
        {
            [22] = ("Harden gift card redemption and return pickups",
                "Squash of the checkout-hardening branch."),
        };

    private static IReadOnlyList<CommitPlan>? _plan;
    public static IReadOnlyList<CommitPlan> Plan => _plan ??= BuildPlan();

    private static List<CommitPlan> BuildPlan()
    {
        var fillers = new Queue<Filler>(Fillers.All);
        var plans = new List<CommitPlan>(Commits);
        for (var c = 1; c <= Commits; c++)
        {
            var changes = Catalog.All
                .SelectMany(s => s.ChangesAt(c).Select(ch => new PlannedChange(s, ch)))
                .ToList();
            if (c == 1 && changes.Count > 0)
                throw new InvalidOperationException("Scenarios may not declare changes in commit 1.");
            Filler? filler = null;
            if (c > 1 && changes.Count == 0)
            {
                if (fillers.Count == 0) throw new InvalidOperationException($"Not enough fillers (commit {c}).");
                filler = fillers.Dequeue();
            }
            plans.Add(new CommitPlan(c, changes, filler));
        }
        return plans;
    }

    public static Snapshot Render(int commit)
    {
        if (commit is < 1 or > Commits) throw new ArgumentOutOfRangeException(nameof(commit));
        var snapshot = new Snapshot();
        StaticFiles.AddTo(snapshot);
        foreach (var scenario in Catalog.All) scenario.Render(commit, snapshot);
        foreach (var plan in Plan.Take(commit)) plan.Filler?.Apply(snapshot);
        return snapshot;
    }

    /// <summary>Subject of the original change that a revert chain starting at <paramref name="commit"/> refers to.</summary>
    private static (string Subject, int Depth) Original(int commit)
    {
        var change = Plan[commit - 1].Primary.Single().Change;
        if (change.RevertOf is int inner)
        {
            var (subject, depth) = Original(inner);
            return (subject, depth + 1);
        }
        return (change.Subject, 0);
    }

    public static string Message(CommitPlan plan, Func<int, string> shaOf)
    {
        string subject;
        string? body;
        var extra = new List<string>();

        if (plan.Index == 1)
        {
            subject = "Create storefront solution with unit and integration tests";
            body = "Domain library, two xUnit test projects and a CI workflow that records\n" +
                   "per-test durations on Linux, Windows and macOS.";
        }
        else if (Squashes.TryGetValue(plan.Index, out var squash))
        {
            subject = squash.Subject;
            var lines = plan.Primary.Select(p => $"* {p.Change.Subject}" + (p.Change.Body is { } b ? $"\n\n  {b.Replace("\n", "\n  ")}" : ""));
            body = squash.Body + "\n\n" + string.Join("\n\n", lines);
            extra.AddRange(plan.RampSteps.Select(r => r.Change.Subject));
        }
        else if (plan.Primary.Count() > 1)
        {
            throw new InvalidOperationException($"Commit {plan.Index} has several primary changes but is not a declared squash.");
        }
        else if (plan.Primary.SingleOrDefault() is { } primary)
        {
            var change = primary.Change;
            if (change.RevertOf is int reverted)
            {
                var (original, depth) = Original(reverted);
                subject = depth % 2 == 0 ? $"Revert \"{original}\"" : $"Reapply \"{original}\"";
                body = $"This reverts commit {shaOf(reverted)}." + (change.Body is { } b ? $"\n\n{b}" : "");
            }
            else
            {
                subject = change.Subject;
                body = change.Body;
            }
            extra.AddRange(plan.RampSteps.Select(r => r.Change.Subject));
        }
        else if (plan.RampSteps.FirstOrDefault() is { } ramp)
        {
            subject = ramp.Change.Subject;
            body = ramp.Change.Body;
            extra.AddRange(plan.RampSteps.Skip(1).Select(r => r.Change.Subject));
        }
        else if (plan.Filler is { } filler)
        {
            subject = filler.Subject;
            body = filler.Body;
        }
        else
        {
            throw new InvalidOperationException($"Commit {plan.Index} has nothing to commit.");
        }

        var sb = new StringBuilder(subject);
        if (!string.IsNullOrWhiteSpace(body)) sb.Append("\n\n").Append(body.Trim());
        if (extra.Count > 0) sb.Append("\n\nAlso in this commit:\n").Append(string.Join("\n", extra.Select(e => $"- {e}")));
        sb.Append("\n\n").Append(Trailer).Append('\n');
        return sb.ToString();
    }

    public static string SubjectOf(int commit, Func<int, string> shaOf) => Message(Plan[commit - 1], shaOf).Split('\n')[0];
}
