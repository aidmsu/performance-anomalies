using Storefront.Infrastructure;

namespace Storefront.Media;

public sealed record Thumbnail(string Path, int Width, int Height, string ColorProfile);

public sealed class ThumbnailGenerator
{
    private const double ResizeCostMs = 50;

    public Thumbnail Generate(string imagePath, int maxSize)
    {
        ArgumentException.ThrowIfNullOrEmpty(imagePath);
        Compute.Run("resize-image", ResizeCostMs);
        var profile = "sRGB";
        return new Thumbnail(imagePath + ".thumb.jpg", maxSize, maxSize * 3 / 4, profile);
    }
}
