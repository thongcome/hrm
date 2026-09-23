namespace HRM.Services.Km;

// KM article search was plain LIKE '%term%' with no ranking — whatever order the DB happened to
// return was the order shown, so a title match and an incidental mention buried in the body
// scored the same. This is deliberately NOT SQL Server Full-Text Search (CONTAINSTABLE/rank) —
// that needs the Full-Text feature installed and a catalog/index per table, which isn't
// guaranteed on every customer's SQL Server instance (see tools/deploy's own install-kit
// constraints). Instead: LIKE still does the broad candidate retrieval (EntitySearchHelper,
// unchanged), and this pure, unit-tested helper re-ranks the materialized rows in-app — same
// "pure logic separate from EF orchestration" split as Services/Pay/MinimumWageRule.cs.
//
// Multi-word terms are split on whitespace and scored per word so "แผน พัฒนา" finds an article
// whose title has both words even if not adjacent — a single LIKE '%แผน พัฒนา%' would have missed it.
public static class KmSearchRelevance
{
    public sealed record Weights(
        int TitleExact = 100,
        int TitleStartsWith = 70,
        int TitleContains = 50,
        int TagsContains = 30,
        int ContentContains = 10,
        int AllWordsInTitleBonus = 40,
        int MaxPopularityBonus = 20,
        int ViewsPerPopularityPoint = 10);

    private static readonly Weights DefaultWeights = new();

    // null/empty term = no ranking signal (caller falls back to its own recency/popularity sort)
    public static int Score(string title, string? tags, string content, string? searchTerm, int viewCount, Weights? weights = null)
    {
        var words = (searchTerm ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return 0;
        var w = weights ?? DefaultWeights;

        var titleLower = title.ToLowerInvariant();
        var tagsLower = (tags ?? "").ToLowerInvariant();
        var contentLower = content.ToLowerInvariant();

        var score = 0;
        var wordsMatchedInTitle = 0;
        foreach (var raw in words)
        {
            var word = raw.ToLowerInvariant();
            if (titleLower == word) { score += w.TitleExact; wordsMatchedInTitle++; }
            else if (titleLower.StartsWith(word, StringComparison.Ordinal)) { score += w.TitleStartsWith; wordsMatchedInTitle++; }
            else if (titleLower.Contains(word, StringComparison.Ordinal)) { score += w.TitleContains; wordsMatchedInTitle++; }

            if (tagsLower.Contains(word, StringComparison.Ordinal)) score += w.TagsContains;
            if (contentLower.Contains(word, StringComparison.Ordinal)) score += w.ContentContains;
        }
        if (wordsMatchedInTitle == words.Length) score += w.AllWordsInTitleBonus;

        // small popularity nudge, capped so a heavily-viewed off-topic article can never
        // outrank a real text match — it only breaks ties among similarly-relevant results
        score += Math.Min(viewCount / w.ViewsPerPopularityPoint, w.MaxPopularityBonus);

        return score;
    }
}
