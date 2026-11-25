using Storefront.Cart;

namespace Storefront.UnitTests.Cart;

public sealed class CartTotalsCalculatorTests
{
    [Fact]
    public void Calculates_totals_for_large_cart()
    {
        var lines = Enumerable.Range(1, 20).Select(i => new CartLine($"HOME-{i:000000}-01", 1, 10m, Bulky: false)).ToList();

        var totals = new CartTotalsCalculator().Calculate(lines);

        Assert.Equal(200m, totals.Subtotal);
        Assert.True(totals.Total > totals.Subtotal);
    }
}
