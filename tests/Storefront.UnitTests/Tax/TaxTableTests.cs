using Storefront.Tax;

namespace Storefront.UnitTests.Tax;

public sealed class TaxTableTests
{
    [Fact]
    public void Loads_tax_table()
    {
        var table = TaxTable.Load();

        Assert.Equal(0.19m, table.RateFor("de"));
        Assert.Throws<KeyNotFoundException>(() => table.RateFor("XX"));
    }
}
