using Storefront.Infrastructure;

namespace Storefront.Inventory;

public sealed record WarehouseStock(string Warehouse, int Available);

public sealed class WarehouseAllocator
{
    private const double StockQueryCostMs = 120;

    public IReadOnlyDictionary<string, int> Allocate(int quantity, IReadOnlyList<WarehouseStock> stock)
    {
        ArgumentNullException.ThrowIfNull(stock);
        SimulatedDatabase.Query("select warehouse, available from stock for update", StockQueryCostMs);
        var allocation = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in stock.OrderByDescending(s => s.Available))
        {
            if (quantity == 0) break;
            var take = Math.Min(quantity, s.Available);
            if (take == 0) continue;
            allocation[s.Warehouse] = take;
            quantity -= take;
        }
        return allocation;
    }
}
