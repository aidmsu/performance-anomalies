using Storefront.Suppliers;

namespace Storefront.UnitTests.Suppliers;

public sealed class SupplierDirectoryTests
{
    [Fact]
    public void Finds_supplier_by_code()
    {
        var result = new SupplierDirectory().FindSupplierByCode("sample-42");

        Assert.Equal("supplierdirectory.find-supplier-by-code:sample-42", result);
    }

    [Fact]
    public void Lists_supplier_contacts()
    {
        var result = new SupplierDirectory().ListSupplierContacts("sample-42");

        Assert.Equal("supplierdirectory.list-supplier-contacts:sample-42", result);
    }

    [Fact]
    public void Loads_payment_terms()
    {
        var result = new SupplierDirectory().LoadPaymentTerms("sample-42");

        Assert.Equal("supplierdirectory.load-payment-terms:sample-42", result);
    }

    [Fact]
    public void Checks_supplier_certification()
    {
        var result = new SupplierDirectory().CheckSupplierCertification("sample-42");

        Assert.Equal("supplierdirectory.check-supplier-certification:sample-42", result);
    }

    [Fact]
    public void Ranks_suppliers_by_lead_time()
    {
        var result = new SupplierDirectory().RankSuppliersByLeadTime("sample-42");

        Assert.Equal("supplierdirectory.rank-suppliers-by-lead-time:sample-42", result);
    }
}
