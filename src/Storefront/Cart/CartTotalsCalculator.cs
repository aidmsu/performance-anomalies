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
    private const decimal FlatShipping = 4.90m;

    public CartTotals Calculate(IReadOnlyList<CartLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var subtotal = 0m;
        foreach (var line in lines)
        {
            SimulatedDatabase.Query("select price from price_list where sku = @sku", LinePricingCostMs);
            subtotal += line.Quantity * line.UnitPrice;
        }
        return new CartTotals(subtotal, FlatShipping);
    }
}
