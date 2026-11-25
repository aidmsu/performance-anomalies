using System.Text.Json;
using Storefront.Infrastructure;

namespace Storefront.Resilience;

public sealed record RetrySettings(int MaxRetries, double BackoffMilliseconds, double AttemptTimeoutMilliseconds);

/// <summary>Retries calls to partner APIs with the settings from the embedded appsettings.json.</summary>
public sealed class RetryPolicy
{
    public RetrySettings Settings { get; } = LoadSettings();

    public T Execute<T>(Func<int, T?> attempt, T fallback) where T : class
    {
        ArgumentNullException.ThrowIfNull(attempt);
        for (var i = 0; i <= Settings.MaxRetries; i++)
        {
            if (i > 0) Workload.Spend(Settings.BackoffMilliseconds);
            SimulatedNetwork.Call("partners/attempt", Settings.AttemptTimeoutMilliseconds);
            if (attempt(i) is { } result) return result;
        }
        return fallback;
    }

    private static RetrySettings LoadSettings()
    {
        using var stream = typeof(RetryPolicy).Assembly.GetManifestResourceStream("Storefront.Resources.appsettings.json")
            ?? throw new InvalidOperationException("appsettings.json is not embedded.");
        using var document = JsonDocument.Parse(stream);
        var retry = document.RootElement.GetProperty("Retry");
        return new RetrySettings(
            retry.GetProperty("MaxRetries").GetInt32(),
            retry.GetProperty("BackoffMilliseconds").GetDouble(),
            retry.GetProperty("AttemptTimeoutMilliseconds").GetDouble());
    }
}
