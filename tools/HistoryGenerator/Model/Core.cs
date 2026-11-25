using System.Globalization;
using System.Text;

namespace HistoryGenerator.Model;

/// <summary>CI targets the history is measured on. Ids match the `target` column of durations.csv.</summary>
public enum Target { LinuxX64, LinuxArm64, WindowsX64, MacArm64 }

public static class Targets
{
    public static readonly IReadOnlyList<Target> All = [Target.LinuxX64, Target.LinuxArm64, Target.WindowsX64, Target.MacArm64];
    public static readonly IReadOnlyList<Target> Posix = [Target.LinuxX64, Target.LinuxArm64, Target.MacArm64];
    public static readonly IReadOnlyList<Target> Linux = [Target.LinuxX64, Target.LinuxArm64];
    public static readonly IReadOnlyList<Target> Arm64 = [Target.LinuxArm64, Target.MacArm64];
    public static readonly IReadOnlyList<Target> WindowsOnly = [Target.WindowsX64];
    public static readonly IReadOnlyList<Target> MacOnly = [Target.MacArm64];

    public static string Id(this Target t) => t switch
    {
        Target.LinuxX64 => "linux-x64",
        Target.LinuxArm64 => "linux-arm64",
        Target.WindowsX64 => "windows-x64",
        Target.MacArm64 => "macos-arm64",
        _ => throw new ArgumentOutOfRangeException(nameof(t)),
    };

    public static Target Parse(string id) =>
        All.Where(t => t.Id() == id).Select(t => (Target?)t).FirstOrDefault()
        ?? throw new FormatException($"Unknown target '{id}'.");

    public static bool IsWindows(this Target t) => t == Target.WindowsX64;
    public static bool IsMac(this Target t) => t == Target.MacArm64;
    public static bool IsLinux(this Target t) => t is Target.LinuxX64 or Target.LinuxArm64;
    public static bool IsArm64(this Target t) => t is Target.LinuxArm64 or Target.MacArm64;

    /// <summary>Mirrors Storefront.Infrastructure.PlatformCost.Select(linux, windows, macOS).</summary>
    public static double Select(this Target t, double linux, double windows, double macOS) =>
        t.IsWindows() ? windows : t.IsMac() ? macOS : linux;
}

/// <summary>Identity of a test case. <see cref="TrxName"/> is the exact `testName` xUnit v2 writes to TRX.</summary>
public sealed record TestRef(string Project, string Namespace, string Class, string Method, string? Args = null)
{
    public string MethodFqn => $"{Namespace}.{Class}.{Method}";
    public string TrxName => Args is null ? MethodFqn : $"{MethodFqn}({Args})";
    public override string ToString() => TrxName;

    public static TestRef Unit(string area, string cls, string method, string? args = null) =>
        new("Storefront.UnitTests", $"Storefront.UnitTests.{area}", cls, method, args);

    public static TestRef Integration(string area, string cls, string method, string? args = null) =>
        new("Storefront.IntegrationTests", $"Storefront.IntegrationTests.{area}", cls, method, args);
}

/// <summary>
/// Expected distribution of a test's duration for one (commit, target). Seeded variants carry the key passed to
/// <c>Workload.Random(key)</c>; the draw u = new Random(seed).NextDouble() decides the realized value:
/// Uniform → Min + u·(Max−Min); Mixture → u &lt; PFirst ? First : Second; Tail → u &lt; P ? Outlier : Base.
/// </summary>
public abstract record Dist
{
    public sealed record Point(double Milliseconds) : Dist;
    public sealed record Uniform(double Min, double Max, string Key) : Dist;
    public sealed record Mixture(double PFirst, double First, double Second, string Key) : Dist;
    public sealed record Tail(double Base, double P, double Outlier, string Key) : Dist;
    /// <summary>Real CPU work, expressed in relative units (only ratios between segments are meaningful).</summary>
    public sealed record CpuBound(double Units) : Dist;
    /// <summary>Sub-millisecond real logic; not modelled beyond "tiny".</summary>
    public sealed record Micro : Dist;
    /// <summary>Test is marked Skip (TRX outcome NotExecuted); <see cref="Underlying"/> is the hidden cost.</summary>
    public sealed record Skipped(Dist Underlying) : Dist;
    /// <summary>Test does not exist at this commit.</summary>
    public sealed record Absent : Dist;

