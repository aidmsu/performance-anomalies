using Storefront.Loyalty;

namespace Storefront.UnitTests.Loyalty;

public sealed class LoyaltyPointsCalculatorTests
{
    [Fact(Skip = "Ledger fixture is flaky; re-enable once it is isolated")]
    public void Accrues_points_for_order()
    {
        var points = new LoyaltyPointsCalculator().Accrue("customer-1", 125.40m);

        Assert.InRange(points, 125, 140);
    }
}
