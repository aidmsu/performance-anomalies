using Storefront.Localization;

namespace Storefront.UnitTests.Localization;

public sealed class OrderDateFormatterTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ja-JP")]
    public void Formats_order_date(string culture)
    {
        var text = OrderDateFormatter.Format(new DateOnly(2026, 3, 14), culture);

        Assert.Contains("2026", text, StringComparison.Ordinal);
        Assert.Contains("14", text, StringComparison.Ordinal);
    }
}
