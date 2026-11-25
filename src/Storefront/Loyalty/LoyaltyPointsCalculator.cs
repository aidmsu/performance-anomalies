using Storefront.Infrastructure;

namespace Storefront.Loyalty;

public sealed class LoyaltyPointsCalculator
{
    private const double LedgerWriteCostMs = 50;
    private const double TierBonusQueryCostMs = 60;

    public int Accrue(string customerId, decimal orderTotal)
    {
        ArgumentException.ThrowIfNullOrEmpty(customerId);
        var points = (int)Math.Floor(orderTotal);
        SimulatedDatabase.Query("select tier, bonus_rate from loyalty_tiers where customer_id = @id", TierBonusQueryCostMs);
        points += points / 10;
        SimulatedDatabase.Execute("insert into points_ledger values (@id, @points)", LedgerWriteCostMs);
        return points;
    }
}
