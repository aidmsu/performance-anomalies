using Storefront.Infrastructure;

namespace Storefront.Media;

/// <summary>Pre-renders image renditions into the CDN cache after a product image changes.</summary>
public sealed class ImageCacheWarmer
{
    private const double RenditionCostMs = 8;

    private static readonly string[] Renditions =
    [
        "legacy-240",
        "legacy-160",
        "legacy-120",
        "legacy-100",
        "legacy-80",
        "legacy-64",
        "legacy-48",
        "legacy-32",
        "legacy-24",
        "legacy-16",
        "zoom-2048",
        "zoom-1600",
        "detail-1200",
        "detail-900",
        "card-600",
        "card-400",
        "list-300",
        "list-200",
        "icon-96",
        "icon-48",
    ];

    public int Warm(string productImage)
    {
        ArgumentException.ThrowIfNullOrEmpty(productImage);
        foreach (var rendition in Renditions)
        {
            SimulatedNetwork.Call($"cdn/prewarm/{rendition}/{productImage}", RenditionCostMs);
        }
        return Renditions.Length;
    }
}
