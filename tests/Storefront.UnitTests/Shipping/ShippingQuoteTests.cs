using Storefront.Shipping;

namespace Storefront.UnitTests.Shipping;

public sealed class ShippingQuoteTests
{
    [Fact]
    public void Returns_quote_for_domestic_parcel()
    {
        var quote = new ShippingQuoteService().Quote(new Parcel("DE", "DE", 2.4m));

        Assert.Equal("national-post", quote.Carrier);
        Assert.Equal(8.50m, quote.Price);
    }
}
