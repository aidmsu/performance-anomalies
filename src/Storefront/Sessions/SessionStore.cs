using Storefront.Infrastructure;

namespace Storefront.Sessions;

public sealed record CustomerSession(string SessionId, string CustomerId, IReadOnlyList<string> CartSkus);

public sealed class SessionStore
{
    private const double SessionTableReadCostMs = 100;

    public CustomerSession Restore(string sessionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        SimulatedDatabase.Query("select payload from sessions where id = @id", SessionTableReadCostMs);
        return new CustomerSession(sessionId, "customer-" + sessionId[^4..], ["HOME-000001-01"]);
    }
}
