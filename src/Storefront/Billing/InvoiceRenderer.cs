using System.Globalization;
using System.Text;
using Storefront.Infrastructure;

namespace Storefront.Billing;

public sealed record InvoiceLine(string Description, int Quantity, decimal UnitPrice);

public sealed record Invoice(string Number, IReadOnlyList<InvoiceLine> Lines);

/// <summary>Renders invoices as PDF documents.</summary>
public sealed class InvoiceRenderer
{
    private const double LayoutCostMs = 3;

    public byte[] RenderPdf(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        Compute.Run("layout-invoice", LayoutCostMs);
        var body = new StringBuilder();
        body.Append("%PDF-1.7\n");
        body.Append(CultureInfo.InvariantCulture, $"Invoice {invoice.Number}\n");
        foreach (var line in invoice.Lines)
        {
            body.Append(CultureInfo.InvariantCulture, $"{line.Description} x{line.Quantity} {line.Quantity * line.UnitPrice:0.00}\n");
        }
        body.Append("%%EOF\n");
        return Encoding.ASCII.GetBytes(body.ToString());
    }
}
