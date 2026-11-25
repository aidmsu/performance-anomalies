using Storefront.Analytics;

namespace Storefront.UnitTests.Analytics;

public sealed class EventTrackerTests
{
    [Fact]
    public void Tracks_page_view()
    {
        var result = new EventTracker().TrackPageView("sample-42");

        Assert.Equal("eventtracker.track-page-view:sample-42", result);
    }

    [Fact]
    public void Tracks_add_to_cart()
    {
        var result = new EventTracker().TrackAddToCart("sample-42");

        Assert.Equal("eventtracker.track-add-to-cart:sample-42", result);
    }

    [Fact]
    public void Aggregates_daily_events()
    {
        var result = new EventTracker().AggregateDailyEvents("sample-42");

        Assert.Equal("eventtracker.aggregate-daily-events:sample-42", result);
    }

    [Fact]
    public void Lists_top_search_terms()
    {
        var result = new EventTracker().ListTopSearchTerms("sample-42");

        Assert.Equal("eventtracker.list-top-search-terms:sample-42", result);
    }

    [Fact]
    public void Purges_expired_events()
    {
        var result = new EventTracker().PurgeExpiredEvents("sample-42");

        Assert.Equal("eventtracker.purge-expired-events:sample-42", result);
    }
}
