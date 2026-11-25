using Storefront.Infrastructure;

namespace Storefront.Suppliers;

/// <summary>Keeps supplier contacts and terms.</summary>
public sealed class SupplierDirectory
{
    public string FindSupplierByCode(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("supplierdirectory.find-supplier-by-code", 8.37);
        return $"supplierdirectory.find-supplier-by-code:{input}";
    }

    public string ListSupplierContacts(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("supplierdirectory.list-supplier-contacts", 9.58);
        return $"supplierdirectory.list-supplier-contacts:{input}";
    }

    public string LoadPaymentTerms(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("supplierdirectory.load-payment-terms", PlatformCost.Select(linux: 1.14, windows: 1.6, macOS: 0.91));
        return $"supplierdirectory.load-payment-terms:{input}";
    }

    public string CheckSupplierCertification(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("supplierdirectory.check-supplier-certification", 16.81);
        return $"supplierdirectory.check-supplier-certification:{input}";
    }

    public string RankSuppliersByLeadTime(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("supplierdirectory.rank-suppliers-by-lead-time", 2.89);
        return $"supplierdirectory.rank-suppliers-by-lead-time:{input}";
    }
}
