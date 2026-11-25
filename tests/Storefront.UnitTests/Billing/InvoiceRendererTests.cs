using System.Text;
using Storefront.Billing;

namespace Storefront.UnitTests.Billing;

public sealed class InvoiceRendererTests
{
    [Fact]
    public void Renders_invoice_pdf()
    {
        var invoice = new Invoice("INV-1001", [new InvoiceLine("Desk lamp", 2, 39.90m), new InvoiceLine("Bulb", 4, 3.25m)]);

        var pdf = new InvoiceRenderer().RenderPdf(invoice);

        var text = Encoding.ASCII.GetString(pdf);
        Assert.StartsWith("%PDF", text, StringComparison.Ordinal);
        Assert.Contains("Desk lamp x2 79.80", text, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF\n", text, StringComparison.Ordinal);
    }
}
