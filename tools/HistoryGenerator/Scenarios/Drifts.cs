using System.Globalization;
using System.Text;
using HistoryGenerator.Model;
using static HistoryGenerator.Model.Fmt;

namespace HistoryGenerator.Scenarios;

/// <summary>Commits on which gradual changes tick: every 4th commit with a given remainder inside a range.</summary>
public static class Ticks
{
    public static IEnumerable<int> Every4(int remainder, int from, int to) =>
        Enumerable.Range(from, to - from + 1).Where(c => c % 4 == remainder);
}

/// <summary>Linear creep: one more checkout validation rule every few commits.</summary>
public sealed class RegGradualLinear : SingleTestScenario
{
    public override string Id => "REG-GRADUAL-LINEAR";
    public override string Category => "drift";
    public override string Summary => "Linear creep on every target: every commit ≡ 1 (mod 4) between 17 and 105 adds a checkout validation rule costing 4 ms, so the test drifts 30 → 122 ms over 23 small steps with no single culprit.";
    public override TestRef Test { get; } = TestRef.Unit("Checkout", "CheckoutValidatorTests", "Validates_complete_checkout");
    private const double BaseMs = 30;
    private const double RuleMs = 4;

    private static readonly (string Id, string Subject)[] Rules =
    [
        ("max-quantity-per-sku", "Limit the quantity per SKU at checkout"),
        ("embargoed-destination", "Block checkout to embargoed destinations"),
        ("po-box-courier", "Reject PO boxes for courier shipments"),
        ("age-restricted-items", "Require age verification for restricted items"),
        ("hazmat-air-shipping", "Keep hazardous items off air shipping"),
        ("billing-country-match", "Check the billing country against the card country"),
        ("duplicate-order-window", "Detect duplicate orders placed within ten minutes"),
        ("gift-card-quantity", "Cap gift card purchases per order"),
        ("preorder-separation", "Separate pre-order items from in-stock items"),
        ("business-vat-id", "Validate VAT IDs of business customers"),
        ("courier-phone-number", "Require a phone number for courier delivery"),
        ("cash-on-delivery-limit", "Limit the value of cash on delivery orders"),
        ("student-discount-proof", "Verify student status for student discounts"),
        ("bundle-integrity", "Keep product bundles complete at checkout"),
        ("delivery-slot-available", "Check that the chosen delivery slot is still free"),
        ("warehouse-cutoff", "Warn when the warehouse cutoff time has passed"),
        ("house-number-present", "Require a house number in delivery addresses"),
        ("export-license", "Require export licenses for controlled goods"),
        ("subscription-overlap", "Prevent overlapping product subscriptions"),
        ("loyalty-redemption-cap", "Cap loyalty point redemption per order"),
        ("marketplace-seller-active", "Check that marketplace sellers are still active"),
        ("parcel-weight-limit", "Enforce carrier parcel weight limits"),
        ("lithium-battery-rules", "Apply lithium battery shipping rules"),
    ];

    private static readonly int[] TickCommits = Ticks.Every4(1, 17, 105).ToArray();

    public override (int Start, int End)? Ramp => (TickCommits[0], TickCommits[^1]);

    public override IReadOnlyList<Change> Changes { get; } =
        TickCommits.Select((c, i) => new Change { Commit = c, Kind = Kinds.RampStep, Tier = Tiers.Should, Subject = Rules[i].Subject }).ToList();

    protected override Dist Model(Target target, int commit) => Dist.Ms(BaseMs + RuleMs * RampSteps(commit));

