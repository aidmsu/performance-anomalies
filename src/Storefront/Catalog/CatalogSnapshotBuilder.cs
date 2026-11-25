using Storefront.Infrastructure;

namespace Storefront.Catalog;

public sealed record SnapshotPartition(string Name, bool Changed);

public sealed class CatalogSnapshotBuilder
{
    private const double PartitionRebuildCostMs = 40;

    public int Build(IReadOnlyList<SnapshotPartition> partitions)
    {
        ArgumentNullException.ThrowIfNull(partitions);
        var rebuilt = 0;
        foreach (var partition in partitions)
        {
            SimulatedDatabase.Query($"select * from catalog where partition = '{partition.Name}'", PartitionRebuildCostMs);
            rebuilt++;
        }
        return rebuilt;
    }
}
