using Storefront.Catalog;

namespace Storefront.UnitTests.Catalog;

public sealed class SkuBatchParserTests
{
    [Fact]
    public void Parses_sku_batch()
    {
        var (valid, invalid) = new SkuBatchParser().Parse(["ELEC-000001-01", "bad", "HOME-000042-03"]);

        Assert.Equal(2, valid.Count);
        Assert.Equal(["bad"], invalid);
    }
}
