using Storefront.Infrastructure;

namespace Storefront.Notifications;

public sealed record OrderConfirmation(string OrderId, string Email, string Language);

/// <summary>Sends transactional e-mails.</summary>
public sealed class NotificationDispatcher
{
    private const double SmtpSubmitCostMs = 20;
    private const double TemplateEngineCostMs = 130;

    public string SendOrderConfirmation(OrderConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        SimulatedNetwork.Call("template-engine/render/order-confirmation", TemplateEngineCostMs);
        var subject = $"[{confirmation.Language}] Order {confirmation.OrderId} confirmed";
        SimulatedNetwork.Call("smtp-relay", SmtpSubmitCostMs);
        return subject;
    }
}
