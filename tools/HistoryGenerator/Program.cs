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
          render --commit N --out DIR              Write the snapshot of commit N into an empty (or previously rendered) DIR.
          lint                                     Check scenarios, files, model and commit messages.
          generate --repo PATH                     Create the 120 commits on top of the base commit (HEAD must be the base).
          check --repo PATH [--ref REF]            Re-render every commit and compare with the committed trees.
          ground-truth --repo PATH --out DIR [--ref REF]
                                                   Write events.json, segments.jsonl, expected-durations.csv, ...
          validate --repo PATH --measurements A.csv[,B.csv] [--ref REF] [--targets t1,t2] [--report FILE]
                                                   Compare measured durations with the model.
          ground-truth and validate refuse a history that does not match the generator unless --force is given.
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
    const string Manifest = ".history-render-manifest";
    var commit = int.Parse(Required("commit"));
    var outDir = Path.GetFullPath(Required("out"));
    if (Directory.Exists(outDir))
    {
        if (Directory.Exists(Path.Combine(outDir, ".git")) || File.Exists(Path.Combine(outDir, ".git")))
            throw new InvalidOperationException($"{outDir} is a git checkout; render into an empty directory instead.");
        var manifestPath = Path.Combine(outDir, Manifest);
        var hasEntries = Directory.EnumerateFileSystemEntries(outDir).Any();
        if (hasEntries && !File.Exists(manifestPath))
            throw new InvalidOperationException($"{outDir} is not empty and was not created by 'render'.");
        if (File.Exists(manifestPath))
        {
            // Delete only what the previous render wrote; build outputs (bin/, obj/, TestResults/) are kept.
            foreach (var path in File.ReadAllLines(manifestPath).Where(p => p.Length > 0))
            {
                var full = Path.GetFullPath(Path.Combine(outDir, path));
                if (full.StartsWith(outDir + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(full)) File.Delete(full);
            }
        }
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
    File.WriteAllLines(Path.Combine(outDir, Manifest), snapshot.Files.Keys);
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
    var reference = options.GetValueOrDefault("ref", "HEAD");
    var shas = repo.GeneratedCommits(reference);
    if (shas.Count != History.Commits)
        throw new InvalidOperationException($"Expected {History.Commits} generated commits, found {shas.Count}.");
    if (!options.ContainsKey("force") && HistoryWriter.Check(repo, reference) != 0)
        throw new InvalidOperationException($"{reference} does not match the generator (see 'check'); pass --force to use it anyway.");
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