    public override string Notes => $"Rule commits: {string.Join(", ", TickCommits)}.";

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        if (Rules.Length != TickCommits.Length) throw new InvalidOperationException("Rule list and tick commits differ.");
        var active = Rules.Take(RampSteps(commit)).ToList();
        var rules = active.Count == 0
            ? "    private static readonly ValidationRule[] Rules = [];"
            : "    private static readonly ValidationRule[] Rules =\n    [\n" +
              string.Join("\n", active.Select(r => $"        new(\"{r.Id}\", RuleCheckCostMs),")) + "\n    ];";
        yield return (Paths.Src("Checkout", "CheckoutValidator"), $$"""
            using Storefront.Infrastructure;

            namespace Storefront.Checkout;

            public sealed record CheckoutRequest(string CustomerId, string Country, IReadOnlyList<string> Skus, decimal Total);

            public sealed record ValidationRule(string Id, double CostMs);

            /// <summary>Runs every checkout rule before an order is placed.</summary>
            public sealed class CheckoutValidator
            {
                private const double AddressCheckCostMs = {{N(BaseMs)}};
                private const double RuleCheckCostMs = {{N(RuleMs)}};

            {{rules}}

                public IReadOnlyList<string> Validate(CheckoutRequest request)
                {
                    ArgumentNullException.ThrowIfNull(request);
                    var violations = new List<string>();
                    SimulatedNetwork.Call("address-verification/check", AddressCheckCostMs);
                    if (request.Skus.Count == 0) violations.Add("empty-cart");
                    foreach (var rule in Rules)
                    {
                        SimulatedDatabase.Query($"rule:{rule.Id}", rule.CostMs);
                    }
                    return violations;
                }
            }
            """);
        yield return (Paths.For(Test), """
            using Storefront.Checkout;

            namespace Storefront.UnitTests.Checkout;

            public sealed class CheckoutValidatorTests
            {
                [Fact]
                public void Validates_complete_checkout()
                {
                    var violations = new CheckoutValidator().Validate(new CheckoutRequest("customer-1", "DE", ["HOME-000001-01"], 59.90m));

                    Assert.Empty(violations);
                }
            }
            """);
    }
}

/// <summary>Accelerating creep driven by data growth (no code change): O(n²) over a growing embedded table.</summary>
public sealed class RegGradualAccelerating : SingleTestScenario
{
    public override string Id => "REG-GRADUAL-ACCEL";
    public override string Category => "drift";
    public override string Summary => "Accelerating creep driven only by data: every commit ≡ 3 (mod 4) between 3 and 119 adds six rows to the embedded tax jurisdiction table, whose loader checks every pair of rows for overlaps. Cost 2 + 0.0045·n(n-1)/2 ms grows from about 4 ms (30 rows) to about 101 ms (210 rows), faster towards the end.";
    public override TestRef Test { get; } = TestRef.Unit("Tax", "TaxJurisdictionRegistryTests", "Loads_jurisdiction_registry");
    private const double FixedMs = 2;
    private const double PerPairMs = 0.0045;
    private const int InitialRows = 30;
    private const int RowsPerStep = 6;

    private static readonly string[] InitialRegions = ["Berlin", "Bavaria", "Hesse", "Saxony", "Hamburg"];

    private static readonly string[] Regions =
    [
        "Brittany", "Normandy", "Provence", "Occitanie", "Lombardy", "Tuscany", "Piedmont", "Catalonia", "Andalusia",
        "Galicia", "Flanders", "Wallonia", "Tyrol", "Styria", "Bohemia", "Moravia", "Silesia", "Pomerania", "Zealand",
        "Jutland", "Scania", "Uppland", "Algarve", "Minho", "Holland", "Friesland", "Limburg", "Carinthia", "Lapland",
        "Ostrobothnia",
    ];

    private static readonly int[] TickCommits = Ticks.Every4(3, 3, 119).ToArray();

    public override (int Start, int End)? Ramp => (TickCommits[0], TickCommits[^1]);

    public override IReadOnlyList<Change> Changes { get; } =
        TickCommits.Select((c, i) => new Change { Commit = c, Kind = Kinds.RampStep, Tier = Tiers.Should, Subject = $"Add tax jurisdictions for {Regions[i]}" }).ToList();

