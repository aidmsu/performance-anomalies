using HistoryGenerator.Model;

namespace HistoryGenerator.Scenarios;

/// <summary>Modality change: stable → bimodal (the user's "1 s, then 1 s or 2 s" case).</summary>
public sealed class RegBimodal : SingleTestScenario
{
    public override string Id => "REG-BIMODAL";
    public override string Category => "regression";
    public override string Summary => "Modality change on every target at commit 62: a stable 100 ms becomes a 50/50 mixture of 100 ms and 200 ms (sessions are read from the distributed cache first and half of the reads miss). The median barely moves; the distribution does.";
    public override TestRef Test { get; } = TestRef.Unit("Sessions", "SessionStoreTests", "Restores_customer_session");
    private const int Culprit = 62;
    private const string Key = "SessionStore.Restore";

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Modality, Direction = Directions.Regression,
            Subject = "Restore sessions from the distributed cache first",
            Body = "Sessions are now written to the distributed cache on every change; reads\ntry the cache before the session table.",
        },
    ];

    protected override Dist Model(Target target, int commit) =>
        commit >= Culprit ? new Dist.Mixture(0.5, 100, 200, Key) : Dist.Ms(100);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Sessions", "SessionStore"), Tpl.Code($$"""
            using Storefront.Infrastructure;

            namespace Storefront.Sessions;

            public sealed record CustomerSession(string SessionId, string CustomerId, IReadOnlyList<string> CartSkus);

            public sealed class SessionStore
            {
                private const double SessionTableReadCostMs = 100; //?!cache
                private const double CacheReadCostMs = 100; //?cache
                private const double SessionTableFallbackCostMs = 100; //?cache
                private const double CacheHitRatio = 0.5; //?cache

                public CustomerSession Restore(string sessionId)
                {
                    ArgumentException.ThrowIfNullOrEmpty(sessionId);
                    SimulatedDatabase.Query("select payload from sessions where id = @id", SessionTableReadCostMs); //?!cache
                    SimulatedNetwork.Call("session-cache/get", CacheReadCostMs); //?cache
                    if (Workload.Random("{{Key}}").NextDouble() >= CacheHitRatio) //?cache
                    { //?cache
                        // Cache miss: the session was written before the cache was populated. //?cache
                        SimulatedDatabase.Query("select payload from sessions where id = @id", SessionTableFallbackCostMs); //?cache
                    } //?cache
                    return new CustomerSession(sessionId, "customer-" + sessionId[^4..], ["HOME-000001-01"]);
                }
            }
            """, ("cache", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Sessions;

            namespace Storefront.UnitTests.Sessions;

            public sealed class SessionStoreTests
            {
                [Fact]
                public void Restores_customer_session()
                {
                    var session = new SessionStore().Restore("sess-00001234");

                    Assert.Equal("customer-1234", session.CustomerId);
                    Assert.Single(session.CartSkus);
                }
            }
            """);
    }
}

