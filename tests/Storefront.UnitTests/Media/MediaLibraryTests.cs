using Storefront.Media;

namespace Storefront.UnitTests.Media;

public sealed class MediaLibraryTests
{
    [Fact]
    public void Registers_media_asset()
    {
        var result = new MediaLibrary().RegisterMediaAsset("sample-42");

        Assert.Equal("medialibrary.register-media-asset:sample-42", result);
    }

    [Fact]
    public void Lists_assets_for_product()
    {
        var result = new MediaLibrary().ListAssetsForProduct("sample-42");

        Assert.Equal("medialibrary.list-assets-for-product:sample-42", result);
    }

    [Fact]
    public void Tags_media_asset()
    {
        var result = new MediaLibrary().TagMediaAsset("sample-42");

        Assert.Equal("medialibrary.tag-media-asset:sample-42", result);
    }

    [Fact]
    public void Archives_unused_assets()
    {
        var result = new MediaLibrary().ArchiveUnusedAssets("sample-42");

        Assert.Equal("medialibrary.archive-unused-assets:sample-42", result);
    }

    [Fact]
    public void Resolves_asset_url()
    {
        var result = new MediaLibrary().ResolveAssetUrl("sample-42");

        Assert.Equal("medialibrary.resolve-asset-url:sample-42", result);
    }
}
