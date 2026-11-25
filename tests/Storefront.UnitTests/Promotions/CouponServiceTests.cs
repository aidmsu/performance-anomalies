using Storefront.Promotions;

namespace Storefront.UnitTests.Promotions;

public sealed class CouponServiceTests
{
    [Fact]
    public void Applies_percentage_coupon()
    {
        var previousOrders = Enumerable.Range(1, 8).Select(i => $"SO-{i}-cancelled").ToList();

        var total = new CouponService().Apply(new Coupon("WELCOME10", 10m, FirstOrderOnly: true), "customer-1", previousOrders, 80m);

        Assert.Equal(72m, total);
    }
}
