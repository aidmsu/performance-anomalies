namespace Storefront.Admin;

/// <summary>Placeholder for the upcoming admin audit log export.</summary>
public static class AuditLogExport
{
    public static string FileName(DateOnly day) => $"audit-{day:yyyy-MM-dd}.csv";
}
