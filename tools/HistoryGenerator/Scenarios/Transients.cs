using HistoryGenerator.Model;

namespace HistoryGenerator.Scenarios;

/// <summary>Regression that appeared and was fixed (the user's required case).</summary>
public sealed class RegFixed : SingleTestScenario
{
    public override string Id => "REG-FIXED";
    public override string Category => "regression";
    public override string Summary => "Regression that is later fixed, on every target: 40 → 400 ms at commit 44 (search loads reviews per product, N+1) and back to 40 ms at commit 82 (reviews are aggregated in the search query).";
    public override TestRef Test { get; } = TestRef.Unit("Search", "ProductSearchServiceTests", "Finds_products_by_keyword");
    private const int Culprit = 44;
    private const int Fix = 82;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Rank search results by average review rating",
            Body = "Load the reviews of each matching product and use the average rating as\na secondary sort key.",
        },
        new()
        {
            Commit = Fix, Kind = Kinds.Step, Subject = "Aggregate review ratings inside the search query",
            Body = "Compute the average rating in the search query itself instead of loading\nthe reviews of every result separately.",
            Link = "fixes", LinkCommit = Culprit,
        },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit && commit < Fix ? 40 + 30 * 12 : 40);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        var phase = Phase(commit);
        yield return (Paths.Src("Search", "ProductSearchService"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Search;

            public sealed record SearchHit(string Sku, string Title, double Rating);

            /// <summary>Keyword search over the product catalog.</summary>
            public sealed class ProductSearchService
            {
                private const double SearchQueryCostMs = 40;
                private const double ReviewQueryCostMs = 12; //?nPlusOne

                private static readonly (string Sku, string Title)[] Catalog = Enumerable.Range(1, 30)
                    .Select(i => ($"HOME-{i:000000}-01", i % 2 == 0 ? $"Oak lamp {i}" : $"Brass lamp {i}"))
                    .ToArray();

                public IReadOnlyList<SearchHit> Search(string keyword)
                {
                    ArgumentException.ThrowIfNullOrEmpty(keyword);
                    SimulatedDatabase.Query("select sku, title from products where title ilike @keyword", SearchQueryCostMs); //?!aggregate
                    SimulatedDatabase.Query("select p.sku, p.title, avg(r.rating) from products p left join reviews r using (sku) where p.title ilike @keyword group by p.sku, p.title", SearchQueryCostMs); //?aggregate
                    var hits = new List<SearchHit>();
                    foreach (var (sku, title) in Catalog)
                    {
                        if (!title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue; //?!nPlusOne
                        SimulatedDatabase.Query("select rating from reviews where sku = @sku", ReviewQueryCostMs); //?nPlusOne
                        if (!title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue; //?nPlusOne
                        hits.Add(new SearchHit(sku, title, Rating: sku.Length % 5));
                    }
                    return hits.OrderByDescending(h => h.Rating).ThenBy(h => h.Sku, StringComparer.Ordinal).ToList();
                }
            }
            """, ("nPlusOne", phase == 1), ("aggregate", phase == 2)));
        yield return (Paths.For(Test), """
            using Storefront.Search;

            namespace Storefront.UnitTests.Search;

            public sealed class ProductSearchServiceTests
            {
                [Fact]
                public void Finds_products_by_keyword()
                {
                    var hits = new ProductSearchService().Search("lamp");

                    Assert.Equal(30, hits.Count);
                }
            }
            """);
    }
}

/// <summary>Regression, partially fixed later.</summary>
public sealed class RegPartialFix : SingleTestScenario
{
    public override string Id => "REG-PARTIAL-FIX";
    public override string Category => "regression";
    public override string Summary => "Regression that is only partially fixed, on every target: 80 → 320 ms at commit 50 (per-line tax breakdown in the sales report) and 320 → 140 ms at commit 78 (breakdown cached per tax category). Still 75% above the original level.";
    public override TestRef Test { get; } = TestRef.Integration("Reporting", "SalesReportExporterTests", "Exports_sales_report_csv");
    private const int Culprit = 50;
    private const int PartialFix = 78;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Subject = "Include a tax breakdown for every sales report line" },
        new()
        {
            Commit = PartialFix, Kind = Kinds.Step, Subject = "Cache tax breakdowns per tax category in sales reports",
            Link = "partially_fixes", LinkCommit = Culprit,
        },
    ];

    protected override Dist Model(Target target, int commit) =>
        Dist.Ms(commit >= PartialFix ? 80 + 10 * 6 : commit >= Culprit ? 80 + 40 * 6 : 80);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        var phase = Phase(commit);
        yield return (Paths.Src("Reporting", "SalesReportExporter"), Tpl.Code("""
            using System.Globalization;
            using System.Text;
            using Storefront.Infrastructure;

            namespace Storefront.Reporting;

            public sealed record SalesLine(string Sku, string TaxCategory, int Quantity, decimal Net);

            /// <summary>Exports the daily sales report for finance.</summary>
            public sealed class SalesReportExporter
            {
                private const double QueryCostMs = 60;
                private const double UploadCostMs = 20;
                private const double TaxBreakdownCostMs = 6; //?breakdown

                public string ExportCsv(IReadOnlyList<SalesLine> lines)
                {
                    ArgumentNullException.ThrowIfNull(lines);
                    SimulatedDatabase.Query("select * from sales where day = current_date", QueryCostMs);
                    var csv = new StringBuilder("sku,quantity,net\n"); //?!breakdown
                    var csv = new StringBuilder("sku,quantity,net,tax\n"); //?breakdown
                    var breakdowns = new Dictionary<string, decimal>(StringComparer.Ordinal); //?cache
                    foreach (var line in lines)
                    {
                        csv.Append(CultureInfo.InvariantCulture, $"{line.Sku},{line.Quantity},{line.Net:0.00}\n"); //?!breakdown
                        var rate = TaxRate(line.TaxCategory); //?perLine
                        if (!breakdowns.TryGetValue(line.TaxCategory, out var rate)) //?cache
                        { //?cache
                            rate = TaxRate(line.TaxCategory); //?cache
                            breakdowns[line.TaxCategory] = rate; //?cache
                        } //?cache
                        csv.Append(CultureInfo.InvariantCulture, $"{line.Sku},{line.Quantity},{line.Net:0.00},{line.Net * rate:0.00}\n"); //?breakdown
                    }
                    SimulatedStorage.Write("reports/sales.csv", UploadCostMs);
                    return csv.ToString();
                }
                //?breakdown
                private static decimal TaxRate(string category) //?breakdown
                { //?breakdown
                    SimulatedNetwork.Call($"tax-service/breakdown/{category}", TaxBreakdownCostMs); //?breakdown
                    return category.StartsWith("reduced", StringComparison.Ordinal) ? 0.07m : 0.19m; //?breakdown
                } //?breakdown
            }
            """, ("breakdown", phase >= 1), ("perLine", phase == 1), ("cache", phase >= 2)));
        yield return (Paths.For(Test), """
            using Storefront.Reporting;

            namespace Storefront.IntegrationTests.Reporting;

            public sealed class SalesReportExporterTests
            {
                [Fact]
                public void Exports_sales_report_csv()
                {
                    var lines = Enumerable.Range(1, 40)
                        .Select(i => new SalesLine($"HOME-{i:000000}-01", $"{(i % 2 == 0 ? "reduced" : "standard")}-{i % 5}", i % 3 + 1, 10m * i))
                        .ToList();

                    var csv = new SalesReportExporter().ExportCsv(lines);

                    Assert.Equal(41, csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
                }
            }
            """);
    }
}

/// <summary>Regression that is reverted and later reapplied.</summary>
public sealed class RegReintroduced : SingleTestScenario
{
    public override string Id => "REG-REINTRODUCED";
    public override string Category => "regression";
    public override string Summary => "Regression that is fixed by a revert and comes back, on every target: 20 → 180 ms at commit 28 (coupon eligibility checks order history, N+1), reverted at commit 54 (back to 20 ms), reapplied at commit 92 (180 ms again).";
    public override TestRef Test { get; } = TestRef.Unit("Promotions", "CouponServiceTests", "Applies_percentage_coupon");
    private const int Culprit = 28;
    private const int Revert = 54;
    private const int Reapply = 92;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Check coupon eligibility against the order history",
            Body = "First-order coupons were redeemed by returning customers. Check each\nprevious order of the customer before applying the coupon.",
        },
        new()
        {
            Commit = Revert, Kind = Kinds.Step, Subject = "(git revert)", RevertOf = Culprit, Link = "reverts", LinkCommit = Culprit,
            Body = "Checkout is blocked on the eligibility check during peak hours; the check\nwill come back once order history is cached.",
        },
        new()
        {
            Commit = Reapply, Kind = Kinds.Step, Subject = "(git revert of the revert)", RevertOf = Revert, Link = "reintroduces", LinkCommit = Culprit,
            Body = "The order history cache did not ship, but first-order coupon abuse is\nback, so reinstate the check.",
        },
    ];

    private static bool CheckActive(int commit) => (commit >= Culprit && commit < Revert) || commit >= Reapply;

    protected override Dist Model(Target target, int commit) => Dist.Ms(CheckActive(commit) ? 20 + 8 * 20 : 20);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Promotions", "CouponService"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Promotions;

            public sealed record Coupon(string Code, decimal Percent, bool FirstOrderOnly);

            public sealed class CouponService
            {
                private const double CouponLookupCostMs = 20;
                private const double OrderLookupCostMs = 20; //?history

                public decimal Apply(Coupon coupon, string customerId, IReadOnlyList<string> previousOrders, decimal total)
                {
                    ArgumentNullException.ThrowIfNull(coupon);
                    ArgumentNullException.ThrowIfNull(previousOrders);
                    SimulatedDatabase.Query("select * from coupons where code = @code", CouponLookupCostMs);
                    if (coupon.FirstOrderOnly) //?history
                    { //?history
                        foreach (var orderId in previousOrders) //?history
                        { //?history
                            SimulatedDatabase.Query("select status from orders where id = @id", OrderLookupCostMs); //?history
                            if (orderId.EndsWith("-delivered", StringComparison.Ordinal)) return total; //?history
                        } //?history
                    } //?history
                    return Math.Round(total * (1 - coupon.Percent / 100m), 2);
                }
            }
            """, ("history", CheckActive(commit))));
        yield return (Paths.For(Test), """
            using Storefront.Promotions;

            namespace Storefront.UnitTests.Promotions;

            public sealed class CouponServiceTests
            {
                [Fact]
                public void Applies_percentage_coupon()
                {
                    var previousOrders = Enumerable.Range(1, 8).Select(i => $"SO-{i}-cancelled").ToList();

                    var total = new CouponService().Apply(new Coupon("WELCOME10", 10m, FirstOrderOnly: true), "customer-1", previousOrders, 80m);

                    Assert.Equal(72m, total);
                }
            }
            """);
    }
}

/// <summary>Optimization that is reverted later (the revert is a regression).</summary>
public sealed class OptReverted : SingleTestScenario
{
    public override string Id => "OPT-REVERTED";
    public override string Category => "optimization";
    public override string Summary => "Optimization that is later reverted, on every target: 150 → 50 ms at commit 40 (rules are precompiled at publish time instead of on every evaluation), back to 150 ms at commit 94 (git revert because edited promotions were not recompiled). The revert is a regression against the preceding level.";
    public override TestRef Test { get; } = TestRef.Unit("Promotions", "PromotionEngineTests", "Evaluates_promotions_for_cart");
    private const int Optimization = 40;
    private const int Revert = 94;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Optimization, Kind = Kinds.Step, Subject = "Use promotion rules precompiled at publish time" },
        new()
        {
            Commit = Revert, Kind = Kinds.Step, Subject = "(git revert)", RevertOf = Optimization, Link = "reverts", LinkCommit = Optimization,
            Body = "Promotions edited in the admin UI kept their old rules, because only the\npublish pipeline recompiled them.",
        },
    ];

    private static bool Cached(int commit) => commit >= Optimization && commit < Revert;

    protected override Dist Model(Target target, int commit) => Dist.Ms(Cached(commit) ? 50 : 150);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Promotions", "PromotionEngine"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Promotions;

            public sealed record Promotion(string Id, string Rule, decimal Discount);

            /// <summary>Evaluates the active promotions for a cart.</summary>
            public sealed class PromotionEngine
            {
                private const double RuleCompilationCostMs = 100; //?!cache
                private const double EvaluationCostMs = 50;

                private static readonly Func<decimal, bool>[] PrecompiledRules = [total => total >= 50m, total => total >= 100m]; //?cache
                //?cache
                public decimal Evaluate(IReadOnlyList<Promotion> promotions, decimal cartTotal)
                {
                    ArgumentNullException.ThrowIfNull(promotions);
                    var rules = CompileRules(); //?!cache
                    IReadOnlyList<Func<decimal, bool>> rules = PrecompiledRules; //?cache
                    Compute.Run("evaluate-promotions", EvaluationCostMs);
                    return promotions.Where((p, i) => rules[i % rules.Count](cartTotal)).Sum(p => p.Discount);
                }
                //?!cache
                private static IReadOnlyList<Func<decimal, bool>> CompileRules() //?!cache
                { //?!cache
                    Compute.Run("compile-promotion-rules", RuleCompilationCostMs); //?!cache
                    return [total => total >= 50m, total => total >= 100m]; //?!cache
                } //?!cache
            }
            """, ("cache", Cached(commit))));
        yield return (Paths.For(Test), """
            using Storefront.Promotions;

            namespace Storefront.UnitTests.Promotions;

            public sealed class PromotionEngineTests
            {
                [Fact]
                public void Evaluates_promotions_for_cart()
                {
                    var promotions = new[] { new Promotion("P1", "total>=50", 5m), new Promotion("P2", "total>=100", 10m) };

                    Assert.Equal(5m, new PromotionEngine().Evaluate(promotions, 75m));
                }
            }
            """);
    }

}

/// <summary>Bad commit fixed by the very next commit (one data point).</summary>
public sealed class RegHotfix : SingleTestScenario
{
    public override string Id => "REG-HOTFIX-ONE-COMMIT";
    public override string Category => "regression";
    public override string Summary => "One-commit regression on every target: 35 → 350 ms at commit 106 only (card BIN lookup calls the issuer registry), hotfixed at commit 107. A single point, but it appears on all four targets at once, unlike random outliers.";
    public override TestRef Test { get; } = TestRef.Unit("Payments", "PaymentGatewayTests", "Authorizes_card_payment");
    private const int Culprit = 106;
    private const int Hotfix = 107;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Tier = Tiers.Should, Subject = "Verify card BIN against the issuer registry" },
        new()
        {
            Commit = Hotfix, Kind = Kinds.Step, Tier = Tiers.Should, Subject = "Hotfix: read card BIN data from the local registry copy",
            Link = "fixes", LinkCommit = Culprit,
        },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit == Culprit ? 350 : 35);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        var phase = Phase(commit);
        yield return (Paths.Src("Payments", "PaymentGateway"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Payments;

            public sealed record CardPayment(string CardNumber, decimal Amount, string Currency);

            public sealed record Authorization(bool Approved, string Reference, string Issuer);

            public sealed class PaymentGateway
            {
                private const double AuthorizeCostMs = 35;
                private const double IssuerRegistryCostMs = 315; //?remote

                private static readonly IReadOnlyDictionary<string, string> LocalBinTable = new Dictionary<string, string> //?local
                { //?local
                    ["4111"] = "Visa test issuer", //?local
                    ["5555"] = "Mastercard test issuer", //?local
                }; //?local
                //?local
                public Authorization Authorize(CardPayment payment)
                {
                    ArgumentNullException.ThrowIfNull(payment);
                    var bin = payment.CardNumber[..4];
                    var issuer = "unknown"; //?!checked
                    SimulatedNetwork.Call($"issuer-registry/bins/{bin}", IssuerRegistryCostMs); //?remote
                    var issuer = bin == "4111" ? "Visa test issuer" : "unknown"; //?remote
                    var issuer = LocalBinTable.GetValueOrDefault(bin, "unknown"); //?local
                    SimulatedNetwork.Call("acquirer/authorize", AuthorizeCostMs);
                    return new Authorization(payment.Amount < 10_000m, $"AUTH-{payment.CardNumber[^4..]}", issuer);
                }
            }
            """, ("remote", phase == 1), ("local", phase >= 2), ("checked", phase >= 1)));
        yield return (Paths.For(Test), """
            using Storefront.Payments;

            namespace Storefront.UnitTests.Payments;

            public sealed class PaymentGatewayTests
            {
                [Fact]
                public void Authorizes_card_payment()
                {
                    var authorization = new PaymentGateway().Authorize(new CardPayment("4111111111111111", 49.90m, "EUR"));

                    Assert.True(authorization.Approved);
                    Assert.Equal("AUTH-1111", authorization.Reference);
                }
            }
            """);
    }
}

