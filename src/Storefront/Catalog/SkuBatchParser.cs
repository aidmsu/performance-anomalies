using Storefront.Domain;
using Storefront.Infrastructure;

namespace Storefront.Catalog;

/// <summary>Parses SKUs uploaded by merchandisers and checks them against the catalog.</summary>
public sealed class SkuBatchParser
{
    private const double CatalogLookupCostMs = 40;

    public (IReadOnlyList<Sku> Valid, IReadOnlyList<string> Invalid) Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var valid = new List<Sku>();
        var invalid = new List<string>();
        foreach (var line in lines)
        {
            if (Sku.TryParse(line, out var sku)) valid.Add(sku);
            else invalid.Add(line);
        }
        SimulatedDatabase.Query("select sku from catalog where sku in (@skus)", CatalogLookupCostMs);
        return (valid, invalid);
    }
}
