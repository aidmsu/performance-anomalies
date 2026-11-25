using Storefront.Security;

namespace Storefront.UnitTests.Security;

public sealed class CatalogHasherTests
{
    [Fact]
    public void Hashes_catalog_export()
    {
        var export = new byte[32 * 1024 * 1024];
        for (var i = 0; i < export.Length; i++) export[i] = (byte)(i * 31 + (i >> 8));

        var first = CatalogHasher.Hash(export);
        var second = CatalogHasher.Hash(export);

        Assert.Equal(64, first.Length);
        Assert.Equal(first, second);
    }
}
