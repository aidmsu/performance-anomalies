using Storefront.Shipping;

namespace Storefront.UnitTests.Shipping;

public sealed class ShippingLabelPrinterTests
{
    [Fact]
    public void Prints_shipping_label()
    {
        var label = new ShippingLabelPrinter().Print(new LabelRequest("SO-7", "national-post", "Ada Lovelace", 1.2m));

        Assert.Equal("^XA", label[0]);
        Assert.Equal("^XZ", label[^1]);
    }
}
