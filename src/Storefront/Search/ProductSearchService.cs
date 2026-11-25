using Storefront.Infrastructure;

namespace Storefront.Search;

public sealed record SearchHit(string Sku, string Title, double Rating);

/// <summary>Keyword search over the product catalog.</summary>
public sealed class ProductSearchService
{
    private const double SearchQueryCostMs = 40;

    private static readonly (string Sku, string Title)[] Catalog = Enumerable.Range(1, 30)
        .Select(i => ($"HOME-{i:000000}-01", i % 2 == 0 ? $"Oak lamp {i}" : $"Brass lamp {i}"))
        .ToArray();

    public IReadOnlyList<SearchHit> Search(string keyword)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyword);
        SimulatedDatabase.Query("select p.sku, p.title, avg(r.rating) from products p left join reviews r using (sku) where p.title ilike @keyword group by p.sku, p.title", SearchQueryCostMs);
        var hits = new List<SearchHit>();
        foreach (var (sku, title) in Catalog)
        {
            if (!title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;
            hits.Add(new SearchHit(sku, title, Rating: sku.Length % 5));
        }
        return hits.OrderByDescending(h => h.Rating).ThenBy(h => h.Sku, StringComparer.Ordinal).ToList();
    }
}
