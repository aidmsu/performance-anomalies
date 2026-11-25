using Storefront.Tax;

namespace Storefront.UnitTests.Tax;

public sealed class TaxJurisdictionRegistryTests
{
    [Fact]
    public void Loads_jurisdiction_registry()
    {
        var registry = TaxJurisdictionRegistry.Load();

        Assert.True(registry.Jurisdictions.Count >= 30);
        Assert.Equal(registry.Jurisdictions.Count, registry.Jurisdictions.Select(j => j.PostalFrom).Distinct().Count());
    }
}
