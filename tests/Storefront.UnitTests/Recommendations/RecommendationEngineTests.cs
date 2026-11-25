using Storefront.Recommendations;

namespace Storefront.UnitTests.Recommendations;

public sealed class RecommendationEngineTests
{
    [Fact]
    public void Returns_top_recommendations()
    {
        var top = new RecommendationEngine().TopFor("customer-7", 5);

        Assert.Equal(5, top.Count);
        Assert.All(top, sku => Assert.StartsWith("SKU-", sku, StringComparison.Ordinal));
    }
}
