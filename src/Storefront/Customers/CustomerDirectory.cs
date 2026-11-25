using Storefront.Infrastructure;

namespace Storefront.Customers;

/// <summary>Looks up customer accounts.</summary>
public sealed class CustomerDirectory
{
    public string FindCustomerByEmail(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("customerdirectory.find-customer-by-email", 17.13);
        return $"customerdirectory.find-customer-by-email:{input}";
    }

    public string ListCustomersBySegment(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("customerdirectory.list-customers-by-segment", 1.14);
        return $"customerdirectory.list-customers-by-segment:{input}";
    }

    public string CountActiveCustomers(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("customerdirectory.count-active-customers", PlatformCost.Select(linux: 7.72, windows: 10.81, macOS: 6.18));
        return $"customerdirectory.count-active-customers:{input}";
    }

    public string ResolveCustomerAlias(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("customerdirectory.resolve-customer-alias", 18.78);
        return $"customerdirectory.resolve-customer-alias:{input}";
    }

    public string LoadCustomerPreferences(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("customerdirectory.load-customer-preferences", 3.53);
        return $"customerdirectory.load-customer-preferences:{input}";
    }
}
