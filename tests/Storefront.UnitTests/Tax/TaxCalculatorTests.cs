using Storefront.Tax;

namespace Storefront.UnitTests.Tax;

public sealed class TaxCalculatorTests
{
    private static readonly string[] Regions = ["DE", "FR", "NL"];

    [Fact]
    public void Calculates_tax_for_all_regions()
    {
        foreach (var region in Regions)
        {
            var tax = TaxCalculator.Calculate(region, 100m);

            Assert.InRange(tax, 15m, 30m);
        }
    }
}
