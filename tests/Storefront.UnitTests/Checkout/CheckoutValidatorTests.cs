using Storefront.Checkout;

namespace Storefront.UnitTests.Checkout;

public sealed class CheckoutValidatorTests
{
    [Fact]
    public void Validates_complete_checkout()
    {
        var violations = new CheckoutValidator().Validate(new CheckoutRequest("customer-1", "DE", ["HOME-000001-01"], 59.90m));

        Assert.Empty(violations);
    }
}
