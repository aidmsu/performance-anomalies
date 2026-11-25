using HistoryGenerator.Model;

namespace HistoryGenerator.Scenarios;

/// <summary>A regression lands while the test is skipped; it shows up only when the test is re-enabled.</summary>
public sealed class LifeGapWithChange : SingleTestScenario
{
    public override string Id => "LIFE-GAP-WITH-CHANGE";
    public override string Category => "lifecycle";
    public override string Summary => "Regression hidden in a gap: the test is skipped at commit 10 (TRX outcome NotExecuted). Commit 12 makes the code 50 → 110 ms on every target while nobody measures it. The test is re-enabled at commit 16. The culprit is commit 12, not 16; any window [12, 18] is acceptable.";
    public override TestRef Test { get; } = TestRef.Unit("Loyalty", "LoyaltyPointsCalculatorTests", "Accrues_points_for_order");
    private const int Skip = 10;
    private const int Culprit = 12;
    private const int Unskip = 16;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Skip, Kind = Kinds.TestSkipped, Tier = Tiers.MustNot, Direction = Directions.Neutral, Subject = "Skip the loyalty points test until the ledger fixture is fixed" },
        new()
        {
            Commit = Culprit, Kind = Kinds.HiddenStep, Subject = "Recalculate the tier bonus on every points accrual",
            Body = "Tier bonuses were only recalculated nightly, so customers saw stale bonus\nrates right after an upgrade.",
        },
        new() { Commit = Unskip, Kind = Kinds.TestUnskipped, Tier = Tiers.MustNot, Direction = Directions.Neutral, Subject = "Re-enable the loyalty points test" },
    ];

    protected override Dist Model(Target target, int commit)
    {
        Dist cost = Dist.Ms(commit >= Culprit ? 110 : 50);
        return commit >= Skip && commit < Unskip ? new Dist.Skipped(cost) : cost;
    }

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Loyalty", "LoyaltyPointsCalculator"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Loyalty;

            public sealed class LoyaltyPointsCalculator
            {
                private const double LedgerWriteCostMs = 50;
                private const double TierBonusQueryCostMs = 60; //?bonus

                public int Accrue(string customerId, decimal orderTotal)
                {
                    ArgumentException.ThrowIfNullOrEmpty(customerId);
                    var points = (int)Math.Floor(orderTotal);
                    SimulatedDatabase.Query("select tier, bonus_rate from loyalty_tiers where customer_id = @id", TierBonusQueryCostMs); //?bonus
                    points += points / 10; //?bonus
                    SimulatedDatabase.Execute("insert into points_ledger values (@id, @points)", LedgerWriteCostMs);
                    return points;
                }
            }
            """, ("bonus", commit >= Culprit)));
        yield return (Paths.For(Test), Tpl.Code("""
            using Storefront.Loyalty;

            namespace Storefront.UnitTests.Loyalty;

            public sealed class LoyaltyPointsCalculatorTests
            {
                [Fact] //?!skip
                [Fact(Skip = "Ledger fixture is flaky; re-enable once it is isolated")] //?skip
                public void Accrues_points_for_order()
                {
                    var points = new LoyaltyPointsCalculator().Accrue("customer-1", 125.40m);

                    Assert.InRange(points, 125, 140);
                }
            }
            """, ("skip", commit >= Skip && commit < Unskip)));
    }
}

/// <summary>A slow test appears mid-history and then regresses after a short baseline.</summary>
public sealed class LifeNewSlowTest : SingleTestScenario
{
    public override string Id => "LIFE-NEW-SLOW-TEST";
    public override string Category => "lifecycle";
    public override string Summary => "A new, already slow test appears at commit 84 (900 ms; the suite total grows, but no test regressed). After a 6-point baseline it regresses 900 → 1140 ms at commit 90 on every target.";
    public override TestRef Test { get; } = TestRef.Integration("Imports", "SupplierBulkImporterTests", "Imports_large_batch");
    protected override int FirstCommit => 84;
    private const int Regression = 90;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = 84, Kind = Kinds.TestAdded, Tier = Tiers.MustNot, Direction = Directions.Neutral, Subject = "Add bulk import for supplier catalogs" },
        new() { Commit = Regression, Kind = Kinds.Step, Tier = Tiers.Should, Subject = "Validate supplier SKUs during bulk import" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Regression ? 900 + 30 * 8 : 900);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        if (!Exists(commit)) yield break;
        yield return (Paths.Src("Imports", "SupplierBulkImporter"), Tpl.Code("""
            using Storefront.Domain;
            using Storefront.Infrastructure;

            namespace Storefront.Imports;

            public sealed record SupplierRow(string Sku, int Quantity, decimal Cost);

            /// <summary>Imports full supplier catalogs in batches.</summary>
            public sealed class SupplierBulkImporter
            {
                private const int BatchSize = 100;
                private const double BatchWriteCostMs = 30;
                private const double SkuValidationCostMs = 8; //?validate

                public int Import(IReadOnlyList<SupplierRow> rows)
                {
                    ArgumentNullException.ThrowIfNull(rows);
                    var imported = 0;
                    foreach (var batch in rows.Chunk(BatchSize))
                    {
                        SimulatedDatabase.Query("select sku from catalog where sku in (@batch)", SkuValidationCostMs); //?validate
                        SimulatedDatabase.Execute("copy supplier_stock from stdin", BatchWriteCostMs);
                        imported += batch.Count(r => Sku.TryParse(r.Sku, out _));
                    }
                    return imported;
                }
            }
            """, ("validate", commit >= Regression)));
        yield return (Paths.For(Test), """
            using Storefront.Imports;

            namespace Storefront.IntegrationTests.Imports;

            public sealed class SupplierBulkImporterTests
            {
                [Fact]
                public void Imports_large_batch()
                {
                    var rows = Enumerable.Range(1, 3000).Select(i => new SupplierRow($"SUPP-{i:000000}-01", i % 40, 2.5m)).ToList();

                    Assert.Equal(3000, new SupplierBulkImporter().Import(rows));
                }
            }
            """);
    }
}

/// <summary>A test is renamed; its duration does not change.</summary>
public sealed class LifeRenamed : Scenario
{
    public override string Id => "LIFE-RENAMED";
    public override string Category => "lifecycle";
    public override string Summary => "Test rename at commit 108: WarehouseAllocatorTests.Allocates_stock becomes Allocates_stock_across_warehouses with the same 120 ms. The history of the new name should be linked to the old one (renamed_from); neither name regressed.";
    private const int Rename = 108;
    private static readonly TestRef Old = TestRef.Unit("Inventory", "WarehouseAllocatorTests", "Allocates_stock");
    private static readonly TestRef New = TestRef.Unit("Inventory", "WarehouseAllocatorTests", "Allocates_stock_across_warehouses");

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Rename, Kind = Kinds.TestRenamed, Tier = Tiers.MustNot, Direction = Directions.Neutral, Subject = "Rename the warehouse allocation test after its behaviour", Note = "renamed_from: Storefront.UnitTests.Inventory.WarehouseAllocatorTests.Allocates_stock" },
    ];

    public override IEnumerable<TestRef> Tests(int commit) => [commit >= Rename ? New : Old];

    public override Dist Expected(TestRef test, Target target, int commit) =>
        test == (commit >= Rename ? New : Old) ? Dist.Ms(120) : new Dist.Absent();

    public override void Render(int commit, Snapshot snapshot)
    {
        snapshot.Add(Paths.Src("Inventory", "WarehouseAllocator"), """
            using Storefront.Infrastructure;

            namespace Storefront.Inventory;

            public sealed record WarehouseStock(string Warehouse, int Available);

            public sealed class WarehouseAllocator
            {
                private const double StockQueryCostMs = 120;

                public IReadOnlyDictionary<string, int> Allocate(int quantity, IReadOnlyList<WarehouseStock> stock)
                {
                    ArgumentNullException.ThrowIfNull(stock);
                    SimulatedDatabase.Query("select warehouse, available from stock for update", StockQueryCostMs);
                    var allocation = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (var s in stock.OrderByDescending(s => s.Available))
                    {
                        if (quantity == 0) break;
                        var take = Math.Min(quantity, s.Available);
                        if (take == 0) continue;
                        allocation[s.Warehouse] = take;
                        quantity -= take;
                    }
                    return allocation;
                }
            }
            """, Id);
        snapshot.Add(Paths.For(Old), Tpl.Code("""
            using Storefront.Inventory;

            namespace Storefront.UnitTests.Inventory;

            public sealed class WarehouseAllocatorTests
            {
                [Fact]
                public void Allocates_stock() //?!renamed
                public void Allocates_stock_across_warehouses() //?renamed
                {
                    var allocation = new WarehouseAllocator().Allocate(7, [new WarehouseStock("berlin", 5), new WarehouseStock("hamburg", 4)]);

                    Assert.Equal(5, allocation["berlin"]);
                    Assert.Equal(2, allocation["hamburg"]);
                }
            }
            """, ("renamed", commit >= Rename)), Id);
    }
}

/// <summary>A test and its code are deleted.</summary>
public sealed class LifeRemoved : SingleTestScenario
{
    public override string Id => "LIFE-REMOVED";
    public override string Category => "lifecycle";
    public override string Summary => "Test removal at commit 112: LegacyXmlExporterTests.Exports_orders_as_xml (90 ms) and the legacy exporter are deleted. The suite total drops, but nothing improved.";
    public override TestRef Test { get; } = TestRef.Unit("Exports", "LegacyXmlExporterTests", "Exports_orders_as_xml");
    protected override int? RemovedAt => 112;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = 112, Kind = Kinds.TestRemoved, Tier = Tiers.MustNot, Direction = Directions.Neutral, Subject = "Remove the legacy XML order export" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(90);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        if (!Exists(commit)) yield break;
        yield return (Paths.Src("Exports", "LegacyXmlExporter"), """
            using System.Xml.Linq;
            using Storefront.Infrastructure;

            namespace Storefront.Exports;

            /// <summary>XML order export for the old ERP connector.</summary>
            public static class LegacyXmlExporter
            {
                private const double OrderQueryCostMs = 90;

                public static string Export(IEnumerable<string> orderIds)
                {
                    SimulatedDatabase.Query("select * from orders where id in (@ids)", OrderQueryCostMs);
                    return new XElement("orders", orderIds.Select(id => new XElement("order", new XAttribute("id", id)))).ToString(SaveOptions.DisableFormatting);
                }
            }
            """);
        yield return (Paths.For(Test), """
            using Storefront.Exports;

            namespace Storefront.UnitTests.Exports;

            public sealed class LegacyXmlExporterTests
            {
                [Fact]
                public void Exports_orders_as_xml()
                {
                    Assert.Equal("<orders><order id=\"SO-1\" /></orders>", LegacyXmlExporter.Export(["SO-1"]));
                }
            }
            """);
    }
}

/// <summary>The test itself does more work; production code is unchanged.</summary>
public sealed class TestScopeChange : SingleTestScenario
{
    public override string Id => "TEST-SCOPE-CHANGE";
    public override string Category => "lifecycle";
    public override string Summary => "Test-scope change at commit 76: the test now covers 12 regions instead of 3, so it goes 49.5 → 198 ms on every target. Only tests/ changed. Reporting it as a product regression is a misclassification.";
    public override TestRef Test { get; } = TestRef.Unit("Tax", "TaxCalculatorTests", "Calculates_tax_for_all_regions");
    private const int Change = 76;
    private const double PerRegionMs = 16.5;

    private static readonly string[] Regions = ["DE", "FR", "NL", "PL", "AT", "BE", "CZ", "DK", "ES", "IT", "PT", "SE"];

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new() { Commit = Change, Kind = Kinds.TestScope, Tier = Tiers.Optional, Direction = Directions.Neutral, Subject = "Cover every supported region in tax calculator tests" },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms((commit >= Change ? 12 : 3) * PerRegionMs);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Tax", "TaxCalculator"), """
            using Storefront.Infrastructure;

            namespace Storefront.Tax;

            public static class TaxCalculator
            {
                private const double RuleLookupCostMs = 16.5;

                private static readonly IReadOnlyDictionary<string, decimal> StandardRates = new Dictionary<string, decimal>
                {
                    ["DE"] = 0.19m, ["FR"] = 0.20m, ["NL"] = 0.21m, ["PL"] = 0.23m, ["AT"] = 0.20m, ["BE"] = 0.21m,
                    ["CZ"] = 0.21m, ["DK"] = 0.25m, ["ES"] = 0.21m, ["IT"] = 0.22m, ["PT"] = 0.23m, ["SE"] = 0.25m,
                };

                public static decimal Calculate(string region, decimal net)
                {
                    SimulatedDatabase.Query("select * from tax_rules where region = @region", RuleLookupCostMs);
                    return Math.Round(net * StandardRates[region], 2);
                }
            }
            """);
        var regions = string.Join(", ", (commit >= Change ? Regions : Regions.Take(3)).Select(r => $"\"{r}\""));
        yield return (Paths.For(Test), $$"""
            using Storefront.Tax;

            namespace Storefront.UnitTests.Tax;

            public sealed class TaxCalculatorTests
            {
                private static readonly string[] Regions = [{{regions}}];

                [Fact]
                public void Calculates_tax_for_all_regions()
                {
                    foreach (var region in Regions)
                    {
                        var tax = TaxCalculator.Calculate(region, 100m);

                        Assert.InRange(tax, 15m, 30m);
                    }
                }
            }
            """);
    }
}

/// <summary>One shared change slows six tests in two projects.</summary>
public sealed class RegFanout : MultiTestScenario
{
    public override string Id => "REG-FANOUT";
    public override string Category => "regression";
    public override string Summary => "Fan-out from one cause at commit 68 on every target: the shared order payload serializer validates against a JSON schema, adding 60 ms to six tests in two projects (baselines 10, 25, 40, 80, 150 and 300 ms; +600% down to +20%). All six events share one cause_group.";
    private const int Culprit = 68;
    private const double SchemaValidationMs = 60;

    private static readonly (TestRef Test, string Method, double BaseMs)[] Operations =
    [
        (TestRef.Unit("Orders", "OrderApiTests", "Returns_order_summary"), "GetSummary", 10),
        (TestRef.Unit("Orders", "OrderApiTests", "Returns_order_details"), "GetDetails", 25),
        (TestRef.Unit("Orders", "OrderApiTests", "Returns_order_history"), "GetHistory", 40),
        (TestRef.Unit("Orders", "OrderApiTests", "Returns_order_export"), "GetExport", 80),
        (TestRef.Integration("Orders", "OrderApiContractTests", "Matches_order_contract"), "GetContract", 150),
        (TestRef.Integration("Orders", "OrderApiContractTests", "Matches_bulk_order_contract"), "GetBulkContract", 300),
    ];

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Step, Subject = "Validate order API payloads against the published JSON schema",
            Body = "Partners rejected payloads with missing optional fields; validate every\npayload before it leaves the API.",
        },
    ];

    public override IEnumerable<TestRef> Tests(int commit) => Operations.Select(o => o.Test);

    public override Dist Expected(TestRef test, Target target, int commit)
    {
        foreach (var op in Operations)
            if (op.Test == test) return Dist.Ms(op.BaseMs + (commit >= Culprit ? SchemaValidationMs : 0));
        return new Dist.Absent();
    }

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Orders", "OrderPayloadSerializer"), Tpl.Code("""
            using System.Text.Json;
            using Storefront.Infrastructure; //?schema

            namespace Storefront.Orders;

            /// <summary>Serializes every payload returned by the order API.</summary>
            public static class OrderPayloadSerializer
            {
                private const double SchemaValidationCostMs = 60; //?schema

                private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

                public static string Serialize<T>(T payload)
                {
                    var json = JsonSerializer.Serialize(payload, Options);
                    Compute.Run("validate-order-schema", SchemaValidationCostMs); //?schema
                    return json;
                }
            }
            """, ("schema", commit >= Culprit)));
        var methods = string.Join("\n\n", Operations.Select(op => $$"""
                public string {{op.Method}}(string orderId)
                {
                    ArgumentException.ThrowIfNullOrEmpty(orderId);
                    SimulatedDatabase.Query("order-api.{{op.Method.ToLowerInvariant()}}", {{Fmt.N(op.BaseMs)}});
                    return OrderPayloadSerializer.Serialize(new OrderPayload(orderId, "{{op.Method}}", "shipped"));
                }
            """));
        yield return (Paths.Src("Orders", "OrderApi"), $$"""
            using Storefront.Infrastructure;

            namespace Storefront.Orders;

            public sealed record OrderPayload(string OrderId, string View, string Status);

            /// <summary>Read side of the public order API.</summary>
            public sealed class OrderApi
            {
            {{methods}}
            }
            """);
        string TestClass(string project, string cls, IEnumerable<(TestRef Test, string Method, double BaseMs)> ops)
        {
            var tests = string.Join("\n\n", ops.Select(op => $$"""
                    [Fact]
                    public void {{op.Test.Method}}()
                    {
                        var json = new OrderApi().{{op.Method}}("SO-100042");

                        Assert.Contains("\"orderId\":\"SO-100042\"", json, StringComparison.Ordinal);
                    }
                """));
            return $$"""
                using Storefront.Orders;

                namespace {{project}}.Orders;

                public sealed class {{cls}}
                {
                {{tests}}
                }
                """;
        }
        yield return (Paths.Unit("Orders", "OrderApiTests"), TestClass("Storefront.UnitTests", "OrderApiTests", Operations.Where(o => o.Test.Project == "Storefront.UnitTests")));
        yield return (Paths.Integration("Orders", "OrderApiContractTests"), TestClass("Storefront.IntegrationTests", "OrderApiContractTests", Operations.Where(o => o.Test.Project == "Storefront.IntegrationTests")));
    }
}
