using HistoryGenerator.Model;
using static HistoryGenerator.Model.Fmt;

namespace HistoryGenerator.Scenarios;

/// <summary>Large sudden step: 3 ms → 500 ms (the user's canonical "spike").</summary>
public sealed class RegStepLarge : SingleTestScenario
{
    public override string Id => "REG-STEP-LARGE";
    public override string Category => "regression";
    public override string Summary => "Sudden large step on every target: 3 ms → 500 ms at commit 60 (invoice rendering starts embedding four font files). A no-op refactoring of the same class at commit 57 is a decoy for culprit detection.";
    public override TestRef Test { get; } = TestRef.Unit("Billing", "InvoiceRendererTests", "Renders_invoice_pdf");

    private const int Decoy = 57;
    private const int Culprit = 60;
    private const double LayoutMs = 3;
    private const double FontReadMs = 124.25;
    private const int FontCount = 4;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Decoy, Kind = Kinds.Decoy, Tier = Tiers.MustNot, Subject = "Extract invoice line formatting into a helper" },
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Embed brand fonts into invoice PDFs",
            Body = "Customers reported that invoices printed with fallback fonts on some\nPDF viewers. Embed the four brand font files into every rendered PDF.",
        },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit ? LayoutMs + FontCount * FontReadMs : LayoutMs);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Billing", "InvoiceRenderer"), Tpl.Code($$"""
            using System.Globalization;
            using System.Text;
            using Storefront.Infrastructure;

            namespace Storefront.Billing;

            public sealed record InvoiceLine(string Description, int Quantity, decimal UnitPrice);

            public sealed record Invoice(string Number, IReadOnlyList<InvoiceLine> Lines);

            /// <summary>Renders invoices as PDF documents.</summary>
            public sealed class InvoiceRenderer
            {
                private const double LayoutCostMs = {{N(LayoutMs)}};
                private const double FontFileReadCostMs = {{N(FontReadMs)}}; //?fonts

                private static readonly string[] BrandFonts = //?fonts
                [ //?fonts
                    "fonts/Inter-Regular.ttf", //?fonts
                    "fonts/Inter-Bold.ttf", //?fonts
                    "fonts/SourceSerif-Regular.ttf", //?fonts
                    "fonts/JetBrainsMono-Regular.ttf", //?fonts
                ]; //?fonts
                //?fonts
                public byte[] RenderPdf(Invoice invoice)
                {
                    ArgumentNullException.ThrowIfNull(invoice);
                    Compute.Run("layout-invoice", LayoutCostMs);
                    var body = new StringBuilder();
                    body.Append("%PDF-1.7\n");
                    body.Append(CultureInfo.InvariantCulture, $"Invoice {invoice.Number}\n");
                    foreach (var line in invoice.Lines) //?!helper
                    { //?!helper
                        body.Append(CultureInfo.InvariantCulture, $"{line.Description} x{line.Quantity} {line.Quantity * line.UnitPrice:0.00}\n"); //?!helper
                    } //?!helper
                    foreach (var line in invoice.Lines) body.Append(FormatLine(line)); //?helper
                    foreach (var font in BrandFonts) //?fonts
                    { //?fonts
                        SimulatedStorage.Read(font, FontFileReadCostMs); //?fonts
                        body.Append(CultureInfo.InvariantCulture, $"/FontFile ({font})\n"); //?fonts
                    } //?fonts
                    body.Append("%%EOF\n");
                    return Encoding.ASCII.GetBytes(body.ToString());
                }
                //?helper
                private static string FormatLine(InvoiceLine line) => //?helper
                    string.Create(CultureInfo.InvariantCulture, $"{line.Description} x{line.Quantity} {line.Quantity * line.UnitPrice:0.00}\n"); //?helper
            }
            """, ("fonts", commit >= Culprit), ("helper", commit >= Decoy)));
        yield return (Paths.For(Test), """
            using System.Text;
            using Storefront.Billing;

            namespace Storefront.UnitTests.Billing;

            public sealed class InvoiceRendererTests
            {
                [Fact]
                public void Renders_invoice_pdf()
                {
                    var invoice = new Invoice("INV-1001", [new InvoiceLine("Desk lamp", 2, 39.90m), new InvoiceLine("Bulb", 4, 3.25m)]);

                    var pdf = new InvoiceRenderer().RenderPdf(invoice);

                    var text = Encoding.ASCII.GetString(pdf);
                    Assert.StartsWith("%PDF", text, StringComparison.Ordinal);
                    Assert.Contains("Desk lamp x2 79.80", text, StringComparison.Ordinal);
                    Assert.EndsWith("%%EOF\n", text, StringComparison.Ordinal);
                }
            }
            """);
    }
}

/// <summary>Moderate step: +40%.</summary>
public sealed class RegStepModerate : SingleTestScenario
{
    public override string Id => "REG-STEP-MODERATE";
    public override string Category => "regression";
    public override string Summary => "Moderate step on every target: 100 → 140 ms (+40%) at commit 36 (cart totals start estimating shipping per line).";
    public override TestRef Test { get; } = TestRef.Unit("Cart", "CartTotalsCalculatorTests", "Calculates_totals_for_large_cart");
    private const int Culprit = 36;
    private const int Lines = 20;
    private const double LinePricingMs = 5;
    private const double ShippingEstimateMs = 2;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Estimate shipping for every cart line",
            Body = "Bulky items ship separately, so the cart total now includes a shipping\nestimate per line instead of one flat estimate.",
        },
    ];

    protected override Dist Model(Target target, int commit) =>
        Dist.Ms(Lines * LinePricingMs + (commit >= Culprit ? Lines * ShippingEstimateMs : 0));

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Cart", "CartTotalsCalculator"), Tpl.Code($$"""
            using Storefront.Infrastructure;

            namespace Storefront.Cart;

            public sealed record CartLine(string Sku, int Quantity, decimal UnitPrice, bool Bulky);

            public sealed record CartTotals(decimal Subtotal, decimal Shipping)
            {
                public decimal Total => Subtotal + Shipping;
            }

            public sealed class CartTotalsCalculator
            {
                private const double LinePricingCostMs = {{N(LinePricingMs)}};
                private const double ShippingEstimateCostMs = {{N(ShippingEstimateMs)}}; //?perLine
                private const decimal FlatShipping = 4.90m; //?!perLine

                public CartTotals Calculate(IReadOnlyList<CartLine> lines)
                {
                    ArgumentNullException.ThrowIfNull(lines);
                    var subtotal = 0m;
                    var shipping = 0m; //?perLine
                    foreach (var line in lines)
                    {
                        SimulatedDatabase.Query("select price from price_list where sku = @sku", LinePricingCostMs);
                        subtotal += line.Quantity * line.UnitPrice;
                        SimulatedNetwork.Call("shipping-estimates", ShippingEstimateCostMs); //?perLine
                        shipping += line.Bulky ? 19.00m : 0.50m; //?perLine
                    }
                    return new CartTotals(subtotal, FlatShipping); //?!perLine
                    return new CartTotals(subtotal, Math.Max(shipping, 4.90m)); //?perLine
                }
            }
            """, ("perLine", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Cart;

            namespace Storefront.UnitTests.Cart;

            public sealed class CartTotalsCalculatorTests
            {
                [Fact]
                public void Calculates_totals_for_large_cart()
                {
                    var lines = Enumerable.Range(1, 20).Select(i => new CartLine($"HOME-{i:000000}-01", 1, 10m, Bulky: false)).ToList();

                    var totals = new CartTotalsCalculator().Calculate(lines);

                    Assert.Equal(200m, totals.Subtotal);
                    Assert.True(totals.Total > totals.Subtotal);
                }
            }
            """);
    }
}

