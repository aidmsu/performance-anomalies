using Storefront.Infrastructure;

namespace Storefront.Returns;

public sealed record ReturnRequest(string OrderId, int DaysSinceDelivery, bool Opened, string PostalCode);

public sealed record ReturnDecision(bool Accepted, bool PickupAvailable);

public sealed class ReturnsPolicy
{
    private const double OrderLookupCostMs = 90;

    public ReturnDecision Evaluate(ReturnRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        SimulatedDatabase.Query("select * from order_lines where order_id = @id", OrderLookupCostMs);
        var accepted = request.DaysSinceDelivery <= 30 && !request.Opened;
        return new ReturnDecision(accepted, PickupAvailable: false);
    }
}
