using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Storefront.Domain;

/// <summary>Stock keeping unit in the form CATEGORY-NUMBER-VARIANT, for example ELEC-001234-02.</summary>
public sealed record Sku(string Category, int Number, int Variant)
{
    public static Sku Parse(string text) =>
        TryParse(text, out var sku) ? sku : throw new FormatException($"'{text}' is not a valid SKU.");

    public static bool TryParse(string? text, [NotNullWhen(true)] out Sku? sku)
    {
        sku = null;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var parts = text.Trim().Split('-');
        if (parts.Length != 3 || parts[0].Length is < 2 or > 6 || !parts[0].All(char.IsAsciiLetter)) return false;
        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var number)) return false;
        if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var variant)) return false;
        sku = new Sku(parts[0].ToUpperInvariant(), number, variant);
        return true;
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Category}-{Number:000000}-{Variant:00}");
}
