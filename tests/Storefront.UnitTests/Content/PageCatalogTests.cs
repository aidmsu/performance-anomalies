using Storefront.Content;

namespace Storefront.UnitTests.Content;

public sealed class PageCatalogTests
{
    [Fact]
    public void Loads_landing_page()
    {
        var result = new PageCatalog().LoadLandingPage("sample-42");

        Assert.Equal("pagecatalog.load-landing-page:sample-42", result);
    }

    [Fact]
    public void Lists_navigation_menu()
    {
        var result = new PageCatalog().ListNavigationMenu("sample-42");

        Assert.Equal("pagecatalog.list-navigation-menu:sample-42", result);
    }

    [Fact]
    public void Resolves_page_slug()
    {
        var result = new PageCatalog().ResolvePageSlug("sample-42");

        Assert.Equal("pagecatalog.resolve-page-slug:sample-42", result);
    }

    [Fact]
    public void Renders_page_breadcrumbs()
    {
        var result = new PageCatalog().RenderPageBreadcrumbs("sample-42");

        Assert.Equal("pagecatalog.render-page-breadcrumbs:sample-42", result);
    }

    [Fact]
    public void Lists_published_banners()
    {
        var result = new PageCatalog().ListPublishedBanners("sample-42");

        Assert.Equal("pagecatalog.list-published-banners:sample-42", result);
    }
}
