using System.Runtime.InteropServices;
using Storefront.Infrastructure;

namespace Storefront.Security;

/// <summary>Checksums payloads exchanged with marketplace partners.</summary>
public static class PayloadChecksum
{
    private const double ChecksumCostMs = 30;
    private const double PortableLoopExtraCostMs = 60;

    public static ulong Compute(ReadOnlySpan<byte> payload)
    {
        Infrastructure.Compute.Run("checksum-payload", ChecksumCostMs);
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            // No vectorized implementation: fall back to the portable table-driven loop.
            Infrastructure.Compute.Run("checksum-payload-portable", PortableLoopExtraCostMs);
        }
        var hash = 14695981039346656037UL;
        foreach (var b in payload)
        {
            hash ^= b;
            hash *= 1099511628211UL;
        }
        return hash;
    }
}
