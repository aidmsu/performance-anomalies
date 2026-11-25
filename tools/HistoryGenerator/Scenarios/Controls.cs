using HistoryGenerator.Model;
using static HistoryGenerator.Model.Fmt;

namespace HistoryGenerator.Scenarios;

/// <summary>Three stable tests of different size that never change.</summary>
public sealed class CtlStable : MultiTestScenario
{
    public override string Id => "CTL-STABLE";
    public override string Category => "control";
    public override string Summary => "Three stable tests (15, 40 and 250 ms) whose cost never changes on any target.";

    private static readonly TestRef Currency = TestRef.Unit("Formatting", "CurrencyFormatterTests", "Formats_amounts_with_currency_symbols");
    private static readonly TestRef Skus = TestRef.Unit("Catalog", "SkuBatchParserTests", "Parses_sku_batch");
    private static readonly TestRef TaxTable = TestRef.Unit("Tax", "TaxTableTests", "Loads_tax_table");

    public override IEnumerable<TestRef> Tests(int commit) => [Currency, Skus, TaxTable];

    public override Dist Expected(TestRef test, Target target, int commit) =>
        test == Currency ? Dist.Ms(15) : test == Skus ? Dist.Ms(40) : test == TaxTable ? Dist.Ms(250) : new Dist.Absent();

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Formatting", "CurrencyFormatter"), """
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
            """);
        yield return (Paths.For(Currency), """
            using Storefront.Domain;
            using Storefront.Formatting;

            namespace Storefront.UnitTests.Formatting;

            public sealed class CurrencyFormatterTests
            {
                [Fact]
                public void Formats_amounts_with_currency_symbols()
                {
                    var formatted = new CurrencyFormatter().FormatAll([new Money(12.5m, "USD"), new Money(3m, "CHF")]);

                    Assert.Equal(["$12.50", "CHF3.00"], formatted);
                }
            }
            """);
        yield return (Paths.Src("Catalog", "SkuBatchParser"), """
            using Storefront.Domain;
            using Storefront.Infrastructure;

            namespace Storefront.Catalog;

            /// <summary>Parses SKUs uploaded by merchandisers and checks them against the catalog.</summary>
            public sealed class SkuBatchParser
            {
                private const double CatalogLookupCostMs = 40;

                public (IReadOnlyList<Sku> Valid, IReadOnlyList<string> Invalid) Parse(IEnumerable<string> lines)
                {
                    ArgumentNullException.ThrowIfNull(lines);
                    var valid = new List<Sku>();
                    var invalid = new List<string>();
                    foreach (var line in lines)
                    {
                        if (Sku.TryParse(line, out var sku)) valid.Add(sku);
                        else invalid.Add(line);
                    }
                    SimulatedDatabase.Query("select sku from catalog where sku in (@skus)", CatalogLookupCostMs);
                    return (valid, invalid);
                }
            }
            """);
        yield return (Paths.For(Skus), """
            using Storefront.Catalog;

            namespace Storefront.UnitTests.Catalog;

            public sealed class SkuBatchParserTests
            {
                [Fact]
                public void Parses_sku_batch()
                {
                    var (valid, invalid) = new SkuBatchParser().Parse(["ELEC-000001-01", "bad", "HOME-000042-03"]);

                    Assert.Equal(2, valid.Count);
                    Assert.Equal(["bad"], invalid);
                }
            }
            """);
        yield return (Paths.Src("Tax", "TaxTable"), """
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
            """);
        yield return (Paths.For(TaxTable), """
            using Storefront.Tax;

            namespace Storefront.UnitTests.Tax;

            public sealed class TaxTableTests
            {
                [Fact]
                public void Loads_tax_table()
                {
                    var table = TaxTable.Load();

                    Assert.Equal(0.19m, table.RateFor("de"));
                    Assert.Throws<KeyNotFoundException>(() => table.RateFor("XX"));
                }
            }
            """);
    }
}

/// <summary>A test with large natural variance (uniform 55..105 ms) and no change.</summary>
public sealed class CtlNoisy : SingleTestScenario
{
    public override string Id => "CTL-NOISY";
    public override string Category => "control";
    public override string Summary => "Noisy but unchanged test: carrier rate lookups take U(55..105) ms on every run of every commit.";
    public override TestRef Test { get; } = TestRef.Unit("Shipping", "ShippingQuoteTests", "Returns_quote_for_domestic_parcel");
    private const string Key = "ShippingQuoteService.Quote";

    protected override Dist Model(Target target, int commit) => new Dist.Uniform(55, 105, Key);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Shipping", "ShippingQuoteService"), $$"""
            using Storefront.Infrastructure;

            namespace Storefront.Shipping;

