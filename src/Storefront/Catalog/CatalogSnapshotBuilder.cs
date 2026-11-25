using Storefront.Infrastructure;

namespace Storefront.Catalog;

public sealed record SnapshotPartition(string Name, bool Changed);

public sealed class CatalogSnapshotBuilder
{
    private const double PartitionRebuildCostMs = 40;
    private const double PartitionCopyCostMs = 0.5;

    public int Build(IReadOnlyList<SnapshotPartition> partitions)
    {
        ArgumentNullException.ThrowIfNull(partitions);
        var rebuilt = 0;
        foreach (var partition in partitions)
        {
            if (!partition.Changed)
            {
                SimulatedStorage.Read($"snapshots/previous/{partition.Name}", PartitionCopyCostMs);
                continue;
            }
            SimulatedDatabase.Query($"select * from catalog where partition = '{partition.Name}'", PartitionRebuildCostMs);
            rebuilt++;
        }
        return rebuilt;
    }
}