/// <summary>Small step: +11%.</summary>
public sealed class RegStepSmall : SingleTestScenario
{
    public override string Id => "REG-STEP-SMALL";
    public override string Category => "regression";
    public override string Summary => "Small step on every target: 200 → 222 ms (+11%) at commit 64 (order numbers are checked against the archive).";
    public override TestRef Test { get; } = TestRef.Unit("Orders", "OrderNumberGeneratorTests", "Generates_order_number_batch");
    private const int Culprit = 64;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Tier = Tiers.Should, Subject = "Check new order numbers against the archive",
            Body = "Numbers reserved before the 2024 migration live in the archive database;\ncheck each batch there as well to avoid collisions.",
        },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit ? 222 : 200);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Orders", "OrderNumberGenerator"), Tpl.Code("""
            using System.Globalization;
            using Storefront.Infrastructure;

            namespace Storefront.Orders;

            /// <summary>Reserves blocks of order numbers.</summary>
            public sealed class OrderNumberGenerator
            {
                private const double ReserveBlockCostMs = 200;
                private const double ArchiveCheckCostMs = 22; //?archive

                private long _next = 100_000;

                public IReadOnlyList<string> NextBatch(int count)
                {
                    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
                    SimulatedDatabase.Execute("update order_number_blocks set next = next + @count", ReserveBlockCostMs);
                    SimulatedDatabase.Query("select 1 from archive.order_numbers where number between @from and @to", ArchiveCheckCostMs); //?archive
                    var batch = Enumerable.Range(0, count).Select(i => "SO-" + (_next + i).ToString(CultureInfo.InvariantCulture)).ToList();
                    _next += count;
                    return batch;
                }
            }
            """, ("archive", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Orders;

            namespace Storefront.UnitTests.Orders;

            public sealed class OrderNumberGeneratorTests
            {
                [Fact]
                public void Generates_order_number_batch()
                {
                    var batch = new OrderNumberGenerator().NextBatch(50);

                    Assert.Equal(50, batch.Distinct().Count());
                    Assert.Equal("SO-100000", batch[0]);
                }
            }
            """);
    }
}

/// <summary>Step that is smaller than the run-to-run noise of the test.</summary>
public sealed class RegStepInNoise : SingleTestScenario
{
    public override string Id => "REG-STEP-IN-NOISE";
    public override string Category => "regression";
    public override string Summary => "Shift inside heavy noise on every target: U(90..150) → U(120..180) ms at commit 58 (fraud scoring queries a device reputation service). Detectable only statistically.";
    public override TestRef Test { get; } = TestRef.Unit("Fraud", "FraudScorerTests", "Scores_order");
    private const int Culprit = 58;
    private const string Key = "FraudScorer.Score";

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Tier = Tiers.Should, Subject = "Query device reputation during fraud scoring",
            Body = "Device fingerprints that were seen in chargebacks raise the fraud score.",
        },
    ];

    protected override Dist Model(Target target, int commit) =>
        commit >= Culprit ? new Dist.Uniform(120, 180, Key) : new Dist.Uniform(90, 150, Key);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Fraud", "FraudScorer"), Tpl.Code($$"""
            using Storefront.Infrastructure;

            namespace Storefront.Fraud;

            public sealed record FraudCheck(string OrderId, string Email, string DeviceId, decimal Amount);

            /// <summary>Scores orders with the external risk engine, whose response time varies a lot.</summary>
            public sealed class FraudScorer
            {
                private const double RiskEngineFastestMs = 90;
                private const double RiskEngineSpreadMs = 60;
                private const double DeviceReputationCostMs = 30; //?device

                public int Score(FraudCheck check)
                {
                    ArgumentNullException.ThrowIfNull(check);
                    var responseTime = RiskEngineFastestMs + Workload.Random("{{Key}}").NextDouble() * RiskEngineSpreadMs;
                    SimulatedNetwork.Call("risk-engine/score", responseTime);
                    var score = check.Amount > 1000 ? 40 : 10;
                    SimulatedNetwork.Call("device-reputation/lookup", DeviceReputationCostMs); //?device
                    if (check.DeviceId.StartsWith("emu-", StringComparison.Ordinal)) score += 30; //?device
                    return Math.Min(score, 100);
                }
            }
            """, ("device", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Fraud;

            namespace Storefront.UnitTests.Fraud;

            public sealed class FraudScorerTests
            {
                [Fact]
                public void Scores_order()
                {
                    var score = new FraudScorer().Score(new FraudCheck("SO-1", "ada@example.com", "ios-17", 1200m));

                    Assert.InRange(score, 40, 100);
                }
            }
            """);
    }
}

/// <summary>Early regression with a short baseline (7 points).</summary>
public sealed class RegEarly : SingleTestScenario
{
    public override string Id => "REG-EARLY";
    public override string Category => "regression";
    public override string Summary => "Regression with a short baseline: 70 → 200 ms at commit 8 on every target (catalog feed rows are upserted one by one). Only 7 points before the change.";
    public override TestRef Test { get; } = TestRef.Integration("Catalog", "CatalogFeedImporterTests", "Imports_catalog_feed");
    private const int Culprit = 8;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Upsert catalog feed rows one at a time",
            Body = "A single bad row used to abort the whole bulk upsert. Upserting rows one\nby one lets the importer report every rejected row.",
        },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit ? 20 + 30 * 6 : 70);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Catalog", "CatalogFeedImporter"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Catalog;

            public sealed record CatalogFeedRow(string Sku, string Title, decimal Price);

            public sealed record FeedImportResult(int Imported, IReadOnlyList<string> Rejected);

            /// <summary>Imports the nightly product feed from the merchandising system.</summary>
            public sealed class CatalogFeedImporter
            {
                private const double ParseCostMs = 20;
                private const double BulkUpsertCostMs = 50; //?!perRow
                private const double RowUpsertCostMs = 6; //?perRow

                public FeedImportResult Import(IReadOnlyList<CatalogFeedRow> rows)
                {
                    ArgumentNullException.ThrowIfNull(rows);
                    Compute.Run("parse-catalog-feed", ParseCostMs);
                    var rejected = rows.Where(r => r.Price <= 0).Select(r => r.Sku).ToList();
                    SimulatedDatabase.Execute("merge into catalog using @rows", BulkUpsertCostMs); //?!perRow
                    foreach (var row in rows) //?perRow
                    { //?perRow
                        SimulatedDatabase.Execute("merge into catalog using @row", RowUpsertCostMs); //?perRow
                    } //?perRow
                    return new FeedImportResult(rows.Count - rejected.Count, rejected);
                }
            }
            """, ("perRow", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Catalog;

            namespace Storefront.IntegrationTests.Catalog;

            public sealed class CatalogFeedImporterTests
            {
                [Fact]
                public void Imports_catalog_feed()
                {
                    var rows = Enumerable.Range(1, 30)
                        .Select(i => new CatalogFeedRow($"HOME-{i:000000}-01", $"Product {i}", i == 30 ? 0m : 9.99m + i))
                        .ToList();

                    var result = new CatalogFeedImporter().Import(rows);

                    Assert.Equal(29, result.Imported);
                    Assert.Equal(["HOME-000030-01"], result.Rejected);
                }
            }
            """);
    }
}

/// <summary>Late regression with only 5 points after the change.</summary>
public sealed class RegLate : SingleTestScenario
{
    public override string Id => "REG-LATE";
    public override string Category => "regression";
    public override string Summary => "Recent regression: 25 → 150 ms at commit 116 on every target (order confirmation e-mails use a new template engine). Only 5 points after the change.";
    public override TestRef Test { get; } = TestRef.Integration("Notifications", "NotificationDispatcherTests", "Sends_order_confirmation");
    private const int Culprit = 116;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Render order confirmations with the new template engine",
            Body = "The marketing team owns the new templates; they support localized\nblocks and per-brand themes.",
        },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit ? 150 : 25);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Notifications", "NotificationDispatcher"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Notifications;

            public sealed record OrderConfirmation(string OrderId, string Email, string Language);

            /// <summary>Sends transactional e-mails.</summary>
            public sealed class NotificationDispatcher
            {
                private const double SmtpSubmitCostMs = 20;
                private const double LegacyTemplateCostMs = 5; //?!engine
                private const double TemplateEngineCostMs = 130; //?engine

                public string SendOrderConfirmation(OrderConfirmation confirmation)
                {
                    ArgumentNullException.ThrowIfNull(confirmation);
                    Compute.Run("render-legacy-template", LegacyTemplateCostMs); //?!engine
                    var subject = $"Order {confirmation.OrderId} confirmed"; //?!engine
                    SimulatedNetwork.Call("template-engine/render/order-confirmation", TemplateEngineCostMs); //?engine
                    var subject = $"[{confirmation.Language}] Order {confirmation.OrderId} confirmed"; //?engine
                    SimulatedNetwork.Call("smtp-relay", SmtpSubmitCostMs);
                    return subject;
                }
            }
            """, ("engine", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Notifications;

            namespace Storefront.IntegrationTests.Notifications;

            public sealed class NotificationDispatcherTests
            {
                [Fact]
                public void Sends_order_confirmation()
                {
                    var subject = new NotificationDispatcher().SendOrderConfirmation(new OrderConfirmation("SO-100042", "ada@example.com", "en"));

                    Assert.Contains("SO-100042", subject, StringComparison.Ordinal);
                }
            }
            """);
    }
}

