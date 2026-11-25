using System.Security.Cryptography;

namespace Storefront.Security;

/// <summary>Fingerprints catalog exports so that partners can verify downloads.</summary>
public static class CatalogHasher
{
    public static string Hash(ReadOnlySpan<byte> export) => Convert.ToHexStringLower(SHA256.HashData(export));
}
