using Storefront.Catalog;

namespace Storefront.IntegrationTests.Catalog;

public sealed class CatalogFeedImporterTests
{
    [Fact]
    public void Imports_catalog_feed()
    {
        var rows = Enumerable.Range(1, 30)
            .Select(i => new CatalogFeedRow($"HOME-{i:000000}-01", $"Product {i}", i == 30 ? 0m : 9.99m + i))
            .ToList();

        var result = new CatalogFeedImporter().Import(rows);

        Assert.Equal(29, result.Imported);
        Assert.Equal(["HOME-000030-01"], result.Rejected);
    }
}