    public static Point Ms(double ms) => new(ms);

    /// <summary>Median of the modelled distribution in ms (null when not modelled in ms).</summary>
    public double? Median => this switch
    {
        Point p => p.Milliseconds,
        Uniform u => (u.Min + u.Max) / 2,
        Mixture m => m.PFirst > 0.5 ? m.First : m.PFirst < 0.5 ? m.Second : (m.First + m.Second) / 2,
        Tail t => t.P < 0.5 ? t.Base : t.Outlier,
        Skipped s => s.Underlying.Median,
        _ => null,
    };

    public double? Mean => this switch
    {
        Point p => p.Milliseconds,
        Uniform u => (u.Min + u.Max) / 2,
        Mixture m => m.PFirst * m.First + (1 - m.PFirst) * m.Second,
        Tail t => (1 - t.P) * t.Base + t.P * t.Outlier,
        Skipped s => s.Underlying.Mean,
        _ => null,
    };

    public string Kind => this switch
    {
        Point => "point",
        Uniform => "uniform",
        Mixture => "mixture",
        Tail => "tail",
        CpuBound => "cpu",
        Micro => "micro",
        Skipped => "skipped",
        Absent => "absent",
        _ => "?",
    };

    public string Describe() => this switch
    {
        Point p => $"{Fmt.N(p.Milliseconds)} ms",
        Uniform u => $"U({Fmt.N(u.Min)}..{Fmt.N(u.Max)}) ms",
        Mixture m => $"{{{Fmt.N(m.First)} ms p={Fmt.N(m.PFirst)} | {Fmt.N(m.Second)} ms}}",
        Tail t => $"{Fmt.N(t.Base)} ms, {Fmt.N(t.P * 100)}% → {Fmt.N(t.Outlier)} ms",
        CpuBound c => $"cpu×{Fmt.N(c.Units)}",
        Micro => "<1 ms",
        Skipped s => $"skipped ({s.Underlying.Describe()} hidden)",
        Absent => "absent",
        _ => "?",
    };
}

public static class Kinds
{
    public const string Step = "step";
    public const string HiddenStep = "hidden_step";
    public const string RampStep = "ramp_step";
    public const string Variance = "variance";
    public const string Modality = "modality";
    public const string Tail = "tail";
    public const string TestScope = "test_scope";
    public const string TestAdded = "test_added";
    public const string TestRemoved = "test_removed";
    public const string TestRenamed = "test_renamed";
    public const string TestSkipped = "test_skipped";
    public const string TestUnskipped = "test_unskipped";
    public const string Decoy = "decoy";

    public static bool IsLifecycle(string kind) =>
        kind is TestAdded or TestRemoved or TestRenamed or TestSkipped or TestUnskipped;
}

public static class Tiers
{
    public const string Must = "must";
    public const string Should = "should";
    public const string Optional = "optional";
    public const string MustNot = "must_not";
}

public static class Directions
{
    public const string Regression = "regression";
    public const string Improvement = "improvement";
    public const string Neutral = "neutral";
}

