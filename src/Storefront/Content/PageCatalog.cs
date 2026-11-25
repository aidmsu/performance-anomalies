using Storefront.Infrastructure;

namespace Storefront.Content;

/// <summary>Serves CMS pages.</summary>
public sealed class PageCatalog
{
    public string LoadLandingPage(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("pagecatalog.load-landing-page", 12.35);
        return $"pagecatalog.load-landing-page:{input}";
    }

    public string ListNavigationMenu(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("pagecatalog.list-navigation-menu", 39.54);
        return $"pagecatalog.list-navigation-menu:{input}";
    }

    public string ResolvePageSlug(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("pagecatalog.resolve-page-slug", PlatformCost.Select(linux: 25.17, windows: 35.24, macOS: 20.14));
        return $"pagecatalog.resolve-page-slug:{input}";
    }

    public string RenderPageBreadcrumbs(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("pagecatalog.render-page-breadcrumbs", 7.71);
        return $"pagecatalog.render-page-breadcrumbs:{input}";
    }

    public string ListPublishedBanners(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("pagecatalog.list-published-banners", 3.85);
        return $"pagecatalog.list-published-banners:{input}";
    }
}
