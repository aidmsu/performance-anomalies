using System.Text;
using Storefront.Infrastructure;

namespace Storefront.Feeds;

public sealed record FeedItem(string Sku, string Title, decimal Price);

/// <summary>Builds the XML product feed for marketplace partners.</summary>
public sealed class ProductFeedBuilder
{
    private const double SerializeCostMs = 60;

    public string Build(IEnumerable<FeedItem> items)
    {
        var xml = new StringBuilder("<feed>");
        foreach (var item in items) xml.Append("<item sku=\"").Append(item.Sku).Append("\"/>");
        xml.Append("</feed>");
        Compute.Run("serialize-feed", SerializeCostMs);
        // Schema validation goes through the platform XML stack, whose speed differs per OS.
        Compute.Run("validate-feed-schema", PlatformCost.Select(linux: 15, windows: 120, macOS: 30));
        return xml.ToString();
    }
}
