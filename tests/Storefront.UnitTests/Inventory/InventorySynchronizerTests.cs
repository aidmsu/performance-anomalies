using Storefront.Inventory;

namespace Storefront.UnitTests.Inventory;

public sealed class InventorySynchronizerTests
{
    [Fact]
    public void Syncs_stock_levels()
    {
        var levels = new InventorySynchronizer().Sync(new Dictionary<string, int> { ["HOME-000001-01"] = 4, ["TOYS-000002-01"] = -1 });

        Assert.Equal(0, levels["TOYS-000002-01"]);
    }
}
