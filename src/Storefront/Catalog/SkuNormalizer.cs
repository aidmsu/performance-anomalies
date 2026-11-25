using System.Text;
using Storefront.Infrastructure;

namespace Storefront.Catalog;

public static class SkuNormalizer
{
    private const double NormalizeCostMs = 0.9;

    public static string Normalize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Compute.Run("normalize-sku", NormalizeCostMs);
        var text = input.Normalize(NormalizationForm.FormKC).Trim();
        return text.ToUpperInvariant().Replace(' ', '-');
    }
}
