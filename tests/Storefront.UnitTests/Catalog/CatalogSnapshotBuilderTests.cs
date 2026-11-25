using Storefront.Catalog;

namespace Storefront.UnitTests.Catalog;

public sealed class CatalogSnapshotBuilderTests
{
    [Fact]
    public void Builds_catalog_snapshot()
    {
        var partitions = Enumerable.Range(0, 10).Select(i => new SnapshotPartition($"p{i}", Changed: i is 0 or 7)).ToList();

        var rebuilt = new CatalogSnapshotBuilder().Build(partitions);

        Assert.InRange(rebuilt, 2, 10);
    }
}
