using Storefront.Preferences;

namespace Storefront.UnitTests.Preferences;

public sealed class PreferenceCenterTests
{
    [Fact]
    public void Loads_notification_preferences()
    {
        var result = new PreferenceCenter().LoadNotificationPreferences("sample-42");

        Assert.Equal("preferencecenter.load-notification-preferences:sample-42", result);
    }

    [Fact]
    public void Opts_out_of_newsletter()
    {
        var result = new PreferenceCenter().OptOutOfNewsletter("sample-42");

        Assert.Equal("preferencecenter.opt-out-of-newsletter:sample-42", result);
    }

    [Fact]
    public void Lists_subscription_topics()
    {
        var result = new PreferenceCenter().ListSubscriptionTopics("sample-42");

        Assert.Equal("preferencecenter.list-subscription-topics:sample-42", result);
    }

    [Fact]
    public void Applies_quiet_hours()
    {
        var result = new PreferenceCenter().ApplyQuietHours("sample-42");

        Assert.Equal("preferencecenter.apply-quiet-hours:sample-42", result);
    }

    [Fact]
    public void Exports_preference_history()
    {
        var result = new PreferenceCenter().ExportPreferenceHistory("sample-42");

        Assert.Equal("preferencecenter.export-preference-history:sample-42", result);
    }
}
