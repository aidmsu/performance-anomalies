using Storefront.Domain;

namespace Storefront.UnitTests.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Adds_amounts_in_the_same_currency()
    {
        var total = new Money(10.25m, "EUR").Add(new Money(4.75m, "EUR"));

        Assert.Equal(new Money(15.00m, "EUR"), total);
    }

    [Fact]
    public void Rejects_mixed_currencies()
    {
        var euros = new Money(1m, "EUR");

        Assert.Throws<InvalidOperationException>(() => euros.Add(new Money(1m, "USD")));
    }

    [Fact]
    public void Rounds_half_to_even_when_multiplying()
    {
        Assert.Equal(0.12m, new Money(0.125m, "EUR").Multiply(1m).Amount);
        Assert.Equal(0.14m, new Money(0.135m, "EUR").Multiply(1m).Amount);
    }

    [Fact]
    public void Formats_with_invariant_culture()
    {
        Assert.Equal("1234.50 USD", new Money(1234.5m, "USD").ToString());
    }

    [Fact]
    public void Zero_is_the_identity_for_addition()
    {
        var amount = new Money(42m, "GBP");

        Assert.Equal(amount, amount.Add(Money.Zero("GBP")));
    }
}
