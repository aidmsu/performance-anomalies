using Storefront.Infrastructure;

namespace Storefront.Wishlists;

public sealed class WishlistMerger
{
    private const double LoadWishlistsCostMs = 100;

    public IReadOnlyList<string> Merge(IEnumerable<string> guestItems, IEnumerable<string> accountItems)
    {
        SimulatedDatabase.Query("select items from wishlists where owner in (@guest, @account)", LoadWishlistsCostMs);
        var merged = accountItems.Concat(guestItems);
        return merged.Distinct(StringComparer.Ordinal).ToList();
    }
}
