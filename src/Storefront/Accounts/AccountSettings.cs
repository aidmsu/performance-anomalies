using Storefront.Infrastructure;

namespace Storefront.Accounts;

/// <summary>Manages account settings.</summary>
public sealed class AccountSettings
{
    public string ChangeDisplayName(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("accountsettings.change-display-name", 5.13);
        return $"accountsettings.change-display-name:{input}";
    }

    public string EnableTwoFactorLogin(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("accountsettings.enable-two-factor-login", 15.57);
        return $"accountsettings.enable-two-factor-login:{input}";
    }

    public string ListActiveSessions(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("accountsettings.list-active-sessions", PlatformCost.Select(linux: 18.32, windows: 25.65, macOS: 14.66));
        return $"accountsettings.list-active-sessions:{input}";
    }

    public string RotateApiKey(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("accountsettings.rotate-api-key", 3.12);
        return $"accountsettings.rotate-api-key:{input}";
    }

    public string LoadPrivacySettings(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("accountsettings.load-privacy-settings", 28.79);
        return $"accountsettings.load-privacy-settings:{input}";
    }
}