/// <summary>Sensitivity ladder: +3% and +6% steps.</summary>
public sealed class RegSensitivity3 : SingleTestScenario
{
    public override string Id => "REG-SENS-3PCT";
    public override string Category => "regression";
    public override string Summary => "Tiny step on every target: 100 → 103 ms (+3%) at commit 14 (tier evaluation writes an audit record). At the noise floor; optional to detect.";
    public override TestRef Test { get; } = TestRef.Unit("Loyalty", "LoyaltyTierEvaluatorTests", "Computes_tier_for_customer");
    private const int Culprit = 14;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Tier = Tiers.Optional, Subject = "Audit loyalty tier evaluations" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit ? 103 : 100);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Loyalty", "LoyaltyTierEvaluator"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Loyalty;

            public enum LoyaltyTier { Bronze, Silver, Gold }

            public sealed class LoyaltyTierEvaluator
            {
                private const double OrderHistoryCostMs = 100;
                private const double AuditWriteCostMs = 3; //?audit

                public LoyaltyTier Evaluate(string customerId, decimal yearlySpend)
                {
                    ArgumentException.ThrowIfNullOrEmpty(customerId);
                    SimulatedDatabase.Query("select sum(total) from orders where customer_id = @id", OrderHistoryCostMs);
                    var tier = yearlySpend >= 2000 ? LoyaltyTier.Gold : yearlySpend >= 500 ? LoyaltyTier.Silver : LoyaltyTier.Bronze;
                    SimulatedDatabase.Execute("insert into loyalty_audit values (@id, @tier)", AuditWriteCostMs); //?audit
                    return tier;
                }
            }
            """, ("audit", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Loyalty;

            namespace Storefront.UnitTests.Loyalty;

            public sealed class LoyaltyTierEvaluatorTests
            {
                [Fact]
                public void Computes_tier_for_customer()
                {
                    Assert.Equal(LoyaltyTier.Silver, new LoyaltyTierEvaluator().Evaluate("customer-1", 750m));
                }
            }
            """);
    }
}

public sealed class RegSensitivity6 : SingleTestScenario
{
    public override string Id => "REG-SENS-6PCT";
    public override string Category => "regression";
    public override string Summary => "Small step on every target: 100 → 106 ms (+6%) at commit 26 (wishlist merge deduplicates by variant).";
    public override TestRef Test { get; } = TestRef.Unit("Wishlists", "WishlistMergerTests", "Merges_guest_and_account_wishlists");
    private const int Culprit = 26;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Tier = Tiers.Should, Subject = "Deduplicate merged wishlist items by variant" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit ? 106 : 100);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Wishlists", "WishlistMerger"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Wishlists;

            public sealed class WishlistMerger
            {
                private const double LoadWishlistsCostMs = 100;
                private const double VariantLookupCostMs = 6; //?variants

                public IReadOnlyList<string> Merge(IEnumerable<string> guestItems, IEnumerable<string> accountItems)
                {
                    SimulatedDatabase.Query("select items from wishlists where owner in (@guest, @account)", LoadWishlistsCostMs);
                    var merged = accountItems.Concat(guestItems);
                    return merged.Distinct(StringComparer.Ordinal).ToList(); //?!variants
                    SimulatedDatabase.Query("select sku, variant_group from variants where sku in (@skus)", VariantLookupCostMs); //?variants
                    return merged.DistinctBy(sku => sku[..sku.LastIndexOf('-')], StringComparer.Ordinal).ToList(); //?variants
                }
            }
            """, ("variants", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Wishlists;

            namespace Storefront.UnitTests.Wishlists;

            public sealed class WishlistMergerTests
            {
                [Fact]
                public void Merges_guest_and_account_wishlists()
                {
                    var merged = new WishlistMerger().Merge(["TOYS-000001-01", "HOME-000002-01"], ["HOME-000002-01"]);

                    Assert.Equal(2, merged.Count);
                }
            }
            """);
    }
}

/// <summary>Staircase: four +25 ms steps.</summary>
public sealed class RegStaircase : SingleTestScenario
{
    public override string Id => "REG-STAIRCASE";
    public override string Category => "regression";
    public override string Summary => "Staircase on every target: 60 → 85 → 110 → 135 → 160 ms at commits 24, 48, 70 and 96 (each commit adds a search analyzer stage).";
    public override TestRef Test { get; } = TestRef.Unit("Search", "SearchIndexBuilderTests", "Builds_index_for_catalog");

    private static readonly (int Commit, string Stage, string Subject)[] Stages =
    [
        (24, "synonyms", "Expand synonyms when building the search index"),
        (48, "attributes", "Index product attributes for faceted search"),
        (70, "phonetic", "Add phonetic matching for brand names"),
        (96, "prefixes", "Store edge n-grams for search autocomplete"),
    ];

    public override IReadOnlyList<Change> Changes { get; } =
        Stages.Select(s => new Change { Commit = s.Commit, Kind = Kinds.Step, Subject = s.Subject }).ToList();

    protected override Dist Model(Target target, int commit) => Dist.Ms(60 + 25 * Phase(commit));

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        var active = Stages.Where(s => s.Commit <= commit).Select(s => $"        \"{s.Stage}\",").ToList();
        var stages = active.Count == 0
            ? "    private static readonly string[] AnalyzerStages = [];"
            : "    private static readonly string[] AnalyzerStages =\n    [\n" + string.Join("\n", active) + "\n    ];";
        yield return (Paths.Src("Search", "SearchIndexBuilder"), $$"""
            using Storefront.Infrastructure;

            namespace Storefront.Search;

            public sealed record SearchDocument(string Sku, string Title, string Brand);

            /// <summary>Builds the product search index. Each analyzer stage runs over the whole catalog.</summary>
            public sealed class SearchIndexBuilder
            {
                private const double TokenizeCostMs = 60;
                private const double AnalyzerStageCostMs = 25;

            {{stages}}

                public IReadOnlyDictionary<string, IReadOnlyList<string>> Build(IReadOnlyList<SearchDocument> documents)
                {
                    ArgumentNullException.ThrowIfNull(documents);
                    Compute.Run("tokenize-catalog", TokenizeCostMs);
                    foreach (var stage in AnalyzerStages)
                    {
                        Compute.Run($"analyzer-{stage}", AnalyzerStageCostMs);
                    }
                    return documents
                        .SelectMany(d => d.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(t => (Term: t.ToLowerInvariant(), d.Sku)))
                        .GroupBy(x => x.Term)
                        .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Sku).Distinct().ToList());
                }
            }
            """);
        yield return (Paths.For(Test), """
            using Storefront.Search;

            namespace Storefront.UnitTests.Search;

            public sealed class SearchIndexBuilderTests
            {
                [Fact]
                public void Builds_index_for_catalog()
                {
                    var index = new SearchIndexBuilder().Build(
                    [
                        new SearchDocument("HOME-000001-01", "Oak desk lamp", "Lumen"),
                        new SearchDocument("HOME-000002-01", "Brass desk", "Forge"),
                    ]);

                    Assert.Equal(2, index["desk"].Count);
                }
            }
            """);
    }
}

