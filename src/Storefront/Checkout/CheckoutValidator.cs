using Storefront.Infrastructure;

namespace Storefront.Checkout;

public sealed record CheckoutRequest(string CustomerId, string Country, IReadOnlyList<string> Skus, decimal Total);

public sealed record ValidationRule(string Id, double CostMs);

/// <summary>Runs every checkout rule before an order is placed.</summary>
public sealed class CheckoutValidator
{
    private const double AddressCheckCostMs = 30;
    private const double RuleCheckCostMs = 4;

    private static readonly ValidationRule[] Rules =
    [
        new("max-quantity-per-sku", RuleCheckCostMs),
        new("embargoed-destination", RuleCheckCostMs),
        new("po-box-courier", RuleCheckCostMs),
        new("age-restricted-items", RuleCheckCostMs),
        new("hazmat-air-shipping", RuleCheckCostMs),
        new("billing-country-match", RuleCheckCostMs),
        new("duplicate-order-window", RuleCheckCostMs),
        new("gift-card-quantity", RuleCheckCostMs),
        new("preorder-separation", RuleCheckCostMs),
        new("business-vat-id", RuleCheckCostMs),
        new("courier-phone-number", RuleCheckCostMs),
        new("cash-on-delivery-limit", RuleCheckCostMs),
        new("student-discount-proof", RuleCheckCostMs),
        new("bundle-integrity", RuleCheckCostMs),
        new("delivery-slot-available", RuleCheckCostMs),
    ];

    public IReadOnlyList<string> Validate(CheckoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var violations = new List<string>();
        SimulatedNetwork.Call("address-verification/check", AddressCheckCostMs);
        if (request.Skus.Count == 0) violations.Add("empty-cart");
        foreach (var rule in Rules)
        {
            SimulatedDatabase.Query($"rule:{rule.Id}", rule.CostMs);
        }
        return violations;
    }
}
