using Storefront.Storage;

namespace Storefront.UnitTests.Storage;

public sealed class PathResolverTests
{
    [Fact]
    public void Resolves_relative_paths()
    {
        Assert.Equal("/exports/b/c.csv", PathResolver.Resolve("/exports/", "a/../../b/./c.csv"));
    }
}
