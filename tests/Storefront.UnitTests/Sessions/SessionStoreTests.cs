using Storefront.Sessions;

namespace Storefront.UnitTests.Sessions;

public sealed class SessionStoreTests
{
    [Fact]
    public void Restores_customer_session()
    {
        var session = new SessionStore().Restore("sess-00001234");

        Assert.Equal("customer-1234", session.CustomerId);
        Assert.Single(session.CartSkus);
    }
}