    private int Rows(int commit) => InitialRows + RowsPerStep * RampSteps(commit);

    protected override Dist Model(Target target, int commit)
    {
        var n = Rows(commit);
        return Dist.Ms(Math.Round(FixedMs + PerPairMs * n * (n - 1) / 2.0, 4));
    }

    public override string Notes => $"Data-growth commits: {string.Join(", ", TickCommits)}. Rows: {InitialRows} at commit 1, {InitialRows + RowsPerStep * TickCommits.Length} at the end.";

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        if (Regions.Length != TickCommits.Length) throw new InvalidOperationException("Region list and tick commits differ.");
        var csv = new StringBuilder("code,country,region,postal_from,postal_to,rate\n");
        var regions = InitialRegions.SelectMany(r => Enumerable.Repeat(r, RowsPerStep))
            .Concat(Regions.Take(RampSteps(commit)).SelectMany(r => Enumerable.Repeat(r, RowsPerStep)))
            .ToList();
        for (var i = 0; i < regions.Count; i++)
        {
            var from = 10000 + i * 1000;
            var country = i < InitialRows ? "DE" : CountryOf(regions[i]);
            csv.Append(CultureInfo.InvariantCulture,
                $"{country}-{regions[i][..3].ToUpperInvariant()}-{i % RowsPerStep + 1:00},{country},{regions[i]},{from},{from + 999},{(i % 3 == 0 ? "0.07" : "0.19")}\n");
        }
        yield return (Paths.Resource("TaxJurisdictions.csv"), csv.ToString());
        yield return (Paths.Src("Tax", "TaxJurisdictionRegistry"), $$"""
            using System.Diagnostics;
            using System.Globalization;
            using Storefront.Infrastructure;

            namespace Storefront.Tax;

            public sealed record TaxJurisdiction(string Code, string Country, string Region, int PostalFrom, int PostalTo, decimal Rate);

            /// <summary>Tax jurisdictions from the embedded TaxJurisdictions.csv.</summary>
            public sealed class TaxJurisdictionRegistry
            {
                private const double LoadCostMs = {{N(FixedMs)}};
                private const double OverlapCheckCostMs = {{N(PerPairMs)}};

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
            """);
        yield return (Paths.For(Test), """
            using Storefront.Tax;

            namespace Storefront.UnitTests.Tax;

            public sealed class TaxJurisdictionRegistryTests
            {
                [Fact]
                public void Loads_jurisdiction_registry()
                {
                    var registry = TaxJurisdictionRegistry.Load();

                    Assert.True(registry.Jurisdictions.Count >= 30);
                    Assert.Equal(registry.Jurisdictions.Count, registry.Jurisdictions.Select(j => j.PostalFrom).Distinct().Count());
                }
            }
            """);
    }

    private static string CountryOf(string region) => region switch
    {
        "Brittany" or "Normandy" or "Provence" or "Occitanie" => "FR",
        "Lombardy" or "Tuscany" or "Piedmont" => "IT",
        "Catalonia" or "Andalusia" or "Galicia" => "ES",
        "Flanders" or "Wallonia" => "BE",
        "Tyrol" or "Styria" or "Carinthia" => "AT",
        "Bohemia" or "Moravia" => "CZ",
        "Silesia" or "Pomerania" => "PL",
        "Zealand" or "Jutland" => "DK",
        "Scania" or "Uppland" => "SE",
        "Algarve" or "Minho" => "PT",
        "Holland" or "Friesland" or "Limburg" => "NL",
        _ => "FI",
    };
}

/// <summary>Creep below the per-commit noise: +3 ms on a 200 ms test every 4th commit.</summary>
public sealed class RegDriftSubNoise : SingleTestScenario
{
    public override string Id => "REG-DRIFT-SUBNOISE";
    public override string Category => "drift";
    public override string Summary => "Slow creep below the per-commit noise on every target: every commit ≡ 3 (mod 4) between 31 and 79 adds a shipping label field costing 3 ms, so the test drifts 200 → 239 ms in 13 steps (each step about half the run-to-run noise).";
    public override TestRef Test { get; } = TestRef.Unit("Shipping", "ShippingLabelPrinterTests", "Prints_shipping_label");
    private const double BaseMs = 200;
    private const double FieldMs = 3;

