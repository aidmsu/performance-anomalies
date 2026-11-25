using Storefront.Infrastructure;

namespace Storefront.Pricing;

public sealed record PriceRequest(string Sku, decimal ListPrice, decimal DiscountPercent);

public sealed class PricingEngine
{
    private const double PricingCostMs = 150;

    public IReadOnlyDictionary<string, decimal> PriceCatalog(IReadOnlyList<PriceRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        Compute.Run("price-catalog", PricingCostMs);
        return requests.ToDictionary(r => r.Sku, r => Math.Round(r.ListPrice * (1 - r.DiscountPercent / 100m), 2), StringComparer.Ordinal);
    }
}
