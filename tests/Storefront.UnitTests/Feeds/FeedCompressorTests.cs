using System.Text;
using Storefront.Feeds;

namespace Storefront.UnitTests.Feeds;

/// <summary>Builds the 3 MB sample feed once, outside the measured test.</summary>
public sealed class ProductFeedFixture
{
    public byte[] Feed { get; }

    public ProductFeedFixture()
    {
        var feed = new StringBuilder();
        for (var i = 0; i < 60_000; i++)
        {
            feed.Append("<item sku=\"HOME-").Append((i * 7919 % 1_000_000).ToString("000000")).Append("-01\" price=\"")
                .Append(i % 997).Append(".99\" stock=\"").Append(i % 13).Append("\"/>\n");
        }
        Feed = Encoding.UTF8.GetBytes(feed.ToString());
    }
}

public sealed class FeedCompressorTests(ProductFeedFixture fixture) : IClassFixture<ProductFeedFixture>
{
    [Fact]
    public void Compresses_product_feed()
    {
        var sizes = FeedCompressor.Compress(fixture.Feed);

        Assert.All(sizes.Values, size => Assert.InRange(size, 1, fixture.Feed.Length / 2));
    }
}
