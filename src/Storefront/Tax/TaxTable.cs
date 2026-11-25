using Storefront.Infrastructure;

namespace Storefront.Tax;

/// <summary>VAT rates per country, loaded from the published tax table.</summary>
public sealed class TaxTable
{
    private const double TableDownloadCostMs = 250;

    private readonly Dictionary<string, decimal> _rates;

    private TaxTable(Dictionary<string, decimal> rates) => _rates = rates;

    public static TaxTable Load()
    {
        SimulatedNetwork.Call("https://tax.example.internal/vat-rates", TableDownloadCostMs);
        return new TaxTable(new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["DE"] = 0.19m,
            ["FR"] = 0.20m,
            ["NL"] = 0.21m,
            ["PL"] = 0.23m,
        });
    }

    public decimal RateFor(string country) =>
        _rates.TryGetValue(country, out var rate) ? rate : throw new KeyNotFoundException(country);
}
