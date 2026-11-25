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