/// <summary>One declared change a scenario makes at a commit.</summary>
public sealed record Change
{
    public required int Commit { get; init; }
    public required string Kind { get; init; }
    public required string Subject { get; init; }
    public string? Body { get; init; }
    /// <summary>Explicit direction; null means "computed from medians".</summary>
    public string? Direction { get; init; }
    public string Tier { get; init; } = Tiers.Must;
    /// <summary>When set, this commit is a `git revert` of the change made at that commit.</summary>
    public int? RevertOf { get; init; }
    /// <summary>"fixes" | "partially_fixes" | "reverts" | "reintroduces".</summary>
    public string? Link { get; init; }
    public int? LinkCommit { get; init; }
    public string? Note { get; init; }
    public bool IsRampStep => Kind == Kinds.RampStep;
    public bool IsPrimary => !IsRampStep;
}

/// <summary>A scenario owns a set of files and the duration model of the tests they contain.</summary>
public abstract class Scenario
{
    /// <summary>Stable id, e.g. REG-STEP-LARGE. Never appears in the generated repository.</summary>
    public abstract string Id { get; }
    /// <summary>control | regression | optimization | lifecycle | drift | environment</summary>
    public abstract string Category { get; }
    public abstract string Summary { get; }
    public virtual IReadOnlyList<Change> Changes => [];
    /// <summary>Gradual changes: first and last commit of the ramp.</summary>
    public virtual (int Start, int End)? Ramp => null;
    /// <summary>Tests that exist at a commit (including skipped ones).</summary>
    public abstract IEnumerable<TestRef> Tests(int commit);
    public abstract Dist Expected(TestRef test, Target target, int commit);
    public abstract void Render(int commit, Snapshot snapshot);
    /// <summary>Optional per-test/target tier override for an event of a change.</summary>
    public virtual string? TierFor(Change change, TestRef test, Target target) => null;
    /// <summary>Extra notes for GROUND_TRUTH.md.</summary>
    public virtual string? Notes => null;

    public IEnumerable<Change> ChangesAt(int commit) => Changes.Where(c => c.Commit == commit);

    /// <summary>Number of primary (non-ramp) changes made up to and including the commit.</summary>
    protected int Phase(int commit) => Changes.Count(c => c.IsPrimary && c.Commit <= commit);

    /// <summary>Number of ramp steps made up to and including the commit.</summary>
    protected int RampSteps(int commit) => Changes.Count(c => c.IsRampStep && c.Commit <= commit);

    public IReadOnlyList<TestRef> AllTests() =>
        Enumerable.Range(1, History.Commits).SelectMany(Tests).Distinct().ToList();
}

public sealed record FileEntry(string Content, string Owner, bool Executable);

/// <summary>Full content of the repository at one commit.</summary>
public sealed class Snapshot
{
    public const string StaticOwner = "static";
    public const string FillerArea = "filler-area";

    private readonly SortedDictionary<string, FileEntry> _files = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, FileEntry> Files => _files;

    public void Add(string path, string content, string owner, bool executable = false)
    {
        if (path.Contains('\\') || path.StartsWith('/') || path.Any(ch => ch > 127))
            throw new InvalidOperationException($"Bad path '{path}'.");
        if (_files.TryGetValue(path, out var existing))
            throw new InvalidOperationException($"'{path}' is emitted by both '{existing.Owner}' and '{owner}'.");
        _files[path] = new FileEntry(Normalize(content), owner, executable);
    }

    /// <summary>Edits a file in the filler area (docs, admin code that no test touches).</summary>
    public void Edit(string path, Func<string, string> edit)
    {
        if (!_files.TryGetValue(path, out var existing))
            throw new InvalidOperationException($"Filler edits missing file '{path}'.");
        if (existing.Owner != FillerArea)
            throw new InvalidOperationException($"Filler may not edit '{path}' owned by '{existing.Owner}'.");
        var updated = Normalize(edit(existing.Content));
        if (updated == existing.Content)
            throw new InvalidOperationException($"Filler edit of '{path}' changed nothing.");
        _files[path] = existing with { Content = updated };
    }

    public void AddFiller(string path, string content)
    {
        if (_files.ContainsKey(path)) Edit(path, _ => content);
        else Add(path, content, FillerArea);
    }

