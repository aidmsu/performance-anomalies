using Storefront.Orders;

namespace Storefront.UnitTests.Orders;

public sealed class GiftWrapServiceTests
{
    [Fact]
    public void Wraps_order_with_message()
    {
        var wrap = new GiftWrapService().Wrap("SO-7", "  Happy birthday!  ");

        Assert.Equal("Happy birthday!", wrap.Message);
    }
}
