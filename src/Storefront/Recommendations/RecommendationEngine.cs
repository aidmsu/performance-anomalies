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
        if (Workload.Random("RecommendationEngine.TopFor").NextDouble() < ColdModelProbability)
        {
            // The model was evicted from the shared cache and has to be loaded again.
            SimulatedStorage.Read("models/recommendations.onnx", ColdModelLoadCostMs);
        }
        Compute.Run("score-products", ScoringCostMs);
        return Enumerable.Range(1, count).Select(i => $"SKU-{customerId.Length * 100 + i:000000}").ToList();
    }
}
