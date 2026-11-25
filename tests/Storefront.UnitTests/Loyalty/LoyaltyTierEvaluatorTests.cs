using Storefront.Loyalty;

namespace Storefront.UnitTests.Loyalty;

public sealed class LoyaltyTierEvaluatorTests
{
    [Fact]
    public void Computes_tier_for_customer()
    {
        Assert.Equal(LoyaltyTier.Silver, new LoyaltyTierEvaluator().Evaluate("customer-1", 750m));
    }
}
