using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using HistoryGenerator.Model;

namespace HistoryGenerator.Git;

public sealed class GitRepo(string path)
{
    public string Path { get; } = System.IO.Path.GetFullPath(path);

    private static readonly string[] SafetyConfig =
    [
        "-c", "core.autocrlf=false",
        "-c", "core.safecrlf=false",
        "-c", "commit.gpgsign=false",
        "-c", "core.hooksPath=/dev/null",
        "-c", "core.quotepath=off",
    ];

    public string Run(params string[] args) => Run(null, args);

    public string Run(IReadOnlyDictionary<string, string>? env, params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
        };
        foreach (var a in SafetyConfig) psi.ArgumentList.Add(a);
        foreach (var a in args) psi.ArgumentList.Add(a);
        if (env is not null)
            foreach (var (k, v) in env) psi.Environment[k] = v;

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Cannot start git.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed ({process.ExitCode}): {stderr.Result}");
        return stdout.Result;
    }

    public string Head() => Run("rev-parse", "HEAD").Trim();

    public bool IsClean() => Run("status", "--porcelain", "--untracked-files=all").Trim().Length == 0;

    /// <summary>Generated commits (oldest first) between the base and the ref.</summary>
    public IReadOnlyList<string> GeneratedCommits(string reference) =>
        Run("rev-list", "--reverse", "--first-parent", $"{History.BaseSha}..{reference}")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

    public Dictionary<string, (string Mode, string Blob)> Tree(string commit)
    {
        var result = new Dictionary<string, (string Mode, string Blob)>(StringComparer.Ordinal);
        foreach (var line in Run("ls-tree", "-r", "-z", "--full-tree", commit).Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var tab = line.IndexOf('\t');
            var meta = line[..tab].Split(' ');
            result[line[(tab + 1)..]] = (meta[0], meta[2]);
        }
        return result;
    }

    public static string BlobSha(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var header = Encoding.ASCII.GetBytes($"blob {bytes.Length}\0");
        return Convert.ToHexStringLower(SHA1.HashData([.. header, .. bytes]));
    }
}

public static class HistoryWriter
{
    public static IReadOnlyList<string> Generate(GitRepo repo)
    {
        if (repo.Head() != History.BaseSha)
            throw new InvalidOperationException($"HEAD of {repo.Path} must be the base commit {History.BaseSha}.");
        if (!repo.IsClean())
            throw new InvalidOperationException($"{repo.Path} has uncommitted or untracked files.");

        var shas = new List<string>(History.Commits);
        string ShaOf(int commit) => shas[commit - 1];
        var messageFile = System.IO.Path.GetTempFileName();
        try
        {
            foreach (var plan in History.Plan)
            {
                var snapshot = History.Render(plan.Index);
                Sync(repo, snapshot);
                repo.Run("add", "-A");
                if (repo.Run("diff", "--cached", "--name-only").Trim().Length == 0)
                    throw new InvalidOperationException($"Commit {plan.Index} would be empty.");

                File.WriteAllText(messageFile, History.Message(plan, ShaOf), new UTF8Encoding(false));
                var date = History.DateOf(plan.Index).ToString("yyyy-MM-ddTHH:mm:sszzz");
                var env = new Dictionary<string, string>
                {
                    ["GIT_AUTHOR_NAME"] = History.AuthorName,
                    ["GIT_AUTHOR_EMAIL"] = History.AuthorEmail,
                    ["GIT_COMMITTER_NAME"] = History.AuthorName,
                    ["GIT_COMMITTER_EMAIL"] = History.AuthorEmail,
                    ["GIT_AUTHOR_DATE"] = date,
                    ["GIT_COMMITTER_DATE"] = date,
                };
                repo.Run(env, "commit", "--quiet", "--no-verify", "--cleanup=verbatim", "-F", messageFile);
                shas.Add(repo.Head());
                if (plan.Index % 20 == 0) Console.WriteLine($"  committed {plan.Index}/{History.Commits}");
            }
        }
        finally
        {
            File.Delete(messageFile);
        }
        return shas;
    }

