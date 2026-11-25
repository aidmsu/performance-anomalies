namespace Storefront.Admin;

public sealed record MaintenanceWindow(DateTimeOffset Start, TimeSpan Duration, string Reason)
{
    public DateTimeOffset End => Start + Duration;

    public bool Contains(DateTimeOffset moment) => moment >= Start && moment < End;
}