/// <summary>Regression only on arm64 (Linux arm64 and macOS), not on x64.</summary>
public sealed class RegArm64Only : SingleTestScenario
{
    public override string Id => "REG-ARM64-ONLY";
    public override string Category => "regression";
    public override string Summary => "Architecture-specific regression: 30 → 90 ms at commit 20 on linux-arm64 and macos-arm64 only (the new checksum has a vectorized path for x64 only). x64 targets are unchanged.";
    public override TestRef Test { get; } = TestRef.Unit("Security", "PayloadChecksumTests", "Computes_payload_checksum");
    private const int Culprit = 20;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Switch payload checksums to CRC-64",
            Body = "CRC-32 collided on large catalog exports. The CRC-64 implementation has\na vectorized path for x64; other architectures use the portable loop.",
        },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit && target.IsArm64() ? 90 : 30);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Security", "PayloadChecksum"), Tpl.Code("""
            using System.Runtime.InteropServices; //?crc64
            using Storefront.Infrastructure;

            namespace Storefront.Security;

            /// <summary>Checksums payloads exchanged with marketplace partners.</summary>
            public static class PayloadChecksum
            {
                private const double ChecksumCostMs = 30;
                private const double PortableLoopExtraCostMs = 60; //?crc64

                public static ulong Compute(ReadOnlySpan<byte> payload)
                {
                    Infrastructure.Compute.Run("checksum-payload", ChecksumCostMs);
                    if (RuntimeInformation.ProcessArchitecture != Architecture.X64) //?crc64
                    { //?crc64
                        // No vectorized implementation: fall back to the portable table-driven loop. //?crc64
                        Infrastructure.Compute.Run("checksum-payload-portable", PortableLoopExtraCostMs); //?crc64
                    } //?crc64
                    var hash = 14695981039346656037UL;
                    foreach (var b in payload)
                    {
                        hash ^= b;
                        hash *= 1099511628211UL;
                    }
                    return hash;
                }
            }
            """, ("crc64", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using System.Text;
            using Storefront.Security;

            namespace Storefront.UnitTests.Security;

            public sealed class PayloadChecksumTests
            {
                [Fact]
                public void Computes_payload_checksum()
                {
                    var payload = Encoding.UTF8.GetBytes("catalog-export-2026-11");

                    var checksum = PayloadChecksum.Compute(payload);

                    Assert.NotEqual(0UL, checksum);
                    Assert.NotEqual(14695981039346656037UL, checksum);
                }
            }
            """);
    }
}

/// <summary>Regression on Linux and macOS, not on Windows.</summary>
public sealed class RegPosixOnly : SingleTestScenario
{
    public override string Id => "REG-POSIX-ONLY";
    public override string Category => "regression";
    public override string Summary => "Regression on three of four targets: 40 → 100 ms at commit 30 on linux-x64, linux-arm64 and macos-arm64 (POSIX permissions are applied per directory). windows-x64 is unchanged.";
    public override TestRef Test { get; } = TestRef.Integration("Storage", "ExportPermissionsTests", "Applies_export_permissions");
    private const int Culprit = 30;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Apply POSIX permissions to every export directory",
            Body = "Partners on SFTP could read each other's exports because only the root\ndirectory had restricted permissions.",
        },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit && !target.IsWindows() ? 100 : 40);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Storage", "ExportPermissions"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Storage;

            /// <summary>Restricts access to partner export folders.</summary>
            public static class ExportPermissions
            {
                private const double AclWriteCostMs = 40;
                private const double ChmodCostMs = 10; //?posix

                public static int Apply(string root, IReadOnlyList<string> directories)
                {
                    ArgumentException.ThrowIfNullOrEmpty(root);
                    ArgumentNullException.ThrowIfNull(directories);
                    SimulatedStorage.Write(root + "/.acl", AclWriteCostMs);
                    var updated = 1;
                    if (!OperatingSystem.IsWindows()) //?posix
                    { //?posix
                        foreach (var directory in directories) //?posix
                        { //?posix
                            SimulatedStorage.Write($"{root}/{directory}", ChmodCostMs); //?posix
                            updated++; //?posix
                        } //?posix
                    } //?posix
                    return updated;
                }
            }
            """, ("posix", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Storage;

            namespace Storefront.IntegrationTests.Storage;

            public sealed class ExportPermissionsTests
            {
                [Fact]
                public void Applies_export_permissions()
                {
                    var updated = ExportPermissions.Apply("/exports/partner-7", ["2026", "2026/11", "2026/11/25", "daily", "weekly", "full"]);

                    Assert.True(updated >= 1);
                }
            }
            """);
    }
}

/// <summary>Windows-only regression (large).</summary>
public sealed class RegWindowsOnly : SingleTestScenario
{
    public override string Id => "REG-WINDOWS-ONLY";
    public override string Category => "regression";
    public override string Summary => "Windows-only regression: 30 → 330 ms at commit 52 on windows-x64 (attachment writes wait for the file handle to be released three times). Linux and macOS are unchanged.";
    public override TestRef Test { get; } = TestRef.Integration("Storage", "AttachmentStoreTests", "Saves_attachment");
    private const int Culprit = 52;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Retry attachment writes on Windows sharing violations",
            Body = "Antivirus scanners keep new files open for a moment on Windows. Wait for\nthe handle to be released before every write attempt.",
        },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit && target.IsWindows() ? 330 : 30);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Storage", "AttachmentStore"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Storage;

            public sealed record Attachment(string FileName, byte[] Content);

            /// <summary>Stores customer attachments (return photos, invoices) in blob storage.</summary>
            public sealed class AttachmentStore
            {
                private const double WriteCostMs = 30;
                private const double HandleReleaseWaitMs = 100; //?retry
                private const int WriteAttempts = 3; //?retry

                public string Save(Attachment attachment)
                {
                    ArgumentNullException.ThrowIfNull(attachment);
                    var path = $"attachments/{Guid.NewGuid():N}/{attachment.FileName}";
                    if (OperatingSystem.IsWindows()) //?retry
                    { //?retry
                        for (var attempt = 0; attempt < WriteAttempts; attempt++) //?retry
                        { //?retry
                            SimulatedStorage.Read(path + ".lock", HandleReleaseWaitMs); //?retry
                        } //?retry
                    } //?retry
                    SimulatedStorage.Write(path, WriteCostMs);
                    return path;
                }
            }
            """, ("retry", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Storage;

            namespace Storefront.IntegrationTests.Storage;

            public sealed class AttachmentStoreTests
            {
                [Fact]
                public void Saves_attachment()
                {
                    var path = new AttachmentStore().Save(new Attachment("photo.jpg", [1, 2, 3]));

                    Assert.EndsWith("/photo.jpg", path, StringComparison.Ordinal);
                }
            }
            """);
    }
}

/// <summary>Windows-only regression near the noise floor (+25%).</summary>
public sealed class RegWindowsHard : SingleTestScenario
{
    public override string Id => "REG-WINDOWS-HARD";
    public override string Category => "regression";
    public override string Summary => "Small Windows-only regression: 80 → 100 ms (+25%) at commit 74 on windows-x64 (CSV export normalizes line endings for Excel). Other targets unchanged.";
    public override TestRef Test { get; } = TestRef.Unit("Reporting", "CsvExporterTests", "Writes_csv_export");
    private const int Culprit = 74;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Tier = Tiers.Should, Subject = "Normalize CSV line endings for Excel on Windows" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit && target.IsWindows() ? 100 : 80);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Reporting", "CsvExporter"), Tpl.Code("""
            using System.Text;
            using Storefront.Infrastructure;

            namespace Storefront.Reporting;

            public static class CsvExporter
            {
                private const double WriteCostMs = 80;
                private const double NormalizeCostMs = 20; //?crlf

                public static string Write(IEnumerable<string[]> rows)
                {
                    var csv = new StringBuilder();
                    foreach (var row in rows) csv.Append(string.Join(',', row)).Append('\n');
                    SimulatedStorage.Write("exports/report.csv", WriteCostMs);
                    if (OperatingSystem.IsWindows()) //?crlf
                    { //?crlf
                        Compute.Run("normalize-line-endings", NormalizeCostMs); //?crlf
                    } //?crlf
                    return csv.ToString();
                }
            }
            """, ("crlf", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Reporting;

            namespace Storefront.UnitTests.Reporting;

            public sealed class CsvExporterTests
            {
                [Fact]
                public void Writes_csv_export()
                {
                    var csv = CsvExporter.Write([["sku", "qty"], ["HOME-000001-01", "3"]]);

                    Assert.Equal("sku,qty\nHOME-000001-01,3\n", csv);
                }
            }
            """);
    }
}

