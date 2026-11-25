using Storefront.Media;

namespace Storefront.UnitTests.Media;

public sealed class ImageCacheWarmerTests
{
    [Fact]
    public void Warms_image_cache()
    {
        Assert.True(new ImageCacheWarmer().Warm("products/lamp.jpg") >= 10);
    }
}
