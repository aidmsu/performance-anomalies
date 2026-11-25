using Storefront.Infrastructure;

namespace Storefront.Cart;

public sealed record CartLine(string Sku, int Quantity, decimal UnitPrice, bool Bulky);

public sealed record CartTotals(decimal Subtotal, decimal Shipping)
{
    public decimal Total => Subtotal + Shipping;
}

public sealed class CartTotalsCalculator
{
    private const double LinePricingCostMs = 5;
    private const double ShippingEstimateCostMs = 2;

    public CartTotals Calculate(IReadOnlyList<CartLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var subtotal = 0m;
        var shipping = 0m;
        foreach (var line in lines)
        {
            SimulatedDatabase.Query("select price from price_list where sku = @sku", LinePricingCostMs);
            subtotal += line.Quantity * line.UnitPrice;
            SimulatedNetwork.Call("shipping-estimates", ShippingEstimateCostMs);
            shipping += line.Bulky ? 19.00m : 0.50m;
        }
        return new CartTotals(subtotal, Math.Max(shipping, 4.90m));
    }
}
