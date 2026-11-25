using Storefront.Accounts;

namespace Storefront.UnitTests.Accounts;

public sealed class AccountSettingsTests
{
    [Fact]
    public void Changes_display_name()
    {
        var result = new AccountSettings().ChangeDisplayName("sample-42");

        Assert.Equal("accountsettings.change-display-name:sample-42", result);
    }

    [Fact]
    public void Enables_two_factor_login()
    {
        var result = new AccountSettings().EnableTwoFactorLogin("sample-42");

        Assert.Equal("accountsettings.enable-two-factor-login:sample-42", result);
    }

    [Fact]
    public void Lists_active_sessions()
    {
        var result = new AccountSettings().ListActiveSessions("sample-42");

        Assert.Equal("accountsettings.list-active-sessions:sample-42", result);
    }

    [Fact]
    public void Rotates_api_key()
    {
        var result = new AccountSettings().RotateApiKey("sample-42");

        Assert.Equal("accountsettings.rotate-api-key:sample-42", result);
    }

    [Fact]
    public void Loads_privacy_settings()
    {
        var result = new AccountSettings().LoadPrivacySettings("sample-42");

        Assert.Equal("accountsettings.load-privacy-settings:sample-42", result);
    }
}