/// <summary>Modality improvement: bimodal → stable.</summary>
public sealed class OptBimodalFixed : SingleTestScenario
{
    public override string Id => "OPT-BIMODAL-FIXED";
    public override string Category => "optimization";
    public override string Summary => "Instability removed on every target at commit 80: a 50/50 mixture of 80 ms and 160 ms (inventory sync sometimes retries because it races the warehouse feed) becomes a stable 80 ms.";
    public override TestRef Test { get; } = TestRef.Unit("Inventory", "InventorySynchronizerTests", "Syncs_stock_levels");
    private const int Fix = 80;
    private const string Key = "InventorySynchronizer.Sync";

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Fix, Kind = Kinds.Modality, Direction = Directions.Improvement,
            Subject = "Wait for the warehouse feed before syncing stock levels",
        },
    ];

    protected override Dist Model(Target target, int commit) =>
        commit >= Fix ? Dist.Ms(80) : new Dist.Mixture(0.5, 80, 160, Key);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Inventory", "InventorySynchronizer"), Tpl.Code($$"""
            using Storefront.Infrastructure;

            namespace Storefront.Inventory;

            public sealed class InventorySynchronizer
            {
                private const double SyncCostMs = 80;
                private const double StaleFeedProbability = 0.5; //?!fixed

                public IReadOnlyDictionary<string, int> Sync(IReadOnlyDictionary<string, int> warehouseLevels)
                {
                    ArgumentNullException.ThrowIfNull(warehouseLevels);
                    SimulatedNetwork.Call("warehouse-feed/await-latest", 0); //?fixed
                    SimulatedDatabase.Execute("merge into stock_levels using @levels", SyncCostMs);
                    if (Workload.Random("{{Key}}").NextDouble() >= StaleFeedProbability) //?!fixed
                    { //?!fixed
                        // The warehouse feed was still being written; sync again with the complete file. //?!fixed
                        SimulatedDatabase.Execute("merge into stock_levels using @levels", SyncCostMs); //?!fixed
                    } //?!fixed
                    return warehouseLevels.ToDictionary(kv => kv.Key, kv => Math.Max(kv.Value, 0), StringComparer.Ordinal);
                }
            }
            """, ("fixed", commit >= Fix)));
        yield return (Paths.For(Test), """
            using Storefront.Inventory;

            namespace Storefront.UnitTests.Inventory;

            public sealed class InventorySynchronizerTests
            {
                [Fact]
                public void Syncs_stock_levels()
                {
                    var levels = new InventorySynchronizer().Sync(new Dictionary<string, int> { ["HOME-000001-01"] = 4, ["TOYS-000002-01"] = -1 });

                    Assert.Equal(0, levels["TOYS-000002-01"]);
                }
            }
            """);
    }
}

/// <summary>Variance increase without a change of the mean.</summary>
public sealed class RegVariance : SingleTestScenario
{
    public override string Id => "REG-VARIANCE";
    public override string Category => "regression";
    public override string Summary => "Stability regression on every target at commit 34: a steady 150 ms becomes uniform 90..210 ms with the same mean (catalog pricing moves to a shared worker pool with variable contention).";
    public override TestRef Test { get; } = TestRef.Unit("Pricing", "PricingEngineTests", "Prices_catalog");
    private const int Culprit = 34;
    private const string Key = "PricingEngine.PriceCatalog";

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Variance, Direction = Directions.Regression, Tier = Tiers.Should,
            Subject = "Price catalog partitions on the shared worker pool",
            Body = "Pricing no longer owns dedicated workers; partitions are queued on the\npool shared with search indexing.",
        },
    ];

    protected override Dist Model(Target target, int commit) =>
        commit >= Culprit ? new Dist.Uniform(90, 210, Key) : Dist.Ms(150);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Pricing", "PricingEngine"), Tpl.Code($$"""
            using Storefront.Infrastructure;

            namespace Storefront.Pricing;

            public sealed record PriceRequest(string Sku, decimal ListPrice, decimal DiscountPercent);

            public sealed class PricingEngine
            {
                private const double PricingCostMs = 150; //?!pool
                private const double BestCaseCostMs = 90; //?pool
                private const double ContentionSpreadMs = 120; //?pool

                public IReadOnlyDictionary<string, decimal> PriceCatalog(IReadOnlyList<PriceRequest> requests)
                {
                    ArgumentNullException.ThrowIfNull(requests);
                    Compute.Run("price-catalog", PricingCostMs); //?!pool
                    var contention = Workload.Random("{{Key}}").NextDouble(); //?pool
                    Compute.Run("price-catalog-on-shared-pool", BestCaseCostMs + contention * ContentionSpreadMs); //?pool
                    return requests.ToDictionary(r => r.Sku, r => Math.Round(r.ListPrice * (1 - r.DiscountPercent / 100m), 2), StringComparer.Ordinal);
                }
            }
            """, ("pool", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Pricing;

            namespace Storefront.UnitTests.Pricing;

            public sealed class PricingEngineTests
            {
                [Fact]
                public void Prices_catalog()
                {
                    var prices = new PricingEngine().PriceCatalog([new PriceRequest("HOME-000001-01", 100m, 15m), new PriceRequest("TOYS-000002-01", 9.99m, 0m)]);

                    Assert.Equal(85m, prices["HOME-000001-01"]);
                    Assert.Equal(9.99m, prices["TOYS-000002-01"]);
                }
            }
            """);
    }
}

