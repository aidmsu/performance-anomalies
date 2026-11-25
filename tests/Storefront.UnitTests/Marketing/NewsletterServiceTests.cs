using Storefront.Marketing;

namespace Storefront.UnitTests.Marketing;

public sealed class NewsletterServiceTests
{
    [Fact]
    public void Builds_newsletter_audience()
    {
        var result = new NewsletterService().BuildNewsletterAudience("sample-42");

        Assert.Equal("newsletterservice.build-newsletter-audience:sample-42", result);
    }

    [Fact]
    public void Schedules_newsletter()
    {
        var result = new NewsletterService().ScheduleNewsletter("sample-42");

        Assert.Equal("newsletterservice.schedule-newsletter:sample-42", result);
    }

    [Fact]
    public void Renders_newsletter_preview()
    {
        var result = new NewsletterService().RenderNewsletterPreview("sample-42");

        Assert.Equal("newsletterservice.render-newsletter-preview:sample-42", result);
    }

    [Fact]
    public void Lists_newsletter_campaigns()
    {
        var result = new NewsletterService().ListNewsletterCampaigns("sample-42");

        Assert.Equal("newsletterservice.list-newsletter-campaigns:sample-42", result);
    }

    [Fact]
    public void Counts_newsletter_bounces()
    {
        var result = new NewsletterService().CountNewsletterBounces("sample-42");

        Assert.Equal("newsletterservice.count-newsletter-bounces:sample-42", result);
    }
}
