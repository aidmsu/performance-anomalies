using Storefront.Orders;

namespace Storefront.UnitTests.Orders;

public sealed class OrderNumberGeneratorTests
{
    [Fact]
    public void Generates_order_number_batch()
    {
        var batch = new OrderNumberGenerator().NextBatch(50);

        Assert.Equal(50, batch.Distinct().Count());
        Assert.Equal("SO-100000", batch[0]);
    }
}