/// <summary>macOS-only regression (not arm64 in general: linux-arm64 is unchanged).</summary>
public sealed class RegMacOnly : SingleTestScenario
{
    public override string Id => "REG-MACOS-ONLY";
    public override string Category => "regression";
    public override string Summary => "macOS-only regression: 50 → 120 ms at commit 72 on macos-arm64 (thumbnails are converted to Display P3). linux-arm64 is unchanged, so this is not an architecture effect.";
    public override TestRef Test { get; } = TestRef.Unit("Media", "ThumbnailGeneratorTests", "Generates_thumbnail");
    private const int Culprit = 72;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Subject = "Convert thumbnails to Display P3 on macOS" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit && target.IsMac() ? 120 : 50);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Media", "ThumbnailGenerator"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Media;

            public sealed record Thumbnail(string Path, int Width, int Height, string ColorProfile);

            public sealed class ThumbnailGenerator
            {
                private const double ResizeCostMs = 50;
                private const double ColorConversionCostMs = 70; //?p3

                public Thumbnail Generate(string imagePath, int maxSize)
                {
                    ArgumentException.ThrowIfNullOrEmpty(imagePath);
                    Compute.Run("resize-image", ResizeCostMs);
                    var profile = "sRGB";
                    if (OperatingSystem.IsMacOS()) //?p3
                    { //?p3
                        Compute.Run("convert-color-profile", ColorConversionCostMs); //?p3
                        profile = "Display P3"; //?p3
                    } //?p3
                    return new Thumbnail(imagePath + ".thumb.jpg", maxSize, maxSize * 3 / 4, profile);
                }
            }
            """, ("p3", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Media;

            namespace Storefront.UnitTests.Media;

            public sealed class ThumbnailGeneratorTests
            {
                [Fact]
                public void Generates_thumbnail()
                {
                    var thumbnail = new ThumbnailGenerator().Generate("products/lamp.jpg", 320);

                    Assert.Equal(240, thumbnail.Height);
                    Assert.NotEmpty(thumbnail.ColorProfile);
                }
            }
            """);
    }
}

/// <summary>One change, different magnitude on every target.</summary>
public sealed class RegAllTargetsDifferent : SingleTestScenario
{
    public override string Id => "REG-ALL-TARGETS-DIFFERENT";
    public override string Category => "regression";
    public override string Summary => "One change that regresses every target by a different amount at commit 56: Linux 60 → 75 ms (+25%), macOS 60 → 90 ms (+50%), Windows 60 → 180 ms (x3). It must be reported on all targets, not as Windows-only.";
    public override TestRef Test { get; } = TestRef.Unit("Feeds", "ProductFeedBuilderTests", "Builds_product_feed");
    private const int Culprit = 56;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Subject = "Validate product feeds against the partner XSD" },
    ];

    protected override Dist Model(Target target, int commit) =>
        Dist.Ms(commit >= Culprit ? 60 + target.Select(linux: 15, windows: 120, macOS: 30) : 60);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Feeds", "ProductFeedBuilder"), Tpl.Code("""
            using System.Text;
            using Storefront.Infrastructure;

            namespace Storefront.Feeds;

            public sealed record FeedItem(string Sku, string Title, decimal Price);

            /// <summary>Builds the XML product feed for marketplace partners.</summary>
            public sealed class ProductFeedBuilder
            {
                private const double SerializeCostMs = 60;

                public string Build(IEnumerable<FeedItem> items)
                {
                    var xml = new StringBuilder("<feed>");
                    foreach (var item in items) xml.Append("<item sku=\"").Append(item.Sku).Append("\"/>");
                    xml.Append("</feed>");
                    Compute.Run("serialize-feed", SerializeCostMs);
                    // Schema validation goes through the platform XML stack, whose speed differs per OS. //?xsd
                    Compute.Run("validate-feed-schema", PlatformCost.Select(linux: 15, windows: 120, macOS: 30)); //?xsd
                    return xml.ToString();
                }
            }
            """, ("xsd", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Feeds;

            namespace Storefront.UnitTests.Feeds;

            public sealed class ProductFeedBuilderTests
            {
                [Fact]
                public void Builds_product_feed()
                {
                    var xml = new ProductFeedBuilder().Build([new FeedItem("HOME-000001-01", "Lamp", 39.9m)]);

                    Assert.Equal("<feed><item sku=\"HOME-000001-01\"/></feed>", xml);
                }
            }
            """);
    }
}

/// <summary>One commit: faster on Linux, slower on Windows, unchanged on macOS.</summary>
public sealed class RegOsTradeoff : SingleTestScenario
{
    public override string Id => "REG-OS-TRADEOFF";
    public override string Category => "regression";
    public override string Summary => "Trade-off in one commit (86): path canonicalization becomes faster on Linux (60 → 25 ms), slower on Windows (60 → 150 ms) and stays at 60 ms on macOS.";
    public override TestRef Test { get; } = TestRef.Unit("Storage", "PathResolverTests", "Resolves_relative_paths");
    private const int Culprit = 86;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Subject = "Use platform-native path canonicalization" },
    ];

    protected override Dist Model(Target target, int commit) =>
        Dist.Ms(commit >= Culprit ? target.Select(linux: 25, windows: 150, macOS: 60) : 60);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Storage", "PathResolver"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Storage;

            /// <summary>Resolves user-supplied export paths inside the storage root.</summary>
            public static class PathResolver
            {
                private const double ManagedCanonicalizationCostMs = 60; //?!native

                public static string Resolve(string root, string relative)
                {
                    ArgumentException.ThrowIfNullOrEmpty(root);
                    ArgumentException.ThrowIfNullOrEmpty(relative);
                    Compute.Run("canonicalize-path", ManagedCanonicalizationCostMs); //?!native
                    // realpath(3) on Linux, GetFinalPathNameByHandle on Windows, CFURL on macOS. //?native
                    SimulatedStorage.Read(root, PlatformCost.Select(linux: 25, windows: 150, macOS: 60)); //?native
                    var segments = new Stack<string>();
                    foreach (var part in relative.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (part == "..") { if (segments.Count > 0) segments.Pop(); }
                        else if (part != ".") segments.Push(part);
                    }
                    return root.TrimEnd('/') + "/" + string.Join('/', segments.Reverse());
                }
            }
            """, ("native", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Storage;

            namespace Storefront.UnitTests.Storage;

            public sealed class PathResolverTests
            {
                [Fact]
                public void Resolves_relative_paths()
                {
                    Assert.Equal("/exports/b/c.csv", PathResolver.Resolve("/exports/", "a/../../b/./c.csv"));
                }
            }
            """);
    }
}

/// <summary>Parameter-dependent regression: O(n) → O(n²) deduplication.</summary>
public sealed class RegParamScaling : MultiTestScenario
{
    public override string Id => "REG-PARAM-SCALING";
    public override string Category => "regression";
    public override string Summary => "Parameter-dependent regression at commit 46 on every target: deduplication switches from a hash set to a nested loop, so cost goes from 2+0.005n ms to 2+0.005n+0.00001·n²/2 ms. itemCount=10 and 100 are unchanged (must_not), 1000 is +70%, 10000 is x10.6. A no-op refactoring of the same class at commit 45 is a decoy.";

    private const int Decoy = 45;
    private const int Culprit = 46;
    private const double FixedMs = 2;
    private const double PerItemMs = 0.005;
    private const double PerPairMs = 0.00001;
    private static readonly int[] Sizes = [10, 100, 1000, 10000];

