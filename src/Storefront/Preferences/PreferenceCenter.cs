using Storefront.Infrastructure;

namespace Storefront.Preferences;

/// <summary>Manages notification preferences.</summary>
public sealed class PreferenceCenter
{
    public string LoadNotificationPreferences(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("preferencecenter.load-notification-preferences", 1.93);
        return $"preferencecenter.load-notification-preferences:{input}";
    }

    public string OptOutOfNewsletter(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("preferencecenter.opt-out-of-newsletter", 16.22);
        return $"preferencecenter.opt-out-of-newsletter:{input}";
    }

    public string ListSubscriptionTopics(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("preferencecenter.list-subscription-topics", PlatformCost.Select(linux: 1.72, windows: 2.41, macOS: 1.38));
        return $"preferencecenter.list-subscription-topics:{input}";
    }

    public string ApplyQuietHours(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("preferencecenter.apply-quiet-hours", 4.16);
        return $"preferencecenter.apply-quiet-hours:{input}";
    }

    public string ExportPreferenceHistory(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("preferencecenter.export-preference-history", 7.05);
        return $"preferencecenter.export-preference-history:{input}";
    }
}
