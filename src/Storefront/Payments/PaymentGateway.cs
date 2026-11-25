using Storefront.Infrastructure;

namespace Storefront.Payments;

public sealed record CardPayment(string CardNumber, decimal Amount, string Currency);

public sealed record Authorization(bool Approved, string Reference, string Issuer);

public sealed class PaymentGateway
{
    private const double AuthorizeCostMs = 35;

    private static readonly IReadOnlyDictionary<string, string> LocalBinTable = new Dictionary<string, string>
    {
        ["4111"] = "Visa test issuer",
        ["5555"] = "Mastercard test issuer",
    };

    public Authorization Authorize(CardPayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        var bin = payment.CardNumber[..4];
        var issuer = LocalBinTable.GetValueOrDefault(bin, "unknown");
        SimulatedNetwork.Call("acquirer/authorize", AuthorizeCostMs);
        return new Authorization(payment.Amount < 10_000m, $"AUTH-{payment.CardNumber[^4..]}", issuer);
    }
}