/// <summary>A tail appears: the median is unchanged, but 15% of runs become 8× slower.</summary>
public sealed class RegTailIntermittent : SingleTestScenario
{
    public override string Id => "REG-TAIL-INTERMITTENT";
    public override string Category => "regression";
    public override string Summary => "Intermittent tail appears on every target at commit 32: 50 ms, but from now on 15% of runs take 400 ms (stale pooled connections are re-established). The median is unchanged; p90/p95 grow 8x. Contrast with CTL-HEAVY-TAIL, which always had a tail.";
    public override TestRef Test { get; } = TestRef.Unit("Data", "ConnectionPoolTests", "Leases_connection");
    private const int Culprit = 32;
    private const string Key = "ConnectionPool.Lease";

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Culprit, Kind = Kinds.Tail, Direction = Directions.Regression,
            Subject = "Validate pooled connections before leasing them",
        },
    ];

    protected override Dist Model(Target target, int commit) =>
        commit >= Culprit ? new Dist.Tail(50, 0.15, 400, Key) : Dist.Ms(50);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Data", "ConnectionPool"), Tpl.Code($$"""
            using Storefront.Infrastructure;

            namespace Storefront.Data;

            public sealed record PooledConnection(int Id, string Database);

            public sealed class ConnectionPool
            {
                private const double LeaseCostMs = 50;
                private const double ReconnectCostMs = 350; //?validate
                private const double StaleConnectionProbability = 0.15; //?validate

                private int _nextId;

                public PooledConnection Lease(string database)
                {
                    ArgumentException.ThrowIfNullOrEmpty(database);
                    SimulatedDatabase.Query("select 1", LeaseCostMs);
                    if (Workload.Random("{{Key}}").NextDouble() < StaleConnectionProbability) //?validate
                    { //?validate
                        // The validation query failed: the server closed the idle connection. //?validate
                        SimulatedNetwork.Call($"db/{database}/connect", ReconnectCostMs); //?validate
                    } //?validate
                    return new PooledConnection(Interlocked.Increment(ref _nextId), database);
                }
            }
            """, ("validate", commit >= Culprit)));
        yield return (Paths.For(Test), """
            using Storefront.Data;

            namespace Storefront.UnitTests.Data;

            public sealed class ConnectionPoolTests
            {
                [Fact]
                public void Leases_connection()
                {
                    var connection = new ConnectionPool().Lease("orders");

                    Assert.Equal(1, connection.Id);
                    Assert.Equal("orders", connection.Database);
                }
            }
            """);
    }
}

/// <summary>Sudden large improvement.</summary>
public sealed class OptStep : SingleTestScenario
{
    public override string Id => "OPT-STEP";
    public override string Category => "optimization";
    public override string Summary => "Sudden improvement on every target: 400 → 84 ms at commit 66 (catalog snapshots are rebuilt incrementally: 2 of 10 partitions are rebuilt, the rest is copied).";
    public override TestRef Test { get; } = TestRef.Unit("Catalog", "CatalogSnapshotBuilderTests", "Builds_catalog_snapshot");
    private const int Optimization = 66;

    public override IReadOnlyList<Change> Changes { get; } =
    [
        new()
        {
            Commit = Optimization, Kind = Kinds.Step, Subject = "Rebuild only changed partitions of the catalog snapshot",
            Body = "Partitions whose source rows did not change since the previous snapshot are\ncopied instead of being rebuilt.",
        },
    ];

    protected override Dist Model(Target target, int commit) => Dist.Ms(commit >= Optimization ? 2 * 40 + 8 * 0.5 : 400);

    protected override IEnumerable<(string Path, string Content)> Files(int commit)
    {
        yield return (Paths.Src("Catalog", "CatalogSnapshotBuilder"), Tpl.Code("""
            using Storefront.Infrastructure;

            namespace Storefront.Catalog;

            public sealed record SnapshotPartition(string Name, bool Changed);

            public sealed class CatalogSnapshotBuilder
            {
                private const double PartitionRebuildCostMs = 40;
                private const double PartitionCopyCostMs = 0.5; //?incremental

                public int Build(IReadOnlyList<SnapshotPartition> partitions)
                {
                    ArgumentNullException.ThrowIfNull(partitions);
                    var rebuilt = 0;
                    foreach (var partition in partitions)
                    {
                        if (!partition.Changed) //?incremental
                        { //?incremental
                            SimulatedStorage.Read($"snapshots/previous/{partition.Name}", PartitionCopyCostMs); //?incremental
                            continue; //?incremental
                        } //?incremental
                        SimulatedDatabase.Query($"select * from catalog where partition = '{partition.Name}'", PartitionRebuildCostMs);
                        rebuilt++;
                    }
                    return rebuilt;
                }
            }
            """, ("incremental", commit >= Optimization)));
        yield return (Paths.For(Test), """
            using Storefront.Catalog;

            namespace Storefront.UnitTests.Catalog;

            public sealed class CatalogSnapshotBuilderTests
            {
                [Fact]
                public void Builds_catalog_snapshot()
                {
                    var partitions = Enumerable.Range(0, 10).Select(i => new SnapshotPartition($"p{i}", Changed: i is 0 or 7)).ToList();

                    var rebuilt = new CatalogSnapshotBuilder().Build(partitions);

                    Assert.InRange(rebuilt, 2, 10);
                }
            }
            """);
    }
}
