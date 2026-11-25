using Storefront.Infrastructure;

namespace Storefront.Search;

public sealed record SearchDocument(string Sku, string Title, string Brand);

/// <summary>Builds the product search index. Each analyzer stage runs over the whole catalog.</summary>
public sealed class SearchIndexBuilder
{
    private const double TokenizeCostMs = 60;
    private const double AnalyzerStageCostMs = 25;

    private static readonly string[] AnalyzerStages =
    [
        "synonyms",
        "attributes",
        "phonetic",
    ];

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Build(IReadOnlyList<SearchDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);
        Compute.Run("tokenize-catalog", TokenizeCostMs);
        foreach (var stage in AnalyzerStages)
        {
            Compute.Run($"analyzer-{stage}", AnalyzerStageCostMs);
        }
        return documents
            .SelectMany(d => d.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(t => (Term: t.ToLowerInvariant(), d.Sku)))
            .GroupBy(x => x.Term)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Sku).Distinct().ToList());
    }
}
