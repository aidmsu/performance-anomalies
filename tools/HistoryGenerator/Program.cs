using HistoryGenerator.Git;
using HistoryGenerator.Model;
using HistoryGenerator.Output;
using HistoryGenerator.Scenarios;
using HistoryGenerator.Validation;

var verb = args.FirstOrDefault() ?? "help";
var options = ParseOptions(args.Skip(1).ToArray());

try
{
    return verb switch
    {
        "plan" => PrintPlan(),
        "render" => Render(),
        "lint" => Lint(),
        "generate" => Generate(),
        "check" => Check(),
        "ground-truth" => WriteGroundTruth(),
        "validate" => Validate(),
        _ => Help(),
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 2;
}

int Help()
{
    Console.WriteLine("""
        HistoryGenerator <verb> [options]

          plan                                     Print the commit timeline.
          render --commit N --out DIR              Write the repository snapshot of commit N into DIR.
          lint                                     Check scenarios, files, model and commit messages.
          generate --repo PATH                     Create the 120 commits on top of the base commit (HEAD must be the base).
          check --repo PATH [--ref REF]            Re-render every commit and compare with the committed trees.
          ground-truth --repo PATH --out DIR [--ref REF]
                                                   Write events.json, segments.jsonl, expected-durations.csv, ...
          validate --repo PATH --measurements A.csv[,B.csv] [--ref REF] [--targets t1,t2] [--report FILE]
                                                   Compare measured durations with the model.
        """);
    return 1;
}

int PrintPlan()
{
    foreach (var plan in History.Plan)
    {
        var what = plan.Changes.Count > 0
            ? string.Join(", ", plan.Changes.Select(c => $"{c.Scenario.Id}:{c.Change.Kind}"))
            : plan.Filler is not null ? "filler" : "initial";
        Console.WriteLine($"{plan.Index,4}  {History.SubjectOf(plan.Index, i => $"<sha{i}>"),-72}  {what}");
    }
    Console.WriteLine($"Scenarios: {Catalog.All.Count}; tests at last commit: {Catalog.All.Sum(s => s.Tests(History.Commits).Count())}; " +
                      $"fillers used: {History.Plan.Count(p => p.Filler is not null)}/{Fillers.All.Count}");
    return 0;
}

int Render()
{
    var commit = int.Parse(Required("commit"));
    var outDir = Path.GetFullPath(Required("out"));
    if (Directory.Exists(outDir) && Directory.EnumerateFileSystemEntries(outDir).Any(e => !e.EndsWith("/bin") && !e.EndsWith("/obj")))
    {
        // Remove previous render output but keep build caches out of the way of stale sources.
        foreach (var file in Directory.EnumerateFiles(outDir, "*", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                                 !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                                 !f.Contains($"{Path.DirectorySeparatorChar}TestResults{Path.DirectorySeparatorChar}")))
            File.Delete(file);
    }
    var snapshot = History.Render(commit);
    foreach (var (path, entry) in snapshot.Files)
    {
        var full = Path.Combine(outDir, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, entry.Content, new System.Text.UTF8Encoding(false));
        if (entry.Executable && !OperatingSystem.IsWindows())
            File.SetUnixFileMode(full, File.GetUnixFileMode(full) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
    }
    Console.WriteLine($"Rendered commit {commit}: {snapshot.Files.Count} files into {outDir}");
    return 0;
}

int Lint()
{
    var errors = Linter.Run();
    foreach (var e in errors) Console.WriteLine($"lint: {e}");
    Console.WriteLine(errors.Count == 0 ? "lint: OK" : $"lint: {errors.Count} problem(s)");
    return errors.Count == 0 ? 0 : 1;
}

int Generate()
{
    var errors = Linter.Run();
    if (errors.Count > 0)
    {
        foreach (var e in errors) Console.WriteLine($"lint: {e}");
        Console.Error.WriteLine("Refusing to generate: lint failed.");
        return 1;
    }
    var repo = new GitRepo(Required("repo"));
    var shas = HistoryWriter.Generate(repo);
    Console.WriteLine($"Generated {shas.Count} commits; HEAD {shas[^1]}");
    return 0;
}

int Check()
{
    var repo = new GitRepo(Required("repo"));
    var mismatches = HistoryWriter.Check(repo, options.GetValueOrDefault("ref", "HEAD"));
    Console.WriteLine(mismatches == 0 ? "check: every commit matches its rendering" : $"check: {mismatches} mismatch(es)");
    return mismatches == 0 ? 0 : 1;
}

int WriteGroundTruth()
{
    var repo = new GitRepo(Required("repo"));
    var shas = Shas(repo);
    var outDir = Path.GetFullPath(Required("out"));
    GroundTruth.Write(outDir, shas);
    Console.WriteLine($"Ground truth written to {outDir}");
    return 0;
}

int Validate()
{
    var repo = new GitRepo(Required("repo"));
    var shas = Shas(repo);
    var files = Required("measurements").Split(',', StringSplitOptions.RemoveEmptyEntries);
    var targets = options.TryGetValue("targets", out var t) ? t.Split(',').Select(Targets.Parse).ToHashSet() : null;
    return Validator.Run(shas, files, targets, options.GetValueOrDefault("report"));
}

IReadOnlyList<string> Shas(GitRepo repo)
{
    var shas = repo.GeneratedCommits(options.GetValueOrDefault("ref", "HEAD"));
    if (shas.Count != History.Commits)
        throw new InvalidOperationException($"Expected {History.Commits} generated commits, found {shas.Count}.");
    return shas;
}

string Required(string name) =>
    options.TryGetValue(name, out var value) ? value : throw new ArgumentException($"--{name} is required");

static Dictionary<string, string> ParseOptions(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Unexpected argument '{args[i]}'");
        var key = args[i][2..];
        result[key] = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal) ? args[++i] : "true";
    }
    return result;
}