            public sealed record Parcel(string FromCountry, string ToCountry, decimal WeightKg);

            public sealed record ShippingQuote(string Carrier, decimal Price);

            /// <summary>Asks the carrier for a rate. Carrier response times vary a lot from call to call.</summary>
            public sealed class ShippingQuoteService
            {
                private const double FastestResponseMs = 55;
                private const double ResponseSpreadMs = 50;

                public ShippingQuote Quote(Parcel parcel)
                {
                    ArgumentNullException.ThrowIfNull(parcel);
                    var responseTime = FastestResponseMs + Workload.Random("{{Key}}").NextDouble() * ResponseSpreadMs;
                    SimulatedNetwork.Call("carrier-rates", responseTime);
                    var price = 4.90m + Math.Ceiling(parcel.WeightKg) * 1.20m;
                    return new ShippingQuote(parcel.FromCountry == parcel.ToCountry ? "national-post" : "express", price);
                }
            }
            """);
        yield return (Paths.For(Test), """
            using Storefront.Shipping;

            namespace Storefront.UnitTests.Shipping;

            public sealed class ShippingQuoteTests
            {
                [Fact]
                public void Returns_quote_for_domestic_parcel()
                {
                    var quote = new ShippingQuoteService().Quote(new Parcel("DE", "DE", 2.4m));

                    Assert.Equal("national-post", quote.Carrier);
                    Assert.Equal(8.50m, quote.Price);
                }
            }
            """);
    }
}

/// <summary>A test with rare heavy outliers from the very first commit (5% of runs take 5× longer).</summary>
public sealed class CtlHeavyTail : SingleTestScenario
{
    public override string Id => "CTL-HEAVY-TAIL";
    public override string Category => "control";
    public override string Summary => "Unchanged test with a heavy tail present from the start: 60 ms, but 5% of runs take 300 ms (cold model load). Outliers are seeded per run; their positions are in expected-durations.csv.";
    public override TestRef Test { get; } = TestRef.Unit("Recommendations", "RecommendationEngineTests", "Returns_top_recommendations");
    private const string Key = "RecommendationEngine.TopFor";

    protected override Dist Model(Target target, int commit) => new Dist.Tail(60, 0.05, 300, Key);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Recommendations", "RecommendationEngine"), $$"""
            using Storefront.Infrastructure;

            namespace Storefront.Recommendations;

            /// <summary>Scores products for a customer with the current recommendation model.</summary>
            public sealed class RecommendationEngine
            {
                private const double ScoringCostMs = 60;
                private const double ColdModelLoadCostMs = 240;
                private const double ColdModelProbability = 0.05;

                public IReadOnlyList<string> TopFor(string customerId, int count)
                {
                    ArgumentException.ThrowIfNullOrEmpty(customerId);
                    if (Workload.Random("{{Key}}").NextDouble() < ColdModelProbability)
                    {
                        // The model was evicted from the shared cache and has to be loaded again.
                        SimulatedStorage.Read("models/recommendations.onnx", ColdModelLoadCostMs);
                    }
                    Compute.Run("score-products", ScoringCostMs);
                    return Enumerable.Range(1, count).Select(i => $"SKU-{customerId.Length * 100 + i:000000}").ToList();
                }
            }
            """);
        yield return (Paths.For(Test), """
            using Storefront.Recommendations;

            namespace Storefront.UnitTests.Recommendations;

