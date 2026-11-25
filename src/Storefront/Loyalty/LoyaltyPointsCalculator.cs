using Storefront.Infrastructure;

namespace Storefront.Loyalty;

public sealed class LoyaltyPointsCalculator
{
    private const double LedgerWriteCostMs = 50;

    public int Accrue(string customerId, decimal orderTotal)
    {
        ArgumentException.ThrowIfNullOrEmpty(customerId);
        var points = (int)Math.Floor(orderTotal);
        SimulatedDatabase.Execute("insert into points_ledger values (@id, @points)", LedgerWriteCostMs);
        return points;
    }
}