    private static TestRef Ref(int n) => TestRef.Unit("Catalog", "DeduplicatorTests", "Removes_duplicates", $"itemCount: {n}");

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Decoy, Kind = Kinds.Decoy, Tier = Tiers.MustNot, Subject = "Extract duplicate key selection in the deduplicator" },
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Keep first occurrence order when removing duplicate items",
            Body = "HashSet-based deduplication returned items in hash order on some\nruntimes. Scan the kept items instead so that the first occurrence wins\nand the original order is preserved.",
        },
    ];

    public override IEnumerable<TestRef> Tests(int commit) => Sizes.Select(Ref);

    public override Dist Expected(TestRef test, Target target, int commit)
    {
        var n = Sizes.FirstOrDefault(s => Ref(s) == test);
        if (n == 0) return new Dist.Absent();
        var cost = FixedMs + PerItemMs * n + (commit >= Culprit ? PerPairMs * n * (double)n / 2 : 0);
        return Dist.Ms(Math.Round(cost, 4));
    }

    public override string? TierFor(Change change, TestRef test, Target target) =>
        change.Kind != Kinds.Step ? null : test.Args == "itemCount: 1000" ? Tiers.Should : test.Args == "itemCount: 10000" ? Tiers.Must : Tiers.MustNot;

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Catalog", "Deduplicator"), Tpl.Code($$"""
            using System.Diagnostics;
            using Storefront.Infrastructure;

            namespace Storefront.Catalog;

            public sealed record CatalogItem(int ProductId, string Variant);

            /// <summary>Removes duplicate items from merchandiser uploads.</summary>
            public static class Deduplicator
            {
                private const double FixedCostMs = {{N(FixedMs)}};
                private const double PerItemCostMs = {{N(PerItemMs)}};
                private const double PerComparisonCostMs = {{N(PerPairMs)}}; //?scan

                public static IReadOnlyList<CatalogItem> RemoveDuplicates(IReadOnlyList<CatalogItem> items)
                {
                    ArgumentNullException.ThrowIfNull(items);
                    var stopwatch = Stopwatch.StartNew();
                    var seen = new HashSet<(int, string)>(); //?!scan
                    var result = new List<CatalogItem>(items.Count);
                    foreach (var item in items)
                    {
                        if (seen.Add((item.ProductId, item.Variant))) result.Add(item); //?!helper
                        if (seen.Add(KeyOf(item))) result.Add(item); //?helperOnly
                        if (!ContainsKey(result, KeyOf(item))) result.Add(item); //?scan
                    }
                    var cost = FixedCostMs + PerItemCostMs * items.Count;
                    cost += PerComparisonCostMs * items.Count * (double)items.Count / 2; //?scan
                    Workload.PadTo(stopwatch, cost);
                    return result;
                }
                //?helper
                private static (int, string) KeyOf(CatalogItem item) => (item.ProductId, item.Variant); //?helper
                //?scan
                private static bool ContainsKey(List<CatalogItem> kept, (int, string) key) //?scan
                { //?scan
                    foreach (var existing in kept) //?scan
                    { //?scan
                        if (KeyOf(existing) == key) return true; //?scan
                    } //?scan
                    return false; //?scan
                } //?scan
            }
            """, ("scan", commit >= Culprit), ("helper", commit >= Decoy), ("helperOnly", commit >= Decoy && commit < Culprit)));
        yield return (Paths.Unit("Catalog", "DeduplicatorTests"), """
            using Storefront.Catalog;

            namespace Storefront.UnitTests.Catalog;

            public sealed class DeduplicatorTests
            {
                [Theory]
                [InlineData(10)]
                [InlineData(100)]
                [InlineData(1000)]
                [InlineData(10000)]
                public void Removes_duplicates(int itemCount)
                {
                    var distinct = itemCount - itemCount / 10;
                    var items = Enumerable.Range(0, itemCount).Select(i => new CatalogItem(i % distinct, "default")).ToList();

                    var result = Deduplicator.RemoveDuplicates(items);

                    Assert.Equal(distinct, result.Count);
                    Assert.Equal(0, result[0].ProductId);
                }
            }
            """);
    }
}

/// <summary>Regression in one data-driven case only (tr-TR culture).</summary>
public sealed class RegParamCase : MultiTestScenario
{
    public override string Id => "REG-PARAM-CASE";
    public override string Category => "regression";
    public override string Summary => "Regression in one theory case at commit 100 on every target: only culture \"tr-TR\" goes 20 → 90 ms (Turkish dotted-I normalization). en-US, de-DE and ja-JP are unchanged (must_not).";

    private const int Culprit = 100;
    private static readonly string[] Cultures = ["en-US", "de-DE", "tr-TR", "ja-JP"];
    private static TestRef Ref(string culture) => TestRef.Unit("Localization", "OrderDateFormatterTests", "Formats_order_date", $"culture: \"{culture}\"");

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Subject = "Normalize Turkish dotted and dotless I in month names" },
    ];

    public override IEnumerable<TestRef> Tests(int commit) => Cultures.Select(Ref);

    public override Dist Expected(TestRef test, Target target, int commit)
    {
        var culture = Cultures.FirstOrDefault(c => Ref(c) == test);
        if (culture is null) return new Dist.Absent();
        return Dist.Ms(commit >= Culprit && culture == "tr-TR" ? 90 : 20);
    }

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Localization", "OrderDateFormatter"), Tpl.Code("""
            using System.Globalization;
            using Storefront.Infrastructure;

            namespace Storefront.Localization;

            /// <summary>Formats order dates for e-mails and invoices in the customer's language.</summary>
            public static class OrderDateFormatter
            {
                private const double ResourceLookupCostMs = 20;
                private const double TurkishNormalizationCostMs = 70; //?turkish

                public static string Format(DateOnly date, string cultureName)
                {
                    ArgumentException.ThrowIfNullOrEmpty(cultureName);
                    var culture = CultureInfo.GetCultureInfo(cultureName);
                    SimulatedStorage.Read($"i18n/{culture.Name}/dates.json", ResourceLookupCostMs);
                    var text = date.ToDateTime(TimeOnly.MinValue).ToString("D", culture);
                    if (culture.TwoLetterISOLanguageName == "tr") //?turkish
                    { //?turkish
                        // Month names may come from templates with ASCII I; normalize them per locale rules. //?turkish
                        Compute.Run("normalize-turkish-i", TurkishNormalizationCostMs); //?turkish
                        text = text.Normalize(System.Text.NormalizationForm.FormC); //?turkish
                    } //?turkish
                    return text;
                }
            }
            """, ("turkish", commit >= Culprit)));
        yield return (Paths.Unit("Localization", "OrderDateFormatterTests"), """
            using Storefront.Localization;

            namespace Storefront.UnitTests.Localization;

            public sealed class OrderDateFormatterTests
            {
                [Theory]
                [InlineData("en-US")]
                [InlineData("de-DE")]
                [InlineData("tr-TR")]
                [InlineData("ja-JP")]
                public void Formats_order_date(string culture)
                {
                    var text = OrderDateFormatter.Format(new DateOnly(2026, 3, 14), culture);

                    Assert.Contains("2026", text, StringComparison.Ordinal);
                    Assert.Contains("14", text, StringComparison.Ordinal);
                }
            }
            """);
    }
}

/// <summary>Parameter case near the noise floor: only KWD +30%.</summary>
public sealed class RegParamHard : MultiTestScenario
{
    public override string Id => "REG-PARAM-HARD";
    public override string Category => "regression";
    public override string Summary => "Small regression in one theory case at commit 18 on every target: only currency \"KWD\" (three decimal places) goes 40 → 52 ms (+30%). USD, EUR and JPY are unchanged (must_not).";

    private const int Culprit = 18;
    private static readonly string[] Currencies = ["USD", "EUR", "JPY", "KWD"];
    private static TestRef Ref(string currency) => TestRef.Unit("Pricing", "CurrencyConverterTests", "Converts_order_total", $"currency: \"{currency}\"");

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Tier = Tiers.Should, Subject = "Round three-decimal currencies with the exchange's rules" },
    ];

    public override IEnumerable<TestRef> Tests(int commit) => Currencies.Select(Ref);

    public override Dist Expected(TestRef test, Target target, int commit)
    {
        var currency = Currencies.FirstOrDefault(c => Ref(c) == test);
        if (currency is null) return new Dist.Absent();
        return Dist.Ms(commit >= Culprit && currency == "KWD" ? 52 : 40);
    }

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Pricing", "CurrencyConverter"), Tpl.Code("""
            using Storefront.Domain;
            using Storefront.Infrastructure;

            namespace Storefront.Pricing;

            /// <summary>Converts order totals using the daily exchange rates.</summary>
            public static class CurrencyConverter
            {
                private const double RateLookupCostMs = 40;
                private const double ExchangeRoundingCostMs = 12; //?precise

                private static readonly IReadOnlyDictionary<string, (decimal Rate, int Decimals)> Rates = new Dictionary<string, (decimal, int)>
                {
                    ["USD"] = (1.08m, 2),
                    ["EUR"] = (1m, 2),
                    ["JPY"] = (162.4m, 0),
                    ["KWD"] = (0.332m, 3),
                };

                public static Money Convert(Money amount, string currency)
                {
                    var (rate, decimals) = Rates[currency];
                    SimulatedNetwork.Call($"fx-rates/{amount.Currency}/{currency}", RateLookupCostMs);
                    if (decimals == 3) //?precise
                    { //?precise
                        // Three-decimal currencies are rounded with the rules published by the exchange. //?precise
                        SimulatedNetwork.Call($"fx-rates/{currency}/rounding-rules", ExchangeRoundingCostMs); //?precise
                    } //?precise
                    return new Money(Math.Round(amount.Amount * rate, decimals, MidpointRounding.ToEven), currency);
                }
            }
            """, ("precise", commit >= Culprit)));
        yield return (Paths.Unit("Pricing", "CurrencyConverterTests"), """
            using Storefront.Domain;
            using Storefront.Pricing;

            namespace Storefront.UnitTests.Pricing;

            public sealed class CurrencyConverterTests
            {
                [Theory]
                [InlineData("USD")]
                [InlineData("EUR")]
                [InlineData("JPY")]
                [InlineData("KWD")]
                public void Converts_order_total(string currency)
                {
                    var converted = CurrencyConverter.Convert(new Money(100m, "EUR"), currency);

                    Assert.Equal(currency, converted.Currency);
                    Assert.True(converted.Amount > 0);
                }
            }
            """);
    }
}

/// <summary>Two unrelated regressions in one squash commit (two causes, one SHA).</summary>
public sealed class RegSquashGiftCard : SingleTestScenario
{
    public override string Id => "REG-SQUASH-GIFTCARD";
    public override string Category => "regression";
    public override string Summary => "First of two unrelated regressions squashed into commit 22: gift card redemption 30 → 75 ms on every target (velocity check). Same SHA as REG-SQUASH-RETURNS but a different cause.";
    public override TestRef Test { get; } = TestRef.Unit("GiftCards", "GiftCardServiceTests", "Redeems_gift_card");

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = 22, Kind = Kinds.Step, Subject = "Check gift card redemption velocity" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= 22 ? 75 : 30);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("GiftCards", "GiftCardService"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.GiftCards;

            public sealed class GiftCardService
            {
                private const double BalanceUpdateCostMs = 30;
                private const double VelocityCheckCostMs = 45; //?velocity

                private readonly Dictionary<string, decimal> _balances = new(StringComparer.Ordinal) { ["GC-1000"] = 50m };

                public decimal Redeem(string code, decimal amount)
                {
                    SimulatedDatabase.Query("select count(*) from gift_card_redemptions where code = @code and at > now() - interval '1 hour'", VelocityCheckCostMs); //?velocity
                    SimulatedDatabase.Execute("update gift_cards set balance = balance - @amount where code = @code", BalanceUpdateCostMs);
                    var balance = _balances[code];
                    var redeemed = Math.Min(balance, amount);
                    _balances[code] = balance - redeemed;
                    return redeemed;
                }
            }
            """, ("velocity", commit >= 22)));
        yield return (Paths.For(Test), """
            using Storefront.GiftCards;

            namespace Storefront.UnitTests.GiftCards;

            public sealed class GiftCardServiceTests
            {
                [Fact]
                public void Redeems_gift_card()
                {
                    var service = new GiftCardService();

                    Assert.Equal(50m, service.Redeem("GC-1000", 80m));
                }
            }
            """);
    }
}

