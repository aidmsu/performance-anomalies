using Storefront.Infrastructure;

namespace Storefront.Inventory;

public sealed class InventorySynchronizer
{
    private const double SyncCostMs = 80;
    private const double StaleFeedProbability = 0.5;

    public IReadOnlyDictionary<string, int> Sync(IReadOnlyDictionary<string, int> warehouseLevels)
    {
        ArgumentNullException.ThrowIfNull(warehouseLevels);
        SimulatedDatabase.Execute("merge into stock_levels using @levels", SyncCostMs);
        if (Workload.Random("InventorySynchronizer.Sync").NextDouble() >= StaleFeedProbability)
        {
            // The warehouse feed was still being written; sync again with the complete file.
            SimulatedDatabase.Execute("merge into stock_levels using @levels", SyncCostMs);
        }
        return warehouseLevels.ToDictionary(kv => kv.Key, kv => Math.Max(kv.Value, 0), StringComparer.Ordinal);
    }
}
