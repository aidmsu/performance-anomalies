using Storefront.Catalog;

namespace Storefront.UnitTests.Catalog;

public sealed class SkuNormalizerTests
{
    [Fact]
    public void Normalizes_sku_input()
    {
        Assert.Equal("HOME-000001-01", SkuNormalizer.Normalize("  home 000001 01 "));
    }
}
