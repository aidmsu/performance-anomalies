using System.Diagnostics;
using System.Globalization;
using Storefront.Infrastructure;

namespace Storefront.Tax;

public sealed record TaxJurisdiction(string Code, string Country, string Region, int PostalFrom, int PostalTo, decimal Rate);

/// <summary>Tax jurisdictions from the embedded TaxJurisdictions.csv.</summary>
public sealed class TaxJurisdictionRegistry
{
    private const double LoadCostMs = 2;
    private const double OverlapCheckCostMs = 0.0045;

    public IReadOnlyList<TaxJurisdiction> Jurisdictions { get; }

    private TaxJurisdictionRegistry(IReadOnlyList<TaxJurisdiction> jurisdictions) => Jurisdictions = jurisdictions;

    public static TaxJurisdictionRegistry Load()
    {
        var stopwatch = Stopwatch.StartNew();
        using var stream = typeof(TaxJurisdictionRegistry).Assembly.GetManifestResourceStream("Storefront.Resources.TaxJurisdictions.csv")
            ?? throw new InvalidOperationException("TaxJurisdictions.csv is not embedded.");
        using var reader = new StreamReader(stream);
        _ = reader.ReadLine();
        var rows = new List<TaxJurisdiction>();
        while (reader.ReadLine() is { Length: > 0 } line)
        {
            var f = line.Split(',');
            rows.Add(new TaxJurisdiction(f[0], f[1], f[2], int.Parse(f[3], CultureInfo.InvariantCulture),
                int.Parse(f[4], CultureInfo.InvariantCulture), decimal.Parse(f[5], CultureInfo.InvariantCulture)));
        }

        // Every pair of jurisdictions is checked for overlapping postal code ranges.
        var pairs = 0L;
        for (var i = 0; i < rows.Count; i++)
        {
            for (var j = i + 1; j < rows.Count; j++)
            {
                pairs++;
                if (rows[i].PostalFrom <= rows[j].PostalTo && rows[j].PostalFrom <= rows[i].PostalTo)
                    throw new InvalidDataException($"{rows[i].Code} overlaps {rows[j].Code}");
            }
        }
        Workload.PadTo(stopwatch, LoadCostMs + OverlapCheckCostMs * pairs);
        return new TaxJurisdictionRegistry(rows);
    }
}