public sealed class RegSquashReturns : SingleTestScenario
{
    public override string Id => "REG-SQUASH-RETURNS";
    public override string Category => "regression";
    public override string Summary => "Second of two unrelated regressions squashed into commit 22: return policy evaluation 90 → 140 ms on every target (carrier pickup lookup).";
    public override TestRef Test { get; } = TestRef.Unit("Returns", "ReturnsPolicyTests", "Evaluates_return_request");

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = 22, Kind = Kinds.Step, Subject = "Offer carrier pickup for eligible returns" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= 22 ? 140 : 90);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Returns", "ReturnsPolicy"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Returns;

            public sealed record ReturnRequest(string OrderId, int DaysSinceDelivery, bool Opened, string PostalCode);

            public sealed record ReturnDecision(bool Accepted, bool PickupAvailable);

            public sealed class ReturnsPolicy
            {
                private const double OrderLookupCostMs = 90;
                private const double PickupAvailabilityCostMs = 50; //?pickup

                public ReturnDecision Evaluate(ReturnRequest request)
                {
                    ArgumentNullException.ThrowIfNull(request);
                    SimulatedDatabase.Query("select * from order_lines where order_id = @id", OrderLookupCostMs);
                    var accepted = request.DaysSinceDelivery <= 30 && !request.Opened;
                    return new ReturnDecision(accepted, PickupAvailable: false); //?!pickup
                    if (!accepted) return new ReturnDecision(false, PickupAvailable: false); //?pickup
                    SimulatedNetwork.Call($"carrier/pickup-slots/{request.PostalCode}", PickupAvailabilityCostMs); //?pickup
                    return new ReturnDecision(true, PickupAvailable: request.PostalCode.Length == 5); //?pickup
                }
            }
            """, ("pickup", commit >= 22)));
        yield return (Paths.For(Test), """
            using Storefront.Returns;

            namespace Storefront.UnitTests.Returns;

            public sealed class ReturnsPolicyTests
            {
                [Fact]
                public void Evaluates_return_request()
                {
                    var decision = new ReturnsPolicy().Evaluate(new ReturnRequest("SO-1", 12, Opened: false, "10115"));

                    Assert.True(decision.Accepted);
                }
            }
            """);
    }
}

/// <summary>Regression caused only by a configuration file.</summary>
public sealed class RegConfig : SingleTestScenario
{
    public override string Id => "REG-CONFIG";
    public override string Category => "regression";
    public override string Summary => "Regression caused by a configuration change only (no code): at commit 98 the embedded appsettings.json raises MaxRetries from 2 to 6, so exhausting retries takes 55 → 155 ms on every target.";
    public override TestRef Test { get; } = TestRef.Integration("Resilience", "RetryPolicyTests", "Falls_back_after_exhausting_retries");
    private const int Culprit = 98;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Raise the default retry budget for partner calls",
            Body = "Partner APIs throttle aggressively during sales; two retries were not\nenough to get through a typical throttling window.",
        },
    ];

    private static int Retries(int commit) => commit >= Culprit ? 6 : 2;

    protected override Dist Model(Target target, int commit) => Dist.Ms((Retries(commit) + 1) * 5 + Retries(commit) * 20);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Resource("appsettings.json"), $$"""
            {
              "Retry": {
                "MaxRetries": {{Retries(commit)}},
                "BackoffMilliseconds": 20,
                "AttemptTimeoutMilliseconds": 5
              },
              "Partners": {
                "BaseAddress": "https://partners.example.internal/",
                "Throttling": "adaptive"
              }
            }
            """);
        yield return (Paths.Src("Resilience", "RetryPolicy"), """
            using System.Text.Json;
            using Storefront.Infrastructure;

            namespace Storefront.Resilience;

            public sealed record RetrySettings(int MaxRetries, double BackoffMilliseconds, double AttemptTimeoutMilliseconds);

            /// <summary>Retries calls to partner APIs with the settings from the embedded appsettings.json.</summary>
            public sealed class RetryPolicy
            {
                public RetrySettings Settings { get; } = LoadSettings();

                public T Execute<T>(Func<int, T?> attempt, T fallback) where T : class
                {
                    ArgumentNullException.ThrowIfNull(attempt);
                    for (var i = 0; i <= Settings.MaxRetries; i++)
                    {
                        if (i > 0) Workload.Spend(Settings.BackoffMilliseconds);
                        SimulatedNetwork.Call("partners/attempt", Settings.AttemptTimeoutMilliseconds);
                        if (attempt(i) is { } result) return result;
                    }
                    return fallback;
                }

                private static RetrySettings LoadSettings()
                {
                    using var stream = typeof(RetryPolicy).Assembly.GetManifestResourceStream("Storefront.Resources.appsettings.json")
                        ?? throw new InvalidOperationException("appsettings.json is not embedded.");
                    using var document = JsonDocument.Parse(stream);
                    var retry = document.RootElement.GetProperty("Retry");
                    return new RetrySettings(
                        retry.GetProperty("MaxRetries").GetInt32(),
                        retry.GetProperty("BackoffMilliseconds").GetDouble(),
                        retry.GetProperty("AttemptTimeoutMilliseconds").GetDouble());
                }
            }
            """);
        yield return (Paths.For(Test), """
            using Storefront.Resilience;

            namespace Storefront.IntegrationTests.Resilience;

            public sealed class RetryPolicyTests
            {
                [Fact]
                public void Falls_back_after_exhausting_retries()
                {
                    var policy = new RetryPolicy();
                    var attempts = 0;

                    var result = policy.Execute<string>(_ => { attempts++; return null; }, "cached-price-list");

                    Assert.Equal("cached-price-list", result);
                    Assert.Equal(policy.Settings.MaxRetries + 1, attempts);
                }
            }
            """);
    }
}

/// <summary>Relative vs absolute impact pair: large absolute, small relative.</summary>
public sealed class RegAbsoluteLarge : SingleTestScenario
{
    public override string Id => "REG-ABSOLUTE-LARGE";
    public override string Category => "regression";
    public override string Summary => "Large absolute but small relative change on every target: 1000 → 1080 ms (+8%, +80 ms) at commit 102 (ledger reconciliation also matches card settlements). Pairs with REG-RELATIVE-LARGE.";
    public override TestRef Test { get; } = TestRef.Integration("Accounting", "LedgerReconcilerTests", "Reconciles_monthly_ledger");
    private const int Culprit = 102;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Subject = "Match card settlements during ledger reconciliation" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit ? 1080 : 1000);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Accounting", "LedgerReconciler"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Accounting;

            public sealed record LedgerEntry(string Reference, decimal Amount);

            public sealed record ReconciliationReport(int Matched, IReadOnlyList<string> Unmatched);

            /// <summary>Reconciles the monthly ledger with the bank statement.</summary>
            public sealed class LedgerReconciler
            {
                private const double StatementDownloadCostMs = 600;
                private const double MatchingCostMs = 400;
                private const double SettlementMatchingCostMs = 80; //?settlements

                public ReconciliationReport Reconcile(IReadOnlyList<LedgerEntry> ledger, IReadOnlyList<LedgerEntry> statement)
                {
                    ArgumentNullException.ThrowIfNull(ledger);
                    ArgumentNullException.ThrowIfNull(statement);
                    SimulatedNetwork.Call("bank/statements/monthly", StatementDownloadCostMs);
                    Compute.Run("match-ledger-entries", MatchingCostMs);
                    SimulatedDatabase.Query("select * from card_settlements where month = @month", SettlementMatchingCostMs); //?settlements
                    var references = statement.Select(s => (s.Reference, s.Amount)).ToHashSet();
                    var unmatched = ledger.Where(e => !references.Contains((e.Reference, e.Amount))).Select(e => e.Reference).ToList();
                    return new ReconciliationReport(ledger.Count - unmatched.Count, unmatched);
                }
            }
            """, ("settlements", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Accounting;

            namespace Storefront.IntegrationTests.Accounting;

            public sealed class LedgerReconcilerTests
            {
                [Fact]
                public void Reconciles_monthly_ledger()
                {
                    var ledger = new[] { new LedgerEntry("TX-1", 10m), new LedgerEntry("TX-2", 20m), new LedgerEntry("TX-3", 30m) };
                    var statement = new[] { new LedgerEntry("TX-1", 10m), new LedgerEntry("TX-3", 30m) };

                    var report = new LedgerReconciler().Reconcile(ledger, statement);

                    Assert.Equal(2, report.Matched);
                    Assert.Equal(["TX-2"], report.Unmatched);
                }
            }
            """);
    }
}

/// <summary>Relative vs absolute impact pair: large relative, tiny absolute.</summary>
public sealed class RegRelativeLarge : SingleTestScenario
{
    public override string Id => "REG-RELATIVE-LARGE";
    public override string Category => "regression";
    public override string Summary => "Large relative but tiny absolute change on every target: 0.3 → 0.9 ms (+200%, +0.6 ms) at commit 104 (SKU normalization applies Unicode compatibility decomposition). Optional to report; pairs with REG-ABSOLUTE-LARGE.";
    public override TestRef Test { get; } = TestRef.Unit("Catalog", "SkuNormalizerTests", "Normalizes_sku_input");
    private const int Culprit = 104;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Tier = Tiers.Optional, Subject = "Apply compatibility decomposition to SKU input" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit ? 0.9 : 0.3);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Catalog", "SkuNormalizer"), Tpl.Code("""
            using System.Text;
            using Storefront.Infrastructure;

            namespace Storefront.Catalog;

            public static class SkuNormalizer
            {
                private const double NormalizeCostMs = 0.3; //?!nfkc
                private const double NormalizeCostMs = 0.9; //?nfkc

                public static string Normalize(string input)
                {
                    ArgumentNullException.ThrowIfNull(input);
                    Compute.Run("normalize-sku", NormalizeCostMs);
                    var text = input.Trim(); //?!nfkc
                    var text = input.Normalize(NormalizationForm.FormKC).Trim(); //?nfkc
                    return text.ToUpperInvariant().Replace(' ', '-');
                }
            }
            """, ("nfkc", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Catalog;

            namespace Storefront.UnitTests.Catalog;

            public sealed class SkuNormalizerTests
            {
                [Fact]
                public void Normalizes_sku_input()
                {
                    Assert.Equal("HOME-000001-01", SkuNormalizer.Normalize("  home 000001 01 "));
                }
            }
            """);
    }
}

