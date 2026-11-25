using Storefront.Domain;
using Storefront.Pricing;

namespace Storefront.UnitTests.Pricing;

public sealed class CurrencyConverterTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("JPY")]
    [InlineData("KWD")]
    public void Converts_order_total(string currency)
    {
        var converted = CurrencyConverter.Convert(new Money(100m, "EUR"), currency);

        Assert.Equal(currency, converted.Currency);
        Assert.True(converted.Amount > 0);
    }
}
