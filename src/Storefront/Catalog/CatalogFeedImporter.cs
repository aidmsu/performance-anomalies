using Storefront.Infrastructure;

namespace Storefront.Catalog;

public sealed record CatalogFeedRow(string Sku, string Title, decimal Price);

public sealed record FeedImportResult(int Imported, IReadOnlyList<string> Rejected);

/// <summary>Imports the nightly product feed from the merchandising system.</summary>
public sealed class CatalogFeedImporter
{
    private const double ParseCostMs = 20;
    private const double BulkUpsertCostMs = 50;

    public FeedImportResult Import(IReadOnlyList<CatalogFeedRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        Compute.Run("parse-catalog-feed", ParseCostMs);
        var rejected = rows.Where(r => r.Price <= 0).Select(r => r.Sku).ToList();
        SimulatedDatabase.Execute("merge into catalog using @rows", BulkUpsertCostMs);
        return new FeedImportResult(rows.Count - rejected.Count, rejected);
    }
}
