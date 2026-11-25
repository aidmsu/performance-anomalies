using Storefront.Infrastructure;

namespace Storefront.Marketing;

/// <summary>Builds and schedules newsletters.</summary>
public sealed class NewsletterService
{
    public string BuildNewsletterAudience(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("newsletterservice.build-newsletter-audience", 4.16);
        return $"newsletterservice.build-newsletter-audience:{input}";
    }

    public string ScheduleNewsletter(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("newsletterservice.schedule-newsletter", 4.03);
        return $"newsletterservice.schedule-newsletter:{input}";
    }

    public string RenderNewsletterPreview(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("newsletterservice.render-newsletter-preview", PlatformCost.Select(linux: 4.97, windows: 6.96, macOS: 3.98));
        return $"newsletterservice.render-newsletter-preview:{input}";
    }

    public string ListNewsletterCampaigns(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("newsletterservice.list-newsletter-campaigns", 21.87);
        return $"newsletterservice.list-newsletter-campaigns:{input}";
    }

    public string CountNewsletterBounces(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("newsletterservice.count-newsletter-bounces", 4.09);
        return $"newsletterservice.count-newsletter-bounces:{input}";
    }
}
