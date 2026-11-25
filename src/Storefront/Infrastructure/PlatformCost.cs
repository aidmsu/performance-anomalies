namespace Storefront.Infrastructure;

/// <summary>Picks the cost of an operation whose price depends on the operating system.</summary>
public static class PlatformCost
{
    public static double Select(double linux, double windows, double macOS) =>
        OperatingSystem.IsWindows() ? windows : OperatingSystem.IsMacOS() ? macOS : linux;
}
