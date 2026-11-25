using Storefront.Reporting;

namespace Storefront.UnitTests.Reporting;

public sealed class CsvExporterTests
{
    [Fact]
    public void Writes_csv_export()
    {
        var csv = CsvExporter.Write([["sku", "qty"], ["HOME-000001-01", "3"]]);

        Assert.Equal("sku,qty\nHOME-000001-01,3\n", csv);
    }
}
