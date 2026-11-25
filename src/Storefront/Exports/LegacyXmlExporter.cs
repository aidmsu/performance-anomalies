using System.Xml.Linq;
using Storefront.Infrastructure;

namespace Storefront.Exports;

/// <summary>XML order export for the old ERP connector.</summary>
public static class LegacyXmlExporter
{
    private const double OrderQueryCostMs = 90;

    public static string Export(IEnumerable<string> orderIds)
    {
        SimulatedDatabase.Query("select * from orders where id in (@ids)", OrderQueryCostMs);
        return new XElement("orders", orderIds.Select(id => new XElement("order", new XAttribute("id", id)))).ToString(SaveOptions.DisableFormatting);
    }
}
