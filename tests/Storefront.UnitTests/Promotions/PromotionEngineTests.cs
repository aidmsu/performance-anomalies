using Storefront.Promotions;

namespace Storefront.UnitTests.Promotions;

public sealed class PromotionEngineTests
{
    [Fact]
    public void Evaluates_promotions_for_cart()
    {
        var promotions = new[] { new Promotion("P1", "total>=50", 5m), new Promotion("P2", "total>=100", 10m) };

        Assert.Equal(5m, new PromotionEngine().Evaluate(promotions, 75m));
    }
}
