using Storefront.Infrastructure;

namespace Storefront.Analytics;

/// <summary>Records storefront analytics events.</summary>
public sealed class EventTracker
{
    public string TrackPageView(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("eventtracker.track-page-view", 10.03);
        return $"eventtracker.track-page-view:{input}";
    }

    public string TrackAddToCart(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("eventtracker.track-add-to-cart", 10.18);
        return $"eventtracker.track-add-to-cart:{input}";
    }

    public string AggregateDailyEvents(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("eventtracker.aggregate-daily-events", PlatformCost.Select(linux: 3.56, windows: 4.98, macOS: 2.85));
        return $"eventtracker.aggregate-daily-events:{input}";
    }

    public string ListTopSearchTerms(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("eventtracker.list-top-search-terms", 5.38);
        return $"eventtracker.list-top-search-terms:{input}";
    }

    public string PurgeExpiredEvents(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("eventtracker.purge-expired-events", 2.32);
        return $"eventtracker.purge-expired-events:{input}";
    }
}
