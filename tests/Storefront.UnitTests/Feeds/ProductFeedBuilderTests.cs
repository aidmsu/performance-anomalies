using Storefront.Feeds;

namespace Storefront.UnitTests.Feeds;

public sealed class ProductFeedBuilderTests
{
    [Fact]
    public void Builds_product_feed()
    {
        var xml = new ProductFeedBuilder().Build([new FeedItem("HOME-000001-01", "Lamp", 39.9m)]);

        Assert.Equal("<feed><item sku=\"HOME-000001-01\"/></feed>", xml);
    }
}