/// <summary>Real CPU-bound regression: twice the compression work.</summary>
public sealed class RegCpuBound : SingleTestScenario
{
    public override string Id => "REG-CPU-BOUND";
    public override string Category => "regression";
    public override string Summary => "Real CPU-bound regression at commit 88 on every target: feed compression runs four Deflate passes instead of two, so duration roughly doubles (expected ratio 2.0 ± 0.25). Absolute values depend on the runner CPU.";
    public override TestRef Test { get; } = TestRef.Unit("Feeds", "FeedCompressorTests", "Compresses_product_feed");
    private const int Culprit = 88;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Subject = "Publish compressed feeds for each CDN region" },
    ];

    protected override Dist Model(Target target, int commit) => new Dist.CpuBound(commit >= Culprit ? 4 : 2);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Feeds", "FeedCompressor"), Tpl.Code("""
            using System.IO.Compression;

            namespace Storefront.Feeds;

            /// <summary>Compresses product feeds before they are published.</summary>
            public static class FeedCompressor
            {
                private static readonly string[] Variants = ["default", "partners"]; //?!regions
                private static readonly string[] Variants = ["eu-west", "eu-central", "us-east", "ap-south"]; //?regions

                public static IReadOnlyDictionary<string, int> Compress(byte[] feed)
                {
                    ArgumentNullException.ThrowIfNull(feed);
                    var sizes = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (var variant in Variants)
                    {
                        using var buffer = new MemoryStream();
                        using (var deflate = new DeflateStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
                        {
                            deflate.Write(feed);
                        }
                        sizes[variant] = (int)buffer.Length;
                    }
                    return sizes;
                }
            }
            """, ("regions", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using System.Text;
            using Storefront.Feeds;

            namespace Storefront.UnitTests.Feeds;

            public sealed class FeedCompressorTests
            {
                [Fact]
                public void Compresses_product_feed()
                {
                    var feed = new StringBuilder();
                    for (var i = 0; i < 60_000; i++)
                    {
                        feed.Append("<item sku=\"HOME-").Append((i * 7919 % 1_000_000).ToString("000000")).Append("-01\" price=\"")
                            .Append(i % 997).Append(".99\" stock=\"").Append(i % 13).Append("\"/>\n");
                    }
                    var bytes = Encoding.UTF8.GetBytes(feed.ToString());

                    var sizes = FeedCompressor.Compress(bytes);

                    Assert.All(sizes.Values, size => Assert.InRange(size, 1, bytes.Length / 2));
                }
            }
            """);
    }
}
