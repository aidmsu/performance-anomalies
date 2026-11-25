using Storefront.Inventory;

namespace Storefront.UnitTests.Inventory;

public sealed class WarehouseAllocatorTests
{
    [Fact]
    public void Allocates_stock_across_warehouses()
    {
        var allocation = new WarehouseAllocator().Allocate(7, [new WarehouseStock("berlin", 5), new WarehouseStock("hamburg", 4)]);

        Assert.Equal(5, allocation["berlin"]);
        Assert.Equal(2, allocation["hamburg"]);
    }
}
