using Storefront.Payments;

namespace Storefront.UnitTests.Payments;

public sealed class PaymentGatewayTests
{
    [Fact]
    public void Authorizes_card_payment()
    {
        var authorization = new PaymentGateway().Authorize(new CardPayment("4111111111111111", 49.90m, "EUR"));

        Assert.True(authorization.Approved);
        Assert.Equal("AUTH-1111", authorization.Reference);
    }
}
