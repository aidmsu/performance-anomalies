using Storefront.Media;

namespace Storefront.UnitTests.Media;

public sealed class ThumbnailGeneratorTests
{
    [Fact]
    public void Generates_thumbnail()
    {
        var thumbnail = new ThumbnailGenerator().Generate("products/lamp.jpg", 320);

        Assert.Equal(240, thumbnail.Height);
        Assert.NotEmpty(thumbnail.ColorProfile);
    }
}
