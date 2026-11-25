using Storefront.Search;

namespace Storefront.UnitTests.Search;

public sealed class SearchIndexBuilderTests
{
    [Fact]
    public void Builds_index_for_catalog()
    {
        var index = new SearchIndexBuilder().Build(
        [
            new SearchDocument("HOME-000001-01", "Oak desk lamp", "Lumen"),
            new SearchDocument("HOME-000002-01", "Brass desk", "Forge"),
        ]);

        Assert.Equal(2, index["desk"].Count);
    }
}
