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
        return xml.ToString();
    }
}