    private static string Normalize(string content)
    {
        var text = content.ReplaceLineEndings("\n");
        if (!text.EndsWith('\n')) text += "\n";
        return text;
    }
}

/// <summary>Invariant number formatting for generated code (never scientific notation).</summary>
public static class Fmt
{
    public static string N(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
        var s = value.ToString("0.##########", CultureInfo.InvariantCulture);
        return s == "-0" ? "0" : s;
    }

    public static string Pct(double ratio) => (ratio - 1).ToString("+0.#%;-0.#%;0%", CultureInfo.InvariantCulture);
}

/// <summary>
/// Minimal, strict line filter for code templates. A line ending with <c>//?flag</c> is kept (without the marker
/// and the whitespace before it) only when the flag is true; <c>//?!flag</c> keeps it only when the flag is false.
/// Unknown or unused flags throw, so typos cannot silently drop code.
/// </summary>
public static class Tpl
{
    public static string Code(string text, params (string Name, bool Value)[] flags)
    {
        var map = flags.ToDictionary(f => f.Name, f => f.Value, StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        var sb = new StringBuilder();
        foreach (var raw in text.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = raw;
            var marker = line.LastIndexOf("//?", StringComparison.Ordinal);
            if (marker >= 0)
            {
                var token = line[(marker + 3)..].Trim();
                var negate = token.StartsWith('!');
                var name = negate ? token[1..] : token;
                if (name.Length == 0 || !name.All(char.IsAsciiLetterOrDigit))
                    throw new InvalidOperationException($"Bad template marker in line: {raw}");
                if (!map.TryGetValue(name, out var value))
                    throw new InvalidOperationException($"Unknown template flag '{name}'.");
                used.Add(name);
                if (value == negate) continue;
                line = line[..marker].TrimEnd();
                if (line.Contains("//?", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Several template markers in line: {raw}");
            }
            sb.Append(line).Append('\n');
        }
        var unused = map.Keys.Except(used).ToList();
        if (unused.Count > 0) throw new InvalidOperationException($"Unused template flags: {string.Join(", ", unused)}");
        var result = sb.ToString();
        return result.EndsWith("\n\n", StringComparison.Ordinal) ? result[..^1] : result;
    }
}

/// <summary>Seeded draws; must stay identical to Storefront.Infrastructure.Workload.</summary>
public static class Seeds
{
    public const double BadRunnerFactor = 1.35;
    public const int BadRunnerModulus = 40;

    public static ulong Fnv1a(string text)
    {
        var hash = 14695981039346656037UL;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            hash ^= b;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    public static int Seed(string key, string sha, Target target, int attempt) =>
        (int)(Fnv1a($"{key}|{sha}|{target.Id()}|{attempt}") & 0x7FFFFFFF);

    public static double Draw(string key, string sha, Target target, int attempt) =>
        new Random(Seed(key, sha, target, attempt)).NextDouble();

    public static bool IsBadRunner(string sha, Target target, int attempt) =>
        Fnv1a($"bad-runner|{sha}|{target.Id()}|{attempt}") % BadRunnerModulus == 0;

    /// <summary>Realized expected value (ms) of a distribution for one run; null when not modelled in ms.</summary>
    public static double? Realize(Dist dist, string sha, Target target, int attempt)
    {
        var factor = IsBadRunner(sha, target, attempt) ? BadRunnerFactor : 1.0;
        return dist switch
        {
            Dist.Point p => p.Milliseconds * factor,
            Dist.Uniform u => (u.Min + Draw(u.Key, sha, target, attempt) * (u.Max - u.Min)) * factor,
            Dist.Mixture m => (Draw(m.Key, sha, target, attempt) < m.PFirst ? m.First : m.Second) * factor,
            Dist.Tail t => (Draw(t.Key, sha, target, attempt) < t.P ? t.Outlier : t.Base) * factor,
            _ => null,
        };
    }
}