    private static readonly (string Field, string Subject)[] Fields =
    [
        ("customs-tariff-code", "Print customs tariff codes on shipping labels"),
        ("sender-phone", "Print the sender phone number on shipping labels"),
        ("return-barcode", "Add a return barcode to shipping labels"),
        ("package-count", "Print the package count on multi-parcel labels"),
        ("delivery-instructions", "Print delivery instructions on shipping labels"),
        ("signature-required", "Mark labels of parcels that need a signature"),
        ("fragile-marker", "Add a fragile marker to shipping labels"),
        ("order-reference", "Print the order reference on shipping labels"),
        ("service-level", "Print the carrier service level on labels"),
        ("weight-imperial", "Print parcel weight in pounds for US carriers"),
        ("country-of-origin", "Print the country of origin on shipping labels"),
        ("insured-value", "Print the insured value on shipping labels"),
        ("recycling-notice", "Add a packaging recycling notice to labels"),
    ];

    private static readonly int[] TickCommits = Ticks.Every4(3, 31, 79).ToArray();

    public override (int Start, int End)? Ramp => (TickCommits[0], TickCommits[^1]);

    public override IReadOnlyList<Change> Changes { get; } =
        TickCommits.Select((c, i) => new Change { Commit = c, Kind = Kinds.RampStep, Tier = Tiers.Should, Subject = Fields[i].Subject }).ToList();

    protected override Dist Model(Target target, int commit) => Dist.Ms(BaseMs + FieldMs * RampSteps(commit));

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        if (Fields.Length != TickCommits.Length) throw new InvalidOperationException("Field list and tick commits differ.");
        var active = Fields.Take(RampSteps(commit)).ToList();
        var fields = active.Count == 0
            ? "    private static readonly string[] ExtraFields = [];"
            : "    private static readonly string[] ExtraFields =\n    [\n" + string.Join("\n", active.Select(f => $"        \"{f.Field}\",")) + "\n    ];";
        yield return (Paths.Src("Shipping", "ShippingLabelPrinter"), $$"""
            using Storefront.Infrastructure;

            namespace Storefront.Shipping;

            public sealed record LabelRequest(string OrderId, string Carrier, string Recipient, decimal WeightKg);

            /// <summary>Renders carrier shipping labels (ZPL).</summary>
            public sealed class ShippingLabelPrinter
            {
                private const double RenderCostMs = {{N(BaseMs)}};
                private const double FieldRenderCostMs = {{N(FieldMs)}};

            {{fields}}

                public IReadOnlyList<string> Print(LabelRequest request)
                {
                    ArgumentNullException.ThrowIfNull(request);
                    Compute.Run("render-label-template", RenderCostMs);
                    var lines = new List<string> { "^XA", $"^FD{request.Recipient}^FS", $"^FD{request.Carrier}^FS" };
                    foreach (var field in ExtraFields)
                    {
                        Compute.Run($"render-label-field-{field}", FieldRenderCostMs);
                        lines.Add($"^FD{field}^FS");
                    }
                    lines.Add("^XZ");
                    return lines;
                }
            }
            """);
        yield return (Paths.For(Test), """
            using Storefront.Shipping;

            namespace Storefront.UnitTests.Shipping;

            public sealed class ShippingLabelPrinterTests
            {
                [Fact]
                public void Prints_shipping_label()
                {
                    var label = new ShippingLabelPrinter().Print(new LabelRequest("SO-7", "national-post", "Ada Lovelace", 1.2m));

                    Assert.Equal("^XA", label[0]);
                    Assert.Equal("^XZ", label[^1]);
                }
            }
            """);
    }
}

