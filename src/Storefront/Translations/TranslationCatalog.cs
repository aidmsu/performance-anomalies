using Storefront.Infrastructure;

namespace Storefront.Translations;

/// <summary>Serves UI translations.</summary>
public sealed class TranslationCatalog
{
    public string LoadTranslationBundle(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("translationcatalog.load-translation-bundle", 26.29);
        return $"translationcatalog.load-translation-bundle:{input}";
    }

    public string FindMissingTranslations(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("translationcatalog.find-missing-translations", 3.3);
        return $"translationcatalog.find-missing-translations:{input}";
    }

    public string ListSupportedLanguages(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("translationcatalog.list-supported-languages", PlatformCost.Select(linux: 4.41, windows: 6.17, macOS: 3.53));
        return $"translationcatalog.list-supported-languages:{input}";
    }

    public string ResolveFallbackLanguage(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("translationcatalog.resolve-fallback-language", 17.82);
        return $"translationcatalog.resolve-fallback-language:{input}";
    }

    public string CountTranslatedKeys(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        SimulatedDatabase.Query("translationcatalog.count-translated-keys", 14.18);
        return $"translationcatalog.count-translated-keys:{input}";
    }
}
