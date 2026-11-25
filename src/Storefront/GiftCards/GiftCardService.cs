using Storefront.Infrastructure;

namespace Storefront.GiftCards;

public sealed class GiftCardService
{
    private const double BalanceUpdateCostMs = 30;
    private const double VelocityCheckCostMs = 45;

    private readonly Dictionary<string, decimal> _balances = new(StringComparer.Ordinal) { ["GC-1000"] = 50m };

    public decimal Redeem(string code, decimal amount)
    {
        SimulatedDatabase.Query("select count(*) from gift_card_redemptions where code = @code and at > now() - interval '1 hour'", VelocityCheckCostMs);
        SimulatedDatabase.Execute("update gift_cards set balance = balance - @amount where code = @code", BalanceUpdateCostMs);
        var balance = _balances[code];
        var redeemed = Math.Min(balance, amount);
        _balances[code] = balance - redeemed;
        return redeemed;
    }
}
