using Storefront.Infrastructure;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Storefront.UnitTests.WarmedUpTestFramework", "Storefront.UnitTests")]

namespace Storefront.UnitTests;

/// <summary>
/// Default xUnit framework that first warms up shared infrastructure (JIT, timers, culture data, serializers),
/// so that one-off start-up costs are not attributed to whichever test happens to run first.
/// </summary>
public sealed class WarmedUpTestFramework : XunitTestFramework
{
    public WarmedUpTestFramework(IMessageSink messageSink)
        : base(messageSink)
    {
        Warmup.Run();
    }
}
