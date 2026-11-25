using Storefront.Infrastructure;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Storefront.IntegrationTests.WarmedUpTestFramework", "Storefront.IntegrationTests")]

namespace Storefront.IntegrationTests;

/// <summary>
/// Default xUnit framework that first warms up shared infrastructure (JIT, timers, culture data, serializers),
/// so that one-off start-up costs are not attributed to whichever test happens to run first.
/// </summary>
public sealed class WarmedUpTestFramework : XunitTestFramework
{
    public WarmedUpTestFramework(IMessageSink messageSink)
        : base(messageSink)
    {
        Warmup.Run(typeof(WarmedUpTestFramework).Assembly);
        WarmUpAssertions();
    }

    private static void WarmUpAssertions()
    {
        Assert.True(true);
        Assert.False(false);
        Assert.Equal(1, 1);
        Assert.Equal(1m, 1m);
        Assert.Equal(1UL, 1UL);
        Assert.Equal("a", "a");
        Assert.Equal(["a"], new List<string> { "a" });
        Assert.NotEqual(0UL, 1UL);
        Assert.Contains("b", "abc", StringComparison.Ordinal);
        Assert.StartsWith("a", "abc", StringComparison.Ordinal);
        Assert.EndsWith("c", "abc", StringComparison.Ordinal);
        Assert.InRange(1, 0, 2);
        Assert.InRange(1m, 0m, 2m);
        Assert.Single(new[] { 1 });
        Assert.Empty(Array.Empty<int>());
        Assert.All(new[] { 1 }, value => Assert.Equal(1, value));
        Assert.Throws<InvalidOperationException>(Fail);

        static void Fail() => throw new InvalidOperationException();
    }
}
