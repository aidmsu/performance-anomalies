using Storefront.Infrastructure;

namespace Storefront.Reviews;

/// <summary>Moderates product reviews.</summary>
public sealed class ReviewModeration
{
    public string QueueReviewForModeration(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("reviewmoderation.queue-review-for-moderation", 4.3);
        return $"reviewmoderation.queue-review-for-moderation:{input}";
    }

    public string ApproveReview(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("reviewmoderation.approve-review", 1.9);
        return $"reviewmoderation.approve-review:{input}";
    }

    public string FlagAbusiveReview(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("reviewmoderation.flag-abusive-review", PlatformCost.Select(linux: 32.36, windows: 45.3, macOS: 25.89));
        return $"reviewmoderation.flag-abusive-review:{input}";
    }

    public string SummarizeReviewRatings(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("reviewmoderation.summarize-review-ratings", 21.6);
        return $"reviewmoderation.summarize-review-ratings:{input}";
    }

    public string ListPendingReviews(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("reviewmoderation.list-pending-reviews", 2.24);
        return $"reviewmoderation.list-pending-reviews:{input}";
    }
}
