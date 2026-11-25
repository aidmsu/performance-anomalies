using Storefront.Infrastructure;

namespace Storefront.Payments;

public sealed record CardPayment(string CardNumber, decimal Amount, string Currency);

public sealed record Authorization(bool Approved, string Reference, string Issuer);

public sealed class PaymentGateway
{
    private const double AuthorizeCostMs = 35;
    private const double IssuerRegistryCostMs = 315;

    public Authorization Authorize(CardPayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        var bin = payment.CardNumber[..4];
        SimulatedNetwork.Call($"issuer-registry/bins/{bin}", IssuerRegistryCostMs);
        var issuer = bin == "4111" ? "Visa test issuer" : "unknown";
        SimulatedNetwork.Call("acquirer/authorize", AuthorizeCostMs);
        return new Authorization(payment.Amount < 10_000m, $"AUTH-{payment.CardNumber[^4..]}", issuer);
    }
}
