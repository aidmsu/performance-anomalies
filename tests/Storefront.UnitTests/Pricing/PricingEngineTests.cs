using Storefront.Pricing;

namespace Storefront.UnitTests.Pricing;

public sealed class PricingEngineTests
{
    [Fact]
    public void Prices_catalog()
    {
        var prices = new PricingEngine().PriceCatalog([new PriceRequest("HOME-000001-01", 100m, 15m), new PriceRequest("TOYS-000002-01", 9.99m, 0m)]);

        Assert.Equal(85m, prices["HOME-000001-01"]);
        Assert.Equal(9.99m, prices["TOYS-000002-01"]);
    }
}
