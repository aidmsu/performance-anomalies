using Storefront.Infrastructure;

namespace Storefront.Returns;

public sealed record ReturnRequest(string OrderId, int DaysSinceDelivery, bool Opened, string PostalCode);

public sealed record ReturnDecision(bool Accepted, bool PickupAvailable);

public sealed class ReturnsPolicy
{
    private const double OrderLookupCostMs = 90;
    private const double PickupAvailabilityCostMs = 50;

    public ReturnDecision Evaluate(ReturnRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        SimulatedDatabase.Query("select * from order_lines where order_id = @id", OrderLookupCostMs);
        var accepted = request.DaysSinceDelivery <= 30 && !request.Opened;
        if (!accepted) return new ReturnDecision(false, PickupAvailable: false);
        SimulatedNetwork.Call($"carrier/pickup-slots/{request.PostalCode}", PickupAvailabilityCostMs);
        return new ReturnDecision(true, PickupAvailable: request.PostalCode.Length == 5);
    }
}
