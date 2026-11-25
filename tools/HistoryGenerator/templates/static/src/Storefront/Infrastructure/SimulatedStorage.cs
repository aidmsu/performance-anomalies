namespace Storefront.Infrastructure;

/// <summary>Stand-in for the file system and blob storage.</summary>
public static class SimulatedStorage
{
    public static void Read(string path, double costMs)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        Workload.Spend(costMs);
    }

    public static void Write(string path, double costMs)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        Workload.Spend(costMs);
    }
}
