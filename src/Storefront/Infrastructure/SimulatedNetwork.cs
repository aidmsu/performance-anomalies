namespace Storefront.Infrastructure;

/// <summary>Stand-in for calls to other services.</summary>
public static class SimulatedNetwork
{
    public static void Call(string endpoint, double costMs)
    {
        ArgumentException.ThrowIfNullOrEmpty(endpoint);
        Workload.Spend(costMs);
    }
}
