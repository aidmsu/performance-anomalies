using Storefront.Infrastructure;

namespace Storefront.GiftCards;

public sealed class GiftCardService
{
    private const double BalanceUpdateCostMs = 30;

    private readonly Dictionary<string, decimal> _balances = new(StringComparer.Ordinal) { ["GC-1000"] = 50m };

    public decimal Redeem(string code, decimal amount)
    {
        SimulatedDatabase.Execute("update gift_cards set balance = balance - @amount where code = @code", BalanceUpdateCostMs);
        var balance = _balances[code];
        var redeemed = Math.Min(balance, amount);
        _balances[code] = balance - redeemed;
        return redeemed;
    }
}
