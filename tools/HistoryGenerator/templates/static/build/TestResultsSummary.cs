#:property PublishAot=false
#:property Nullable=enable
#:property ImplicitUsings=enable

// Turns the TRX files of one test run into durations.csv (one row per test case, RFC 4180) and prints a summary.
//
//   dotnet run --file build/TestResultsSummary.cs -- <results-dir> [--output <file.csv>]
//
// Run metadata comes from the GitHub Actions environment when present (GITHUB_SHA, GITHUB_RUN_ID, ...),
// otherwise from git and the local machine. When GITHUB_STEP_SUMMARY is set, a Markdown table is appended to it.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

var resultsDir = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal) ? args[0] : "TestResults";
var outputIndex = Array.IndexOf(args, "--output");
var output = outputIndex >= 0 && outputIndex + 1 < args.Length ? args[outputIndex + 1] : Path.Combine(resultsDir, "durations.csv");

if (!Directory.Exists(resultsDir))
{
    Console.Error.WriteLine($"Results directory '{resultsDir}' does not exist.");
    return 1;
}

var trxFiles = Directory.GetFiles(resultsDir, "*.trx", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal).ToList();
if (trxFiles.Count == 0)
{
    Console.Error.WriteLine($"No .trx files found under '{resultsDir}'.");
    return 1;
}

var sha = Env("GITHUB_SHA") ?? Git("rev-parse", "HEAD") ?? "";
var commitIndex = int.TryParse(Git("rev-list", "--count", sha), out var count) ? (count - 1).ToString(CultureInfo.InvariantCulture) : "";
var meta = new (string Name, string Value)[]
{
    ("commit_sha", sha),
    ("commit_index", commitIndex),
    ("ref", Env("GITHUB_REF_NAME") ?? Git("rev-parse", "--abbrev-ref", "HEAD") ?? ""),
    ("event", Env("GITHUB_EVENT_NAME") ?? "local"),
    ("run_id", Env("GITHUB_RUN_ID") ?? "local"),
    ("run_attempt", Env("GITHUB_RUN_ATTEMPT") ?? "1"),
    ("job", Env("GITHUB_JOB") ?? ""),
    ("target", Target()),
    ("os", RuntimeInformation.OSDescription),
    ("arch", RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()),
    ("image_os", Env("ImageOS") ?? ""),
    ("image_version", Env("ImageVersion") ?? ""),
    ("runner_name", Env("RUNNER_NAME") ?? Environment.MachineName),
    ("cpu_model", CpuModel()),
    ("logical_cores", Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture)),
    ("sdk", Run("dotnet", "--version") ?? ""),
    ("runtime", RuntimeInformation.FrameworkDescription),
};

XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
var rows = new List<Row>();
foreach (var file in trxFiles)
{
    var doc = XDocument.Load(file);
    var codeBases = doc.Descendants(ns + "UnitTest").ToDictionary(
        e => (string?)e.Attribute("id") ?? "",
        e => (string?)e.Element(ns + "TestMethod")?.Attribute("codeBase") ?? "");
    foreach (var result in doc.Descendants(ns + "UnitTestResult"))
    {
        var testName = (string?)result.Attribute("testName") ?? "";
        var codeBase = codeBases.GetValueOrDefault((string?)result.Attribute("testId") ?? "", "");
        var project = codeBase.Length > 0 ? Path.GetFileNameWithoutExtension(codeBase.Replace('\\', '/')) : Path.GetFileNameWithoutExtension(file);
        var paren = testName.IndexOf('(');
        var methodFqn = paren < 0 ? testName : testName[..paren];
        var arguments = paren < 0 ? "" : testName[(paren + 1)..].TrimEnd(')');
        var duration = TimeSpan.TryParse((string?)result.Attribute("duration"), CultureInfo.InvariantCulture, out var d) ? d : TimeSpan.Zero;
        rows.Add(new Row(project, testName, methodFqn, arguments, (string?)result.Attribute("outcome") ?? "",
            duration.TotalMilliseconds, (string?)result.Attribute("startTime") ?? "", (string?)result.Attribute("endTime") ?? ""));
    }
}
rows = rows.OrderBy(r => r.Project, StringComparer.Ordinal).ThenBy(r => r.Test, StringComparer.Ordinal).ToList();

