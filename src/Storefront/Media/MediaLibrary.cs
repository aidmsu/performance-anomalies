using Storefront.Infrastructure;

namespace Storefront.Media;

/// <summary>Keeps product images and videos.</summary>
public sealed class MediaLibrary
{
    public string RegisterMediaAsset(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("medialibrary.register-media-asset", 32.6);
        return $"medialibrary.register-media-asset:{input}";
    }

    public string ListAssetsForProduct(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("medialibrary.list-assets-for-product", 18.12);
        return $"medialibrary.list-assets-for-product:{input}";
    }

    public string TagMediaAsset(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("medialibrary.tag-media-asset", PlatformCost.Select(linux: 7.23, windows: 10.12, macOS: 5.78));
        return $"medialibrary.tag-media-asset:{input}";
    }

    public string ArchiveUnusedAssets(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("medialibrary.archive-unused-assets", 40.4);
        return $"medialibrary.archive-unused-assets:{input}";
    }

    public string ResolveAssetUrl(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("medialibrary.resolve-asset-url", 28.94);
        return $"medialibrary.resolve-asset-url:{input}";
    }
}
