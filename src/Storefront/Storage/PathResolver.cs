using Storefront.Infrastructure;

namespace Storefront.Storage;

/// <summary>Resolves user-supplied export paths inside the storage root.</summary>
public static class PathResolver
{
    private const double ManagedCanonicalizationCostMs = 60;

    public static string Resolve(string root, string relative)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentException.ThrowIfNullOrEmpty(relative);
        Compute.Run("canonicalize-path", ManagedCanonicalizationCostMs);
        var segments = new Stack<string>();
        foreach (var part in relative.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == "..") { if (segments.Count > 0) segments.Pop(); }
            else if (part != ".") segments.Push(part);
        }
        return root.TrimEnd('/') + "/" + string.Join('/', segments.Reverse());
    }
}