/// <summary>Short-lived regression: three data points.</summary>
public sealed class RegShortLived : SingleTestScenario
{
    public override string Id => "REG-SHORT-LIVED";
    public override string Category => "regression";
    public override string Summary => "Short-lived regression on every target: 30 → 90 ms at commit 110 (gift message previews rendered as images), fixed at commit 113 (rendered lazily). Three points at the higher level.";
    public override TestRef Test { get; } = TestRef.Unit("Orders", "GiftWrapServiceTests", "Wraps_order_with_message");
    private const int Culprit = 110;
    private const int Fix = 113;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Culprit, Kind = Kinds.Step, Subject = "Render gift message previews as images" },
        new() { Commit = Fix, Kind = Kinds.Step, Subject = "Render gift message previews on first view", Link = "fixes", LinkCommit = Culprit },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Culprit && commit < Fix ? 90 : 30);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        var phase = Phase(commit);
        yield return (Paths.Src("Orders", "GiftWrapService"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Orders;

            public sealed record GiftWrap(string OrderId, string Message, string? PreviewUrl);

            public sealed class GiftWrapService
            {
                private const double SaveCostMs = 30;
                private const double PreviewRenderCostMs = 60; //?eager

                public GiftWrap Wrap(string orderId, string message)
                {
                    ArgumentException.ThrowIfNullOrEmpty(orderId);
                    SimulatedDatabase.Execute("insert into gift_wraps values (@order, @message)", SaveCostMs);
                    return new GiftWrap(orderId, message.Trim(), PreviewUrl: null); //?!preview
                    Compute.Run("render-gift-message-preview", PreviewRenderCostMs); //?eager
                    return new GiftWrap(orderId, message.Trim(), $"previews/{orderId}.png"); //?preview
                }
            }
            """, ("eager", phase == 1), ("preview", phase >= 1)));
        yield return (Paths.For(Test), """
            using Storefront.Orders;

            namespace Storefront.UnitTests.Orders;

            public sealed class GiftWrapServiceTests
            {
                [Fact]
                public void Wraps_order_with_message()
                {
                    var wrap = new GiftWrapService().Wrap("SO-7", "  Happy birthday!  ");

                    Assert.Equal("Happy birthday!", wrap.Message);
                }
            }
            """);
    }
}
