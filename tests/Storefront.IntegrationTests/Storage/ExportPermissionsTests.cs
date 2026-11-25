using Storefront.Storage;

namespace Storefront.IntegrationTests.Storage;

public sealed class ExportPermissionsTests
{
    [Fact]
    public void Applies_export_permissions()
    {
        var updated = ExportPermissions.Apply("/exports/partner-7", ["2026", "2026/11", "2026/11/25", "daily", "weekly", "full"]);

        Assert.True(updated >= 1);
    }
}
