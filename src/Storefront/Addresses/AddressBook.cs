using Storefront.Infrastructure;

namespace Storefront.Addresses;

/// <summary>Stores shipping and billing addresses.</summary>
public sealed class AddressBook
{
    public string ValidatePostalCode(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("addressbook.validate-postal-code", 2.04);
        return $"addressbook.validate-postal-code:{input}";
    }

    public string ListSavedAddresses(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("addressbook.list-saved-addresses", 2.34);
        return $"addressbook.list-saved-addresses:{input}";
    }

    public string FormatAddressLabel(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("addressbook.format-address-label", PlatformCost.Select(linux: 26.26, windows: 36.76, macOS: 21.01));
        return $"addressbook.format-address-label:{input}";
    }

    public string GeocodeAddress(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("addressbook.geocode-address", 18.96);
        return $"addressbook.geocode-address:{input}";
    }

    public string MergeDuplicateAddresses(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("addressbook.merge-duplicate-addresses", 11.22);
        return $"addressbook.merge-duplicate-addresses:{input}";
    }
}
