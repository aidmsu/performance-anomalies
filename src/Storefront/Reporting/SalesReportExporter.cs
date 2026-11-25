using System.Globalization;
using System.Text;
using Storefront.Infrastructure;

namespace Storefront.Reporting;

public sealed record SalesLine(string Sku, string TaxCategory, int Quantity, decimal Net);

/// <summary>Exports the daily sales report for finance.</summary>
public sealed class SalesReportExporter
{
    private const double QueryCostMs = 60;
    private const double UploadCostMs = 20;

    public string ExportCsv(IReadOnlyList<SalesLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        SimulatedDatabase.Query("select * from sales where day = current_date", QueryCostMs);
        var csv = new StringBuilder("sku,quantity,net\n");
        foreach (var line in lines)
        {
            csv.Append(CultureInfo.InvariantCulture, $"{line.Sku},{line.Quantity},{line.Net:0.00}\n");
        }
        SimulatedStorage.Write("reports/sales.csv", UploadCostMs);
        return csv.ToString();
    }
}
