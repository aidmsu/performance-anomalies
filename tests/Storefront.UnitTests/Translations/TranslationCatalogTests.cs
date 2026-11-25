using Storefront.Translations;

namespace Storefront.UnitTests.Translations;

public sealed class TranslationCatalogTests
{
    [Fact]
    public void Loads_translation_bundle()
    {
        var result = new TranslationCatalog().LoadTranslationBundle("sample-42");

        Assert.Equal("translationcatalog.load-translation-bundle:sample-42", result);
    }

    [Fact]
    public void Finds_missing_translations()
    {
        var result = new TranslationCatalog().FindMissingTranslations("sample-42");

        Assert.Equal("translationcatalog.find-missing-translations:sample-42", result);
    }

    [Fact]
    public void Lists_supported_languages()
    {
        var result = new TranslationCatalog().ListSupportedLanguages("sample-42");

        Assert.Equal("translationcatalog.list-supported-languages:sample-42", result);
    }

    [Fact]
    public void Resolves_fallback_language()
    {
        var result = new TranslationCatalog().ResolveFallbackLanguage("sample-42");

        Assert.Equal("translationcatalog.resolve-fallback-language:sample-42", result);
    }

    [Fact]
    public void Counts_translated_keys()
    {
        var result = new TranslationCatalog().CountTranslatedKeys("sample-42");

        Assert.Equal("translationcatalog.count-translated-keys:sample-42", result);
    }
}
