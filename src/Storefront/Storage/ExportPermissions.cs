using Storefront.Infrastructure;

namespace Storefront.Storage;

/// <summary>Restricts access to partner export folders.</summary>
public static class ExportPermissions
{
    private const double AclWriteCostMs = 40;

    public static int Apply(string root, IReadOnlyList<string> directories)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentNullException.ThrowIfNull(directories);
        SimulatedStorage.Write(root + "/.acl", AclWriteCostMs);
        var updated = 1;
        return updated;
    }
}
