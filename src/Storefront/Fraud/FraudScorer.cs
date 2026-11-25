using Storefront.Infrastructure;

namespace Storefront.Fraud;

public sealed record FraudCheck(string OrderId, string Email, string DeviceId, decimal Amount);

/// <summary>Scores orders with the external risk engine, whose response time varies a lot.</summary>
public sealed class FraudScorer
{
    private const double RiskEngineFastestMs = 90;
    private const double RiskEngineSpreadMs = 60;

    public int Score(FraudCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        var responseTime = RiskEngineFastestMs + Workload.Random("FraudScorer.Score").NextDouble() * RiskEngineSpreadMs;
        SimulatedNetwork.Call("risk-engine/score", responseTime);
        var score = check.Amount > 1000 ? 40 : 10;
        return Math.Min(score, 100);
    }
}
