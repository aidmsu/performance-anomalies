using HistoryGenerator.Model;

namespace HistoryGenerator.Scenarios;

/// <summary>Commits that change nothing a test measures: docs and admin code that no test touches.</summary>
public static class Fillers
{
    public static readonly IReadOnlyList<Filler> All =
    [
        new("Document the order lifecycle", "Describe the states an order goes through from checkout to delivery.",
            s => s.AddFiller("docs/order-lifecycle.md", """
                # Order lifecycle

                1. **Placed** - checkout validation passed and payment is authorised.
                2. **Reserved** - stock is reserved in one or more warehouses.
                3. **Shipped** - every parcel has a tracking number.
                4. **Delivered** - the carrier confirmed delivery.

                Orders can be cancelled until they are shipped. Returns start from the delivered state.
                """)),
        new("Add fulfilment terms to the glossary", "",
            s => s.Edit("docs/glossary.md", t => t + "- **Fulfilment** - picking, packing and handing parcels to the carrier.\n- **Cut-off** - the latest order time for same-day dispatch.\n")),
        new("Describe maintenance windows in the operations guide", "",
            s => s.Edit("docs/operations.md", t => t + "\n## Maintenance windows\n\nMaintenance windows are announced one week ahead. During a window the storefront\nkeeps accepting orders but defers payment capture.\n")),
        new("Add XML documentation to MaintenanceWindow", "",
            s => s.Edit("src/Storefront/Admin/MaintenanceWindow.cs", t => t.Replace(
                "public sealed record MaintenanceWindow(",
                "/// <summary>A planned period in which background jobs are paused.</summary>\npublic sealed record MaintenanceWindow("))),
        new("Record the decision to simulate infrastructure", "",
            s => s.AddFiller("docs/adr/0001-simulated-infrastructure.md", """
                # 1. Simulated infrastructure

                ## Status

                Accepted

                ## Context

                The test suite must run on every platform without a database, message broker or network access.

                ## Decision

                Components call cost primitives (`SimulatedDatabase`, `SimulatedStorage`, `SimulatedNetwork`, `Compute`)
                that stand in for external systems.

                ## Consequences

                Tests are hermetic and fast to set up. Timing of external calls is part of the code under test.
                """)),
        new("Explain currency rounding rules", "Document the rounding behaviour customers see on invoices.",
            s => s.AddFiller("docs/pricing.md", """
                # Pricing

                - Amounts are rounded half to even to the minor unit of the currency.
                - Currencies use the minor units defined by ISO 4217.
                - Discounts are applied before tax.
                """)),
        new("Add a stub for exporting the admin audit log", "",
            s => s.AddFiller("src/Storefront/Admin/AuditLogExport.cs", """
                namespace Storefront.Admin;

                /// <summary>Placeholder for the upcoming admin audit log export.</summary>
                public static class AuditLogExport
                {
                    public static string FileName(DateOnly day) => $"audit-{day:yyyy-MM-dd}.csv";
                }
                """)),
        new("Fix typos in the operations guide", "",
            s => s.Edit("docs/operations.md", t => t.Replace("Each job uploads\nits test results", "Each job uploads\nits test results (TRX, JUnit XML and a CSV of durations)"))),
        new("Document the supported shipping carriers", "",
            s => s.AddFiller("docs/shipping.md", """
                # Shipping

                | Carrier | Services | Label format |
                |---|---|---|
                | national-post | standard, tracked | ZPL |
                | express | next day, same day | ZPL |
                | freight | pallets | PDF |
                """)),
        new("Record the search indexing pipeline decision", "",
            s => s.AddFiller("docs/adr/0002-search-indexing.md", """
                # 2. Search indexing pipeline

                ## Status

                Accepted

                ## Decision

                The search index is rebuilt from the catalog snapshot. Analyzer stages run one after another over the
                whole catalog so that every stage sees the output of the previous one.
                """)),
        new("Tidy up admin feature flag names", "",
            s => s.Edit("src/Storefront/Admin/FeatureFlags.cs", t => t.Replace(
                "    public const string ExpressShipping = \"shipping.express\";",
                "    public const string ExpressShipping = \"shipping.express\";\n    public const string GiftMessages = \"orders.gift-messages\";"))),
        new("Document the release process", "",
            s => s.AddFiller("docs/releasing.md", """
                # Releasing

                Every commit on `master` is releasable. Releases are tagged from `master` after CI is green on all
                platforms. There are no release branches.
                """)),
        new("Add payment terms to the glossary", "",
            s => s.Edit("docs/glossary.md", t => t + "- **Authorisation** - the issuer reserved the amount on the card.\n- **Capture** - the reserved amount is actually charged.\n")),
        new("Add a runbook for inventory sync incidents", "",
            s => s.AddFiller("docs/runbooks/inventory-sync.md", """
                # Runbook: inventory sync

                1. Check that the warehouse feed for the last hour exists.
                2. Compare stock levels of five random SKUs with the warehouse system.
                3. If they differ, trigger a full sync from the admin console.
                """)),
        new("Describe the module layout in the architecture notes", "",
            s => s.Edit("docs/architecture.md", t => t + "\nEach business area is a folder and a namespace; areas talk to each other only through public types.\n")),
        new("Note the supported .NET version in the docs", "",
            s => s.Edit("docs/operations.md", t => t + "\n## Toolchain\n\nThe solution targets .NET 10. The exact SDK version is pinned in `global.json`.\n")),
    ];
}
