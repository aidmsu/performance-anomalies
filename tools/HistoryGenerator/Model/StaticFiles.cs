namespace HistoryGenerator.Model;

/// <summary>Files copied verbatim from templates/static into every commit.</summary>
public static class StaticFiles
{
    private static IReadOnlyList<(string Path, string Content)>? _files;

    public static string Root => Path.Combine(AppContext.BaseDirectory, "templates", "static");

    public static IReadOnlyList<(string Path, string Content)> Files => _files ??= Load();

    private static List<(string Path, string Content)> Load()
    {
        if (!Directory.Exists(Root)) throw new DirectoryNotFoundException(Root);
        return Directory.EnumerateFiles(Root, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 })
            .Select(f => (Path: Path.GetRelativePath(Root, f).Replace('\\', '/'), Content: File.ReadAllText(f)))
            .Where(f => !f.Path.EndsWith(".DS_Store", StringComparison.Ordinal))
            .OrderBy(f => f.Path, StringComparer.Ordinal)
            .ToList();
    }

    public static bool IsFillerArea(string path) =>
        path.StartsWith("docs/", StringComparison.Ordinal) ||
        path.StartsWith("src/Storefront/Admin/", StringComparison.Ordinal);

    public static void AddTo(Snapshot snapshot)
    {
        foreach (var (path, content) in Files)
        {
            snapshot.Add(path, content,
                IsFillerArea(path) ? Snapshot.FillerArea : Snapshot.StaticOwner,
                executable: path.EndsWith(".sh", StringComparison.Ordinal));
        }
    }
}
