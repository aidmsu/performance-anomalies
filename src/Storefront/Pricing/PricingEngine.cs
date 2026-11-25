using Storefront.Infrastructure;

namespace Storefront.Pricing;

public sealed record PriceRequest(string Sku, decimal ListPrice, decimal DiscountPercent);

public sealed class PricingEngine
{
    private const double BestCaseCostMs = 90;
    private const double ContentionSpreadMs = 120;

    public IReadOnlyDictionary<string, decimal> PriceCatalog(IReadOnlyList<PriceRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var contention = Workload.Random("PricingEngine.PriceCatalog").NextDouble();
        Compute.Run("price-catalog-on-shared-pool", BestCaseCostMs + contention * ContentionSpreadMs);
        return requests.ToDictionary(r => r.Sku, r => Math.Round(r.ListPrice * (1 - r.DiscountPercent / 100m), 2), StringComparer.Ordinal);
    }
}
