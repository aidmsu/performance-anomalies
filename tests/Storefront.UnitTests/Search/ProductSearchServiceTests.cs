using Storefront.Search;

namespace Storefront.UnitTests.Search;

public sealed class ProductSearchServiceTests
{
    [Fact]
    public void Finds_products_by_keyword()
    {
        var hits = new ProductSearchService().Search("lamp");

        Assert.Equal(30, hits.Count);
    }
}
