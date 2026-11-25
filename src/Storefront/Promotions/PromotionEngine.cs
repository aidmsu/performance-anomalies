using Storefront.Infrastructure;

namespace Storefront.Promotions;

public sealed record Promotion(string Id, string Rule, decimal Discount);

/// <summary>Evaluates the active promotions for a cart.</summary>
public sealed class PromotionEngine
{
    private const double EvaluationCostMs = 50;

    private static readonly Func<decimal, bool>[] PrecompiledRules = [total => total >= 50m, total => total >= 100m];

    public decimal Evaluate(IReadOnlyList<Promotion> promotions, decimal cartTotal)
    {
        ArgumentNullException.ThrowIfNull(promotions);
        IReadOnlyList<Func<decimal, bool>> rules = PrecompiledRules;
        Compute.Run("evaluate-promotions", EvaluationCostMs);
        return promotions.Where((p, i) => rules[i % rules.Count](cartTotal)).Sum(p => p.Discount);
    }
}
