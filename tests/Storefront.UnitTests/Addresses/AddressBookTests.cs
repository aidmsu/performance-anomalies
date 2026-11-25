using Storefront.Addresses;

namespace Storefront.UnitTests.Addresses;

public sealed class AddressBookTests
{
    [Fact]
    public void Validates_postal_code()
    {
        var result = new AddressBook().ValidatePostalCode("sample-42");

        Assert.Equal("addressbook.validate-postal-code:sample-42", result);
    }

    [Fact]
    public void Lists_saved_addresses()
    {
        var result = new AddressBook().ListSavedAddresses("sample-42");

        Assert.Equal("addressbook.list-saved-addresses:sample-42", result);
    }

    [Fact]
    public void Formats_address_label()
    {
        var result = new AddressBook().FormatAddressLabel("sample-42");

        Assert.Equal("addressbook.format-address-label:sample-42", result);
    }

    [Fact]
    public void Geocodes_address()
    {
        var result = new AddressBook().GeocodeAddress("sample-42");

        Assert.Equal("addressbook.geocode-address:sample-42", result);
    }

    [Fact]
    public void Merges_duplicate_addresses()
    {
        var result = new AddressBook().MergeDuplicateAddresses("sample-42");

        Assert.Equal("addressbook.merge-duplicate-addresses:sample-42", result);
    }
}
