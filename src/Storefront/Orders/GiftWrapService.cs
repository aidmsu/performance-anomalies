using Storefront.Infrastructure;

namespace Storefront.Orders;

public sealed record GiftWrap(string OrderId, string Message, string? PreviewUrl);

public sealed class GiftWrapService
{
    private const double SaveCostMs = 30;

    public GiftWrap Wrap(string orderId, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(orderId);
        SimulatedDatabase.Execute("insert into gift_wraps values (@order, @message)", SaveCostMs);
        return new GiftWrap(orderId, message.Trim(), $"previews/{orderId}.png");
    }
}
