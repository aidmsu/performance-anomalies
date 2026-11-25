using Storefront.Domain;

namespace Storefront.UnitTests.Domain;

public sealed class SkuTests
{
    [Fact]
    public void Parses_valid_sku()
    {
        var sku = Sku.Parse("ELEC-001234-02");

        Assert.Equal(new Sku("ELEC", 1234, 2), sku);
    }

    [Fact]
    public void Normalizes_category_to_upper_case()
    {
        Assert.Equal("HOME", Sku.Parse("home-000001-01").Category);
    }

    [Fact]
    public void Round_trips_to_string()
    {
        Assert.Equal("TOYS-000077-10", Sku.Parse("TOYS-000077-10").ToString());
    }

    [Fact]
    public void Rejects_sku_without_variant()
    {
        Assert.False(Sku.TryParse("ELEC-001234", out _));
    }

    [Fact]
    public void Rejects_empty_input()
    {
        Assert.False(Sku.TryParse("  ", out _));
        Assert.Throws<FormatException>(() => Sku.Parse(""));
    }
}
