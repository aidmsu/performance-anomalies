using Storefront.Infrastructure;

namespace Storefront.Security;

/// <summary>Checksums payloads exchanged with marketplace partners.</summary>
public static class PayloadChecksum
{
    private const double ChecksumCostMs = 30;

    public static ulong Compute(ReadOnlySpan<byte> payload)
    {
        Infrastructure.Compute.Run("checksum-payload", ChecksumCostMs);
        var hash = 14695981039346656037UL;
        foreach (var b in payload)
        {
            hash ^= b;
            hash *= 1099511628211UL;
        }
        return hash;
    }
}