            public sealed class RecommendationEngineTests
            {
                [Fact]
                public void Returns_top_recommendations()
                {
                    var top = new RecommendationEngine().TopFor("customer-7", 5);

                    Assert.Equal(5, top.Count);
                    Assert.All(top, sku => Assert.StartsWith("SKU-", sku));
                }
            }
            """);
    }
}

/// <summary>Sub-millisecond tests of pure domain logic (static files: MoneyTests and SkuTests).</summary>
public sealed class CtlMicro : Scenario
{
    public override string Id => "CTL-MICRO";
    public override string Category => "control";
    public override string Summary => "Ten sub-millisecond tests of pure domain logic (Money, Sku). Their durations are dominated by timer resolution and JIT; relative changes are meaningless.";

    private static readonly TestRef[] All =
    [
        TestRef.Unit("Domain", "MoneyTests", "Adds_amounts_in_the_same_currency"),
        TestRef.Unit("Domain", "MoneyTests", "Rejects_mixed_currencies"),
        TestRef.Unit("Domain", "MoneyTests", "Rounds_half_to_even_when_multiplying"),
        TestRef.Unit("Domain", "MoneyTests", "Formats_with_invariant_culture"),
        TestRef.Unit("Domain", "MoneyTests", "Zero_is_the_identity_for_addition"),
        TestRef.Unit("Domain", "SkuTests", "Parses_valid_sku"),
        TestRef.Unit("Domain", "SkuTests", "Normalizes_category_to_upper_case"),
        TestRef.Unit("Domain", "SkuTests", "Round_trips_to_string"),
        TestRef.Unit("Domain", "SkuTests", "Rejects_sku_without_variant"),
        TestRef.Unit("Domain", "SkuTests", "Rejects_empty_input"),
    ];

    public override IEnumerable<TestRef> Tests(int commit) => All;
    public override Dist Expected(TestRef test, Target target, int commit) => All.Contains(test) ? new Dist.Micro() : new Dist.Absent();
    public override void Render(int commit, Snapshot snapshot) { }
}

/// <summary>Real CPU-bound work that never changes (SHA-256 over 32 MB).</summary>
public sealed class CtlCpuBound : SingleTestScenario
{
    public override string Id => "CTL-CPU-BOUND";
    public override string Category => "control";
    public override string Summary => "Real CPU-bound work (SHA-256 over 32 MB) that never changes. Its absolute duration depends on the runner CPU, so only stability matters.";
    public override TestRef Test { get; } = TestRef.Unit("Security", "CatalogHasherTests", "Hashes_catalog_export");

    protected override Dist Model(Target target, int commit) => new Dist.CpuBound(1);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Security", "CatalogHasher"), """
            using System.Security.Cryptography;

            namespace Storefront.Security;

            /// <summary>Fingerprints catalog exports so that partners can verify downloads.</summary>
            public static class CatalogHasher
            {
                public static string Hash(ReadOnlySpan<byte> export) => Convert.ToHexStringLower(SHA256.HashData(export));
            }
            """);
        yield return (Paths.For(Test), """
            using Storefront.Security;

            namespace Storefront.UnitTests.Security;

