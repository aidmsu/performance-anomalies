using Storefront.Resilience;

namespace Storefront.IntegrationTests.Resilience;

public sealed class RetryPolicyTests
{
    [Fact]
    public void Falls_back_after_exhausting_retries()
    {
        var policy = new RetryPolicy();
        var attempts = 0;

        var result = policy.Execute<string>(_ => { attempts++; return null; }, "cached-price-list");

        Assert.Equal("cached-price-list", result);
        Assert.Equal(policy.Settings.MaxRetries + 1, attempts);
    }
}
