using Storefront.Infrastructure;

namespace Storefront.Sessions;

public sealed record CustomerSession(string SessionId, string CustomerId, IReadOnlyList<string> CartSkus);

public sealed class SessionStore
{
    private const double CacheReadCostMs = 100;
    private const double SessionTableFallbackCostMs = 100;
    private const double CacheHitRatio = 0.5;

    public CustomerSession Restore(string sessionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        SimulatedNetwork.Call("session-cache/get", CacheReadCostMs);
        if (Workload.Random("SessionStore.Restore").NextDouble() >= CacheHitRatio)
        {
            // Cache miss: the session was written before the cache was populated.
            SimulatedDatabase.Query("select payload from sessions where id = @id", SessionTableFallbackCostMs);
        }
        return new CustomerSession(sessionId, "customer-" + sessionId[^4..], ["HOME-000001-01"]);
    }
}