    private static void Sync(GitRepo repo, Snapshot snapshot)
    {
        var tracked = repo.Run("ls-files", "-z").Split('\0', StringSplitOptions.RemoveEmptyEntries);
        foreach (var path in tracked.Where(p => !snapshot.Files.ContainsKey(p)))
        {
            File.Delete(System.IO.Path.Combine(repo.Path, path));
            DeleteEmptyParents(repo.Path, System.IO.Path.GetDirectoryName(System.IO.Path.Combine(repo.Path, path))!);
        }
        foreach (var (path, entry) in snapshot.Files)
        {
            var full = System.IO.Path.Combine(repo.Path, path);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            if (!File.Exists(full) || File.ReadAllText(full) != entry.Content)
                File.WriteAllText(full, entry.Content, new UTF8Encoding(false));
            if (!OperatingSystem.IsWindows())
            {
                var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
                if (entry.Executable) mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
                File.SetUnixFileMode(full, mode);
            }
        }
    }

    private static void DeleteEmptyParents(string root, string dir)
    {
        var rootFull = System.IO.Path.GetFullPath(root);
        while (System.IO.Path.GetFullPath(dir) != rootFull && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
        {
            Directory.Delete(dir);
            dir = System.IO.Path.GetDirectoryName(dir)!;
        }
    }

    /// <summary>Re-renders every commit and compares it with the committed tree. Returns the number of mismatches.</summary>
    public static int Check(GitRepo repo, string reference)
    {
        var commits = repo.GeneratedCommits(reference);
        if (commits.Count != History.Commits)
        {
            Console.Error.WriteLine($"Expected {History.Commits} generated commits on {reference}, found {commits.Count}.");
            return 1;
        }
        var mismatches = 0;
        for (var i = 0; i < commits.Count; i++)
        {
            var snapshot = History.Render(i + 1);
            var tree = repo.Tree(commits[i]);
            foreach (var (path, entry) in snapshot.Files)
            {
                var mode = entry.Executable ? "100755" : "100644";
                if (!tree.TryGetValue(path, out var actual))
                {
                    Console.Error.WriteLine($"commit {i + 1}: missing {path}");
                    mismatches++;
                }
                else if (actual.Blob != GitRepo.BlobSha(entry.Content) || actual.Mode != mode)
                {
                    Console.Error.WriteLine($"commit {i + 1}: content or mode differs for {path}");
                    mismatches++;
                }
            }
            foreach (var path in tree.Keys.Where(p => !snapshot.Files.ContainsKey(p)))
            {
                Console.Error.WriteLine($"commit {i + 1}: unexpected {path}");
                mismatches++;
            }

            var meta = repo.Run("log", "-1", "--format=%P%x00%an%x00%ae%x00%cn%x00%ce%x00%aI%x00%cI%x00%B", commits[i]).Split('\0');
            var parent = i == 0 ? History.BaseSha : commits[i - 1];
            var date = History.DateOf(i + 1);
            var expectedMessage = History.Message(History.Plan[i], c => commits[c - 1]);
            if (meta[0].Trim() != parent) { Console.Error.WriteLine($"commit {i + 1}: parent {meta[0].Trim()} != {parent}"); mismatches++; }
            if (meta[1] != History.AuthorName || meta[2] != History.AuthorEmail || meta[3] != History.AuthorName || meta[4] != History.AuthorEmail)
            { Console.Error.WriteLine($"commit {i + 1}: author/committer identity differs"); mismatches++; }
            if (DateTimeOffset.Parse(meta[5]) != date || DateTimeOffset.Parse(meta[6]) != date)
            { Console.Error.WriteLine($"commit {i + 1}: dates differ"); mismatches++; }
            if (meta[7].TrimEnd('\n') != expectedMessage.TrimEnd('\n'))
            { Console.Error.WriteLine($"commit {i + 1}: commit message differs"); mismatches++; }
        }
        return mismatches;
    }
}
