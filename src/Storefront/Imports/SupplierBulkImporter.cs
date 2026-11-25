using Storefront.Domain;
using Storefront.Infrastructure;

namespace Storefront.Imports;

public sealed record SupplierRow(string Sku, int Quantity, decimal Cost);

/// <summary>Imports full supplier catalogs in batches.</summary>
public sealed class SupplierBulkImporter
{
    private const int BatchSize = 100;
    private const double BatchWriteCostMs = 30;

    public int Import(IReadOnlyList<SupplierRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var imported = 0;
        foreach (var batch in rows.Chunk(BatchSize))
        {
            SimulatedDatabase.Execute("copy supplier_stock from stdin", BatchWriteCostMs);
            imported += batch.Count(r => Sku.TryParse(r.Sku, out _));
        }
        return imported;
    }
}
