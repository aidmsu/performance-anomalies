using Storefront.Infrastructure;

namespace Storefront.Tax;

public static class TaxCalculator
{
    private const double RuleLookupCostMs = 16.5;

    private static readonly IReadOnlyDictionary<string, decimal> StandardRates = new Dictionary<string, decimal>
    {
        ["DE"] = 0.19m, ["FR"] = 0.20m, ["NL"] = 0.21m, ["PL"] = 0.23m, ["AT"] = 0.20m, ["BE"] = 0.21m,
        ["CZ"] = 0.21m, ["DK"] = 0.25m, ["ES"] = 0.21m, ["IT"] = 0.22m, ["PT"] = 0.23m, ["SE"] = 0.25m,
    };

    public static decimal Calculate(string region, decimal net)
    {
        SimulatedDatabase.Query("select * from tax_rules where region = @region", RuleLookupCostMs);
        return Math.Round(net * StandardRates[region], 2);
    }
}
