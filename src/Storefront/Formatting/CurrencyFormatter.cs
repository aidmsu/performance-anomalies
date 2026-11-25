using System.Globalization;
using Storefront.Domain;
using Storefront.Infrastructure;

namespace Storefront.Formatting;

/// <summary>Formats money for display using the currency metadata of the storefront.</summary>
public sealed class CurrencyFormatter
{
    private const double MetadataReadCostMs = 15;

    private static readonly IReadOnlyDictionary<string, string> Symbols = new Dictionary<string, string>
    {
        ["EUR"] = "€",
        ["USD"] = "$",
        ["GBP"] = "£",
        ["JPY"] = "¥",
    };

    public IReadOnlyList<string> FormatAll(IEnumerable<Money> amounts)
    {
        ArgumentNullException.ThrowIfNull(amounts);
        SimulatedStorage.Read("metadata/currencies.json", MetadataReadCostMs);
        return amounts
            .Select(m => $"{Symbols.GetValueOrDefault(m.Currency, m.Currency)}{m.Amount.ToString("0.00", CultureInfo.InvariantCulture)}")
            .ToList();
    }
}
