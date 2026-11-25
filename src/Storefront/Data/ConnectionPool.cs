using Storefront.Infrastructure;

namespace Storefront.Data;

public sealed record PooledConnection(int Id, string Database);

public sealed class ConnectionPool
{
    private const double LeaseCostMs = 50;
    private const double ReconnectCostMs = 350;
    private const double StaleConnectionProbability = 0.15;

    private int _nextId;

    public PooledConnection Lease(string database)
    {
        ArgumentException.ThrowIfNullOrEmpty(database);
        SimulatedDatabase.Query("select 1", LeaseCostMs);
        if (Workload.Random("ConnectionPool.Lease").NextDouble() < StaleConnectionProbability)
        {
            // The validation query failed: the server closed the idle connection.
            SimulatedNetwork.Call($"db/{database}/connect", ReconnectCostMs);
        }
        return new PooledConnection(Interlocked.Increment(ref _nextId), database);
    }
}
