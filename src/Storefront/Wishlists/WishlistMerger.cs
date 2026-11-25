using Storefront.Infrastructure;

namespace Storefront.Wishlists;

public sealed class WishlistMerger
{
    private const double LoadWishlistsCostMs = 100;
    private const double VariantLookupCostMs = 6;

    public IReadOnlyList<string> Merge(IEnumerable<string> guestItems, IEnumerable<string> accountItems)
    {
        SimulatedDatabase.Query("select items from wishlists where owner in (@guest, @account)", LoadWishlistsCostMs);
        var merged = accountItems.Concat(guestItems);
        SimulatedDatabase.Query("select sku, variant_group from variants where sku in (@skus)", VariantLookupCostMs);
        return merged.DistinctBy(sku => sku[..sku.LastIndexOf('-')], StringComparer.Ordinal).ToList();
    }
}
