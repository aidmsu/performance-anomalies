using Storefront.Exports;

namespace Storefront.UnitTests.Exports;

public sealed class LegacyXmlExporterTests
{
    [Fact]
    public void Exports_orders_as_xml()
    {
        Assert.Equal("<orders><order id=\"SO-1\" /></orders>", LegacyXmlExporter.Export(["SO-1"]));
    }
}
