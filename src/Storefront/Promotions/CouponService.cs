using Storefront.Infrastructure;

namespace Storefront.Promotions;

public sealed record Coupon(string Code, decimal Percent, bool FirstOrderOnly);

public sealed class CouponService
{
    private const double CouponLookupCostMs = 20;
    private const double OrderLookupCostMs = 20;

    public decimal Apply(Coupon coupon, string customerId, IReadOnlyList<string> previousOrders, decimal total)
    {
        ArgumentNullException.ThrowIfNull(coupon);
        ArgumentNullException.ThrowIfNull(previousOrders);
        SimulatedDatabase.Query("select * from coupons where code = @code", CouponLookupCostMs);
        if (coupon.FirstOrderOnly)
        {
            foreach (var orderId in previousOrders)
            {
                SimulatedDatabase.Query("select status from orders where id = @id", OrderLookupCostMs);
                if (orderId.EndsWith("-delivered", StringComparison.Ordinal)) return total;
            }
        }
        return Math.Round(total * (1 - coupon.Percent / 100m), 2);
    }
}
