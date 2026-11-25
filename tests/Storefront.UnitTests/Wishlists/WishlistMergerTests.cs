using Storefront.Wishlists;

namespace Storefront.UnitTests.Wishlists;

public sealed class WishlistMergerTests
{
    [Fact]
    public void Merges_guest_and_account_wishlists()
    {
        var merged = new WishlistMerger().Merge(["TOYS-000001-01", "HOME-000002-01"], ["HOME-000002-01"]);

        Assert.Equal(2, merged.Count);
    }
}
