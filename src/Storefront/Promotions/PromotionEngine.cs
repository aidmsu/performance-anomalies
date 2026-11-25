using Storefront.Infrastructure;

namespace Storefront.Promotions;

public sealed record Promotion(string Id, string Rule, decimal Discount);

/// <summary>Evaluates the active promotions for a cart.</summary>
public sealed class PromotionEngine
{
    private const double RuleCompilationCostMs = 100;
    private const double EvaluationCostMs = 50;

    public decimal Evaluate(IReadOnlyList<Promotion> promotions, decimal cartTotal)
    {
        ArgumentNullException.ThrowIfNull(promotions);
        var rules = CompileRules();
        Compute.Run("evaluate-promotions", EvaluationCostMs);
        return promotions.Where((p, i) => rules[i % rules.Count](cartTotal)).Sum(p => p.Discount);
    }

    private static IReadOnlyList<Func<decimal, bool>> CompileRules()
    {
        Compute.Run("compile-promotion-rules", RuleCompilationCostMs);
        return [total => total >= 50m, total => total >= 100m];
    }
}
