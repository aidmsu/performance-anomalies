using Storefront.Returns;

namespace Storefront.UnitTests.Returns;

public sealed class ReturnsPolicyTests
{
    [Fact]
    public void Evaluates_return_request()
    {
        var decision = new ReturnsPolicy().Evaluate(new ReturnRequest("SO-1", 12, Opened: false, "10115"));

        Assert.True(decision.Accepted);
    }
}
