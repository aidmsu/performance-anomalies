using Storefront.Infrastructure;

namespace Storefront.Accounting;

public sealed record LedgerEntry(string Reference, decimal Amount);

public sealed record ReconciliationReport(int Matched, IReadOnlyList<string> Unmatched);

/// <summary>Reconciles the monthly ledger with the bank statement.</summary>
public sealed class LedgerReconciler
{
    private const double StatementDownloadCostMs = 600;
    private const double MatchingCostMs = 400;
    private const double SettlementMatchingCostMs = 80;

    public ReconciliationReport Reconcile(IReadOnlyList<LedgerEntry> ledger, IReadOnlyList<LedgerEntry> statement)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(statement);
        SimulatedNetwork.Call("bank/statements/monthly", StatementDownloadCostMs);
        Compute.Run("match-ledger-entries", MatchingCostMs);
        SimulatedDatabase.Query("select * from card_settlements where month = @month", SettlementMatchingCostMs);
        var references = statement.Select(s => (s.Reference, s.Amount)).ToHashSet();
        var unmatched = ledger.Where(e => !references.Contains((e.Reference, e.Amount))).Select(e => e.Reference).ToList();
        return new ReconciliationReport(ledger.Count - unmatched.Count, unmatched);
    }
}
