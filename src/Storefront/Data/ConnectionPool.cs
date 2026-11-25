using Storefront.Infrastructure;

namespace Storefront.Data;

public sealed record PooledConnection(int Id, string Database);

public sealed class ConnectionPool
{
    private const double LeaseCostMs = 50;

    private int _nextId;

    public PooledConnection Lease(string database)
    {
        ArgumentException.ThrowIfNullOrEmpty(database);
        SimulatedDatabase.Query("select 1", LeaseCostMs);
        return new PooledConnection(Interlocked.Increment(ref _nextId), database);
    }
}
