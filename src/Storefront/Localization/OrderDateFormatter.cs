using System.Globalization;
using Storefront.Infrastructure;

namespace Storefront.Localization;

/// <summary>Formats order dates for e-mails and invoices in the customer's language.</summary>
public static class OrderDateFormatter
{
    private const double ResourceLookupCostMs = 20;
    private const double TurkishNormalizationCostMs = 70;

    public static string Format(DateOnly date, string cultureName)
    {
        ArgumentException.ThrowIfNullOrEmpty(cultureName);
        var culture = CultureInfo.GetCultureInfo(cultureName);
        SimulatedStorage.Read($"i18n/{culture.Name}/dates.json", ResourceLookupCostMs);
        var text = date.ToDateTime(TimeOnly.MinValue).ToString("D", culture);
        if (culture.TwoLetterISOLanguageName == "tr")
        {
            // Month names may come from templates with ASCII I; normalize them per locale rules.
            Compute.Run("normalize-turkish-i", TurkishNormalizationCostMs);
            text = text.Normalize(System.Text.NormalizationForm.FormC);
        }
        return text;
    }
}
