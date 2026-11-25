namespace Storefront.Infrastructure;

/// <summary>Stand-in for the relational database. Each statement costs the given number of milliseconds.</summary>
public static class SimulatedDatabase
{
    public static void Query(string statement, double costMs)
    {
        ArgumentException.ThrowIfNullOrEmpty(statement);
        Workload.Spend(costMs);
    }

    public static void Execute(string statement, double costMs)
    {
        ArgumentException.ThrowIfNullOrEmpty(statement);
        Workload.Spend(costMs);
    }
}