            public sealed class CatalogHasherTests
            {
                [Fact]
                public void Hashes_catalog_export()
                {
                    var export = new byte[32 * 1024 * 1024];
                    for (var i = 0; i < export.Length; i++) export[i] = (byte)(i * 31 + (i >> 8));

                    var first = CatalogHasher.Hash(export);
                    var second = CatalogHasher.Hash(export);

                    Assert.Equal(64, first.Length);
                    Assert.Equal(first, second);
                }
            }
            """);
    }
}

/// <summary>
/// The quiet majority: 60 small tests that never change. 12 of them have constant per-OS cost factors
/// (Windows ×1.4, macOS ×0.8), so pooling all targets into one series is wrong but never a change.
/// </summary>
public sealed class CtlQuiet : MultiTestScenario
{
    public override string Id => "CTL-QUIET";
    public override string Category => "control";
    public override string Summary => "60 small unchanged tests with log-normal baselines (0.2-200 ms). Twelve of them have constant per-OS cost factors (Windows x1.4, macOS x0.8). Every alert on them is a false positive.";

    private sealed record Operation(string Method, string Test, string Statement, double CostMs, bool PlatformDependent)
    {
        public double CostOn(Target target) => PlatformDependent ? target.Select(CostMs, Math.Round(CostMs * 1.4, 2), Math.Round(CostMs * 0.8, 2)) : CostMs;
    }

    private sealed record Service(string Area, string Type, string Description, Operation[] Operations);

    private static readonly IReadOnlyList<Service> Services = Build();

    private static List<Service> Build()
    {
        var specs = new (string Area, string Type, string Description, string[] Ops)[]
        {
            ("Customers", "CustomerDirectory", "Looks up customer accounts.", ["Find customer by email", "List customers by segment", "Count active customers", "Resolve customer alias", "Load customer preferences"]),
            ("Addresses", "AddressBook", "Stores shipping and billing addresses.", ["Validate postal code", "List saved addresses", "Format address label", "Geocode address", "Merge duplicate addresses"]),
            ("Reviews", "ReviewModeration", "Moderates product reviews.", ["Queue review for moderation", "Approve review", "Flag abusive review", "Summarize review ratings", "List pending reviews"]),
            ("Media", "MediaLibrary", "Keeps product images and videos.", ["Register media asset", "List assets for product", "Tag media asset", "Archive unused assets", "Resolve asset url"]),
            ("Suppliers", "SupplierDirectory", "Keeps supplier contacts and terms.", ["Find supplier by code", "List supplier contacts", "Load payment terms", "Check supplier certification", "Rank suppliers by lead time"]),
            ("Content", "PageCatalog", "Serves CMS pages.", ["Load landing page", "List navigation menu", "Resolve page slug", "Render page breadcrumbs", "List published banners"]),
            ("Preferences", "PreferenceCenter", "Manages notification preferences.", ["Load notification preferences", "Opt out of newsletter", "List subscription topics", "Apply quiet hours", "Export preference history"]),
            ("Accounts", "AccountSettings", "Manages account settings.", ["Change display name", "Enable two factor login", "List active sessions", "Rotate api key", "Load privacy settings"]),
            ("Analytics", "EventTracker", "Records storefront analytics events.", ["Track page view", "Track add to cart", "Aggregate daily events", "List top search terms", "Purge expired events"]),
            ("Marketing", "NewsletterService", "Builds and schedules newsletters.", ["Build newsletter audience", "Schedule newsletter", "Render newsletter preview", "List newsletter campaigns", "Count newsletter bounces"]),
            ("Support", "TicketRouter", "Routes support tickets to teams.", ["Route ticket by topic", "Escalate overdue ticket", "List open tickets", "Assign ticket owner", "Close resolved tickets"]),
            ("Translations", "TranslationCatalog", "Serves UI translations.", ["Load translation bundle", "Find missing translations", "List supported languages", "Resolve fallback language", "Count translated keys"]),
        };
        var random = new Random(20261125);
        var services = new List<Service>();
        var index = 0;
        foreach (var (area, type, description, ops) in specs)
        {
            var operations = new List<Operation>();
            foreach (var op in ops)
            {
                var u1 = 1.0 - random.NextDouble();
                var u2 = random.NextDouble();
                var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                var cost = Math.Round(Math.Clamp(Math.Exp(Math.Log(8) + 1.1 * z), 0.2, 200), 2);
                var words = op.Split(' ');
                var verb = words[0].EndsWith('y') ? words[0][..^1] + "ies" : words[0].EndsWith('h') || words[0].EndsWith('s') ? words[0] + "es" : words[0] + "s";
                var test = string.Join('_', new[] { verb }.Concat(words.Skip(1).Select(w => w.ToLowerInvariant())));
                var method = string.Concat(words.Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
                var statement = $"{type.ToLowerInvariant()}.{string.Join('-', words).ToLowerInvariant()}";
                operations.Add(new Operation(method, test, statement, cost, index % 5 == 2));
                index++;
            }
            services.Add(new Service(area, type, description, operations.ToArray()));
        }
        return services;
    }

    private static TestRef Ref(Service s, Operation o) => TestRef.Unit(s.Area, $"{s.Type}Tests", o.Test);

    public override IEnumerable<TestRef> Tests(int commit) => Services.SelectMany(s => s.Operations.Select(o => Ref(s, o)));

    public override Dist Expected(TestRef test, Target target, int commit)
    {
        foreach (var s in Services)
        foreach (var o in s.Operations)
            if (Ref(s, o) == test) return Dist.Ms(o.CostOn(target));
        return new Dist.Absent();
    }

    public override string Notes =>
        "Platform-dependent tests (constant factors): " + string.Join(", ", Services.SelectMany(s => s.Operations.Where(o => o.PlatformDependent).Select(o => $"`{s.Type}Tests.{o.Test}`")));

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        foreach (var s in Services)
        {
            var methods = string.Join("\n\n", s.Operations.Select(o =>
            {
                var cost = o.PlatformDependent
                    ? $"PlatformCost.Select(linux: {N(o.CostMs)}, windows: {N(Math.Round(o.CostMs * 1.4, 2))}, macOS: {N(Math.Round(o.CostMs * 0.8, 2))})"
                    : N(o.CostMs);
                return $$"""
                        public string {{o.Method}}(string input)
                        {
                            ArgumentException.ThrowIfNullOrEmpty(input);
                            SimulatedDatabase.Query("{{o.Statement}}", {{cost}});
                            return $"{{o.Statement}}:{input}";
                        }
                    """;
            }));
            yield return (Paths.Src(s.Area, s.Type), $$"""
                using Storefront.Infrastructure;

                namespace Storefront.{{s.Area}};

                /// <summary>{{s.Description}}</summary>
                public sealed class {{s.Type}}
                {
                {{methods}}
                }
                """);
            var tests = string.Join("\n\n", s.Operations.Select(o => $$"""
                    [Fact]
                    public void {{o.Test}}()
                    {
                        var result = new {{s.Type}}().{{o.Method}}("sample-42");

                        Assert.Equal("{{o.Statement}}:sample-42", result);
                    }
                """));
            yield return (Paths.Unit(s.Area, $"{s.Type}Tests"), $$"""
                using Storefront.{{s.Area}};

                namespace Storefront.UnitTests.{{s.Area}};

                public sealed class {{s.Type}}Tests
                {
                {{tests}}
                }
                """);
        }
    }
}
