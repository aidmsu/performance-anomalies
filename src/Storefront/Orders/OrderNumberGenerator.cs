using System.Globalization;
using Storefront.Infrastructure;

namespace Storefront.Orders;

/// <summary>Reserves blocks of order numbers.</summary>
public sealed class OrderNumberGenerator
{
    private const double ReserveBlockCostMs = 200;
    private const double ArchiveCheckCostMs = 22;

    private long _next = 100_000;

    public IReadOnlyList<string> NextBatch(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        SimulatedDatabase.Execute("update order_number_blocks set next = next + @count", ReserveBlockCostMs);
        SimulatedDatabase.Query("select 1 from archive.order_numbers where number between @from and @to", ArchiveCheckCostMs);
        var batch = Enumerable.Range(0, count).Select(i => "SO-" + (_next + i).ToString(CultureInfo.InvariantCulture)).ToList();
        _next += count;
        return batch;
    }
}
