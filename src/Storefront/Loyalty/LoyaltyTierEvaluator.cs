using Storefront.Infrastructure;

namespace Storefront.Loyalty;

public enum LoyaltyTier { Bronze, Silver, Gold }

public sealed class LoyaltyTierEvaluator
{
    private const double OrderHistoryCostMs = 100;

    public LoyaltyTier Evaluate(string customerId, decimal yearlySpend)
    {
        ArgumentException.ThrowIfNullOrEmpty(customerId);
        SimulatedDatabase.Query("select sum(total) from orders where customer_id = @id", OrderHistoryCostMs);
        var tier = yearlySpend >= 2000 ? LoyaltyTier.Gold : yearlySpend >= 500 ? LoyaltyTier.Silver : LoyaltyTier.Bronze;
        return tier;
    }
}
