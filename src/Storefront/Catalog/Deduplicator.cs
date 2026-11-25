using System.Diagnostics;
using Storefront.Infrastructure;

namespace Storefront.Catalog;

public sealed record CatalogItem(int ProductId, string Variant);

/// <summary>Removes duplicate items from merchandiser uploads.</summary>
public static class Deduplicator
{
    private const double FixedCostMs = 2;
    private const double PerItemCostMs = 0.005;
    private const double PerComparisonCostMs = 0.00001;

    public static IReadOnlyList<CatalogItem> RemoveDuplicates(IReadOnlyList<CatalogItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var stopwatch = Stopwatch.StartNew();
        var result = new List<CatalogItem>(items.Count);
        foreach (var item in items)
        {
            if (!ContainsKey(result, KeyOf(item))) result.Add(item);
        }
        var cost = FixedCostMs + PerItemCostMs * items.Count;
        cost += PerComparisonCostMs * items.Count * (double)items.Count / 2;
        Workload.PadTo(stopwatch, cost);
        return result;
    }

    private static (int, string) KeyOf(CatalogItem item) => (item.ProductId, item.Variant);

    private static bool ContainsKey(List<CatalogItem> kept, (int, string) key)
    {
        foreach (var existing in kept)
        {
            if (KeyOf(existing) == key) return true;
        }
        return false;
    }
}
