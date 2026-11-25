using Storefront.Orders;

namespace Storefront.UnitTests.Orders;

public sealed class OrderApiTests
{
    [Fact]
    public void Returns_order_summary()
    {
        var json = new OrderApi().GetSummary("SO-100042");

        Assert.Contains("\"orderId\":\"SO-100042\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Returns_order_details()
    {
        var json = new OrderApi().GetDetails("SO-100042");

        Assert.Contains("\"orderId\":\"SO-100042\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Returns_order_history()
    {
        var json = new OrderApi().GetHistory("SO-100042");

        Assert.Contains("\"orderId\":\"SO-100042\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Returns_order_export()
    {
        var json = new OrderApi().GetExport("SO-100042");

        Assert.Contains("\"orderId\":\"SO-100042\"", json, StringComparison.Ordinal);
    }
}
