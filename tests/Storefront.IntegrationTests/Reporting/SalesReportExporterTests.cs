using Storefront.Reporting;

namespace Storefront.IntegrationTests.Reporting;

public sealed class SalesReportExporterTests
{
    [Fact]
    public void Exports_sales_report_csv()
    {
        var lines = Enumerable.Range(1, 40)
            .Select(i => new SalesLine($"HOME-{i:000000}-01", $"{(i % 2 == 0 ? "reduced" : "standard")}-{i % 5}", i % 3 + 1, 10m * i))
            .ToList();

        var csv = new SalesReportExporter().ExportCsv(lines);

        Assert.Equal(41, csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
