using System.IO.Compression;

namespace Storefront.Feeds;

/// <summary>Compresses product feeds before they are published.</summary>
public static class FeedCompressor
{
    private static readonly string[] Variants = ["default", "partners"];

    public static IReadOnlyDictionary<string, int> Compress(byte[] feed)
    {
        ArgumentNullException.ThrowIfNull(feed);
        var sizes = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var variant in Variants)
        {
            using var buffer = new MemoryStream();
            using (var deflate = new DeflateStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
            {
                deflate.Write(feed);
            }
            sizes[variant] = (int)buffer.Length;
        }
        return sizes;
    }
}