/// <summary>Gradual improvement: fewer image renditions are pre-warmed.</summary>
public sealed class OptGradual : SingleTestScenario
{
    public override string Id => "OPT-GRADUAL";
    public override string Category => "drift";
    public override string Summary => "Gradual improvement on every target: every commit ≡ 1 (mod 4) between 33 and 89 stops pre-warming one legacy image rendition (8 ms each), so the test improves 200 → 80 ms in 15 steps.";
    public override TestRef Test { get; } = TestRef.Unit("Media", "ImageCacheWarmerTests", "Warms_image_cache");
    private const double RenditionMs = 8;

    private static readonly string[] Renditions =
    [
        "legacy-1024", "legacy-800", "legacy-640", "legacy-480", "legacy-320", "legacy-240", "legacy-160", "legacy-120",
        "legacy-100", "legacy-80", "legacy-64", "legacy-48", "legacy-32", "legacy-24", "legacy-16",
        "zoom-2048", "zoom-1600", "detail-1200", "detail-900", "card-600", "card-400", "list-300", "list-200", "icon-96", "icon-48",
    ];

    private static readonly int[] TickCommits = Ticks.Every4(1, 33, 89).ToArray();

    public override (int Start, int End)? Ramp => (TickCommits[0], TickCommits[^1]);

    public override IReadOnlyList<Change> Changes { get; } =
        TickCommits.Select((c, i) => new Change
        {
            Commit = c, Kind = Kinds.RampStep, Tier = Tiers.Should, Direction = Directions.Improvement,
            Subject = $"Stop pre-warming {Renditions[i]} image renditions",
        }).ToList();

    protected override Dist Model(Target target, int commit) => Dist.Ms(RenditionMs * (Renditions.Length - RampSteps(commit)));

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        var active = Renditions.Skip(RampSteps(commit)).ToList();
        var list = string.Join("\n", active.Select(r => $"        \"{r}\","));
        yield return (Paths.Src("Media", "ImageCacheWarmer"), $$"""
            using Storefront.Infrastructure;

            namespace Storefront.Media;

            /// <summary>Pre-renders image renditions into the CDN cache after a product image changes.</summary>
            public sealed class ImageCacheWarmer
            {
                private const double RenditionCostMs = {{N(RenditionMs)}};

                private static readonly string[] Renditions =
                [
            {{list}}
                ];

                public int Warm(string productImage)
                {
                    ArgumentException.ThrowIfNullOrEmpty(productImage);
                    foreach (var rendition in Renditions)
                    {
                        SimulatedNetwork.Call($"cdn/prewarm/{rendition}/{productImage}", RenditionCostMs);
                    }
                    return Renditions.Length;
                }
            }
            """);
        yield return (Paths.For(Test), """
            using Storefront.Media;

            namespace Storefront.UnitTests.Media;

            public sealed class ImageCacheWarmerTests
            {
                [Fact]
                public void Warms_image_cache()
                {
                    Assert.True(new ImageCacheWarmer().Warm("products/lamp.jpg") >= 10);
                }
            }
            """);
    }
}

/// <summary>Run-level anomaly: a few runs land on a noisy machine (no code change).</summary>
public sealed class EnvBadRunner : Scenario
{
    public override string Id => "ENV-BAD-RUNNER";
    public override string Category => "environment";
    public override string Summary => "Environment noise, not a code change: in about 1 of 40 runs (decided by a hash of commit, target and run attempt) every simulated cost is 1.35x slower, as on a noisy CI machine. The affected runs are listed in bad-runners.json. No per-test alert is expected (must_not); a run-level anomaly may be reported. Real CPU-bound and sub-millisecond tests are not affected.";
    public override IEnumerable<TestRef> Tests(int commit) => [];
    public override Dist Expected(TestRef test, Target target, int commit) => new Dist.Absent();
    public override void Render(int commit, Snapshot snapshot) { }
}
