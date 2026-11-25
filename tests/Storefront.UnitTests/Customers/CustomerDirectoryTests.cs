using Storefront.Customers;

namespace Storefront.UnitTests.Customers;

public sealed class CustomerDirectoryTests
{
    [Fact]
    public void Finds_customer_by_email()
    {
        var result = new CustomerDirectory().FindCustomerByEmail("sample-42");

        Assert.Equal("customerdirectory.find-customer-by-email:sample-42", result);
    }

    [Fact]
    public void Lists_customers_by_segment()
    {
        var result = new CustomerDirectory().ListCustomersBySegment("sample-42");

        Assert.Equal("customerdirectory.list-customers-by-segment:sample-42", result);
    }

    [Fact]
    public void Counts_active_customers()
    {
        var result = new CustomerDirectory().CountActiveCustomers("sample-42");

        Assert.Equal("customerdirectory.count-active-customers:sample-42", result);
    }

    [Fact]
    public void Resolves_customer_alias()
    {
        var result = new CustomerDirectory().ResolveCustomerAlias("sample-42");

        Assert.Equal("customerdirectory.resolve-customer-alias:sample-42", result);
    }

    [Fact]
    public void Loads_customer_preferences()
    {
        var result = new CustomerDirectory().LoadCustomerPreferences("sample-42");

        Assert.Equal("customerdirectory.load-customer-preferences:sample-42", result);
    }
}
