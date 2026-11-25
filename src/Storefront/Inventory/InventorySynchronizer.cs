using Storefront.Infrastructure;

namespace Storefront.Inventory;

public sealed class InventorySynchronizer
{
    private const double SyncCostMs = 80;

    public IReadOnlyDictionary<string, int> Sync(IReadOnlyDictionary<string, int> warehouseLevels)
    {
        ArgumentNullException.ThrowIfNull(warehouseLevels);
        SimulatedNetwork.Call("warehouse-feed/await-latest", 0);
        SimulatedDatabase.Execute("merge into stock_levels using @levels", SyncCostMs);
        return warehouseLevels.ToDictionary(kv => kv.Key, kv => Math.Max(kv.Value, 0), StringComparer.Ordinal);
    }
}
