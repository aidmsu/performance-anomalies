using Storefront.Imports;

namespace Storefront.IntegrationTests.Imports;

public sealed class SupplierBulkImporterTests
{
    [Fact]
    public void Imports_large_batch()
    {
        var rows = Enumerable.Range(1, 3000).Select(i => new SupplierRow($"SUPP-{i:000000}-01", i % 40, 2.5m)).ToList();

        Assert.Equal(3000, new SupplierBulkImporter().Import(rows));
    }
}