var csv = new StringBuilder();
csv.AppendLine(string.Join(',', meta.Select(m => m.Name).Concat(["project", "test", "method_fqn", "args", "outcome", "duration_ms", "start_time", "end_time"])));
foreach (var row in rows)
{
    var fields = meta.Select(m => m.Value).Concat([row.Project, row.Test, row.MethodFqn, row.Args, row.Outcome,
        row.DurationMs.ToString("0.###", CultureInfo.InvariantCulture), row.Start, row.End]);
    csv.AppendLine(string.Join(',', fields.Select(Csv)));
}
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
File.WriteAllText(output, csv.ToString().ReplaceLineEndings("\n"), new UTF8Encoding(false));

var failed = rows.Count(r => r.Outcome is not ("Passed" or "NotExecuted"));
var header = string.Create(CultureInfo.InvariantCulture,
    $"{rows.Count} tests from {trxFiles.Count} TRX file(s), commit {Short(sha)} (#{commitIndex}), target {Target()}, total {rows.Sum(r => r.DurationMs) / 1000:0.00} s, failed {failed}");
Console.WriteLine(header);
Console.WriteLine($"{"duration_ms",12}  {"outcome",-11}  test");
foreach (var row in rows)
    Console.WriteLine($"{row.DurationMs.ToString("0.000", CultureInfo.InvariantCulture),12}  {row.Outcome,-11}  {row.Test}");
Console.WriteLine($"Wrote {output}");

if (Env("GITHUB_STEP_SUMMARY") is { } summaryPath)
{
    var md = new StringBuilder();
    md.AppendLine($"### Test durations ({Target()})");
    md.AppendLine();
    md.AppendLine(header);
    md.AppendLine();
    md.AppendLine("| Test | Outcome | Duration (ms) |");
    md.AppendLine("|---|---|---:|");
    foreach (var row in rows.OrderByDescending(r => r.DurationMs))
        md.AppendLine($"| `{row.Test.Replace("|", "\\|")}` | {row.Outcome} | {row.DurationMs.ToString("0.0", CultureInfo.InvariantCulture)} |");
    File.AppendAllText(summaryPath, md.ToString());
}

return failed == 0 ? 0 : 1;

static string? Env(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v.Trim() : null;

static string Short(string sha) => sha.Length > 7 ? sha[..7] : sha;

static string Csv(string value) =>
    value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;

static string Target()
{
    var os = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
    var arch = RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.Arm64 => "arm64",
        var other => other.ToString().ToLowerInvariant(),
    };
    return $"{os}-{arch}";
}

static string CpuModel()
{
    try
    {
        if (OperatingSystem.IsLinux() && File.Exists("/proc/cpuinfo"))
        {
            var lines = File.ReadAllLines("/proc/cpuinfo");
            var model = lines.FirstOrDefault(l => l.StartsWith("model name", StringComparison.Ordinal))
                        ?? lines.FirstOrDefault(l => l.StartsWith("Model", StringComparison.Ordinal));
            if (model is not null) return model[(model.IndexOf(':') + 1)..].Trim();
            var part = lines.FirstOrDefault(l => l.StartsWith("CPU part", StringComparison.Ordinal));
            if (part is not null) return "arm " + part[(part.IndexOf(':') + 1)..].Trim();
        }
        if (OperatingSystem.IsMacOS()) return Run("sysctl", "-n", "machdep.cpu.brand_string") ?? "";
        if (OperatingSystem.IsWindows())
            return Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", null) as string
                   ?? Env("PROCESSOR_IDENTIFIER") ?? "";
    }
    catch (Exception)
    {
        // Metadata only; never fail the summary because of it.
    }
    return "";
}

static string? Git(params string[] arguments) => Run("git", arguments);

static string? Run(string fileName, params string[] arguments)
{
    try
    {
        var psi = new ProcessStartInfo(fileName) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var a in arguments) psi.ArgumentList.Add(a);
        using var process = Process.Start(psi);
        if (process is null) return null;
        var text = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return process.ExitCode == 0 && text.Length > 0 ? text : null;
    }
    catch (Exception)
    {
        return null;
    }
}

record Row(string Project, string Test, string MethodFqn, string Args, string Outcome, double DurationMs, string Start, string End);
