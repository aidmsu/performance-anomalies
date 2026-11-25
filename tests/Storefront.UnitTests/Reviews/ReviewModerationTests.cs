using Storefront.Reviews;

namespace Storefront.UnitTests.Reviews;

public sealed class ReviewModerationTests
{
    [Fact]
    public void Queues_review_for_moderation()
    {
        var result = new ReviewModeration().QueueReviewForModeration("sample-42");

        Assert.Equal("reviewmoderation.queue-review-for-moderation:sample-42", result);
    }

    [Fact]
    public void Approves_review()
    {
        var result = new ReviewModeration().ApproveReview("sample-42");

        Assert.Equal("reviewmoderation.approve-review:sample-42", result);
    }

    [Fact]
    public void Flags_abusive_review()
    {
        var result = new ReviewModeration().FlagAbusiveReview("sample-42");

        Assert.Equal("reviewmoderation.flag-abusive-review:sample-42", result);
    }

    [Fact]
    public void Summarizes_review_ratings()
    {
        var result = new ReviewModeration().SummarizeReviewRatings("sample-42");

        Assert.Equal("reviewmoderation.summarize-review-ratings:sample-42", result);
    }

    [Fact]
    public void Lists_pending_reviews()
    {
        var result = new ReviewModeration().ListPendingReviews("sample-42");

        Assert.Equal("reviewmoderation.list-pending-reviews:sample-42", result);
    }
}
