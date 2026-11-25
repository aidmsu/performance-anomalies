using Storefront.GiftCards;

namespace Storefront.UnitTests.GiftCards;

public sealed class GiftCardServiceTests
{
    [Fact]
    public void Redeems_gift_card()
    {
        var service = new GiftCardService();

        Assert.Equal(50m, service.Redeem("GC-1000", 80m));
    }
}
