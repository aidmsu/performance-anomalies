using Storefront.Domain;
using Storefront.Formatting;

namespace Storefront.UnitTests.Formatting;

public sealed class CurrencyFormatterTests
{
    [Fact]
    public void Formats_amounts_with_currency_symbols()
    {
        var formatted = new CurrencyFormatter().FormatAll([new Money(12.5m, "USD"), new Money(3m, "CHF")]);

        Assert.Equal(["$12.50", "CHF3.00"], formatted);
    }
}
