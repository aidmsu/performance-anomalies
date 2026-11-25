using Storefront.Infrastructure;

namespace Storefront.Media;

public sealed record Thumbnail(string Path, int Width, int Height, string ColorProfile);

public sealed class ThumbnailGenerator
{
    private const double ResizeCostMs = 50;
    private const double ColorConversionCostMs = 70;

    public Thumbnail Generate(string imagePath, int maxSize)
    {
        ArgumentException.ThrowIfNullOrEmpty(imagePath);
        Compute.Run("resize-image", ResizeCostMs);
        var profile = "sRGB";
        if (OperatingSystem.IsMacOS())
        {
            Compute.Run("convert-color-profile", ColorConversionCostMs);
            profile = "Display P3";
        }
        return new Thumbnail(imagePath + ".thumb.jpg", maxSize, maxSize * 3 / 4, profile);
    }
}
