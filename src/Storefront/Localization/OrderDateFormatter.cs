using System.Globalization;
using Storefront.Infrastructure;

namespace Storefront.Localization;

/// <summary>Formats order dates for e-mails and invoices in the customer's language.</summary>
public static class OrderDateFormatter
{
    private const double ResourceLookupCostMs = 20;

    public static string Format(DateOnly date, string cultureName)
    {
        ArgumentException.ThrowIfNullOrEmpty(cultureName);
        var culture = CultureInfo.GetCultureInfo(cultureName);
        SimulatedStorage.Read($"i18n/{culture.Name}/dates.json", ResourceLookupCostMs);
        var text = date.ToDateTime(TimeOnly.MinValue).ToString("D", culture);
        return text;
    }
}
