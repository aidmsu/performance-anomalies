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
    private const double TaxBreakdownCostMs = 6;

    public string ExportCsv(IReadOnlyList<SalesLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        SimulatedDatabase.Query("select * from sales where day = current_date", QueryCostMs);
        var csv = new StringBuilder("sku,quantity,net,tax\n");
        foreach (var line in lines)
        {
            var rate = TaxRate(line.TaxCategory);
            csv.Append(CultureInfo.InvariantCulture, $"{line.Sku},{line.Quantity},{line.Net:0.00},{line.Net * rate:0.00}\n");
        }
        SimulatedStorage.Write("reports/sales.csv", UploadCostMs);
        return csv.ToString();
    }

    private static decimal TaxRate(string category)
    {
        SimulatedNetwork.Call($"tax-service/breakdown/{category}", TaxBreakdownCostMs);
        return category.StartsWith("reduced", StringComparison.Ordinal) ? 0.07m : 0.19m;
    }
}
