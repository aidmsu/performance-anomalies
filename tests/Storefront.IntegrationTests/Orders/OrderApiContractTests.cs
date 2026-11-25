using Storefront.Orders;

namespace Storefront.IntegrationTests.Orders;

public sealed class OrderApiContractTests
{
    [Fact]
    public void Matches_order_contract()
    {
        var json = new OrderApi().GetContract("SO-100042");

        Assert.Contains("\"orderId\":\"SO-100042\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Matches_bulk_order_contract()
    {
        var json = new OrderApi().GetBulkContract("SO-100042");

        Assert.Contains("\"orderId\":\"SO-100042\"", json, StringComparison.Ordinal);
    }
}
