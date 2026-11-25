using Storefront.Data;

namespace Storefront.UnitTests.Data;

public sealed class ConnectionPoolTests
{
    [Fact]
    public void Leases_connection()
    {
        var connection = new ConnectionPool().Lease("orders");

        Assert.Equal(1, connection.Id);
        Assert.Equal("orders", connection.Database);
    }
}
