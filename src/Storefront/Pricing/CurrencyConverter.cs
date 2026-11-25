using Storefront.Domain;
using Storefront.Infrastructure;

namespace Storefront.Pricing;

/// <summary>Converts order totals using the daily exchange rates.</summary>
public static class CurrencyConverter
{
    private const double RateLookupCostMs = 40;

    private static readonly IReadOnlyDictionary<string, (decimal Rate, int Decimals)> Rates = new Dictionary<string, (decimal, int)>
    {
        ["USD"] = (1.08m, 2),
        ["EUR"] = (1m, 2),
        ["JPY"] = (162.4m, 0),
        ["KWD"] = (0.332m, 3),
    };

    public static Money Convert(Money amount, string currency)
    {
        var (rate, decimals) = Rates[currency];
        SimulatedNetwork.Call($"fx-rates/{amount.Currency}/{currency}", RateLookupCostMs);
        return new Money(Math.Round(amount.Amount * rate, decimals, MidpointRounding.ToEven), currency);
    }
}
