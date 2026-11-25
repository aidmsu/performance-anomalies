using Storefront.Fraud;

namespace Storefront.UnitTests.Fraud;

public sealed class FraudScorerTests
{
    [Fact]
    public void Scores_order()
    {
        var score = new FraudScorer().Score(new FraudCheck("SO-1", "ada@example.com", "ios-17", 1200m));

        Assert.InRange(score, 40, 100);
    }
}
