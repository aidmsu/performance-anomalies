using Storefront.Catalog;

namespace Storefront.UnitTests.Catalog;

public sealed class DeduplicatorTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Removes_duplicates(int itemCount)
    {
        var distinct = itemCount - itemCount / 10;
        var items = Enumerable.Range(0, itemCount).Select(i => new CatalogItem(i % distinct, "default")).ToList();

        var result = Deduplicator.RemoveDuplicates(items);

        Assert.Equal(distinct, result.Count);
        Assert.Equal(0, result[0].ProductId);
    }
}
