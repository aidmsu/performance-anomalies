namespace Storefront.Infrastructure;

/// <summary>Stand-in for CPU-heavy work (rendering, parsing, hashing large payloads).</summary>
public static class Compute
{
    public static void Run(string operation, double costMs)
    {
        ArgumentException.ThrowIfNullOrEmpty(operation);
        Workload.Spend(costMs);
    }
}
