using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;

namespace HRM.Services.Job;

// "มี AI คอยมอง JD ว่าอันไหนควรเป็น competency (ใช้ ML.Net)" — CEO order
// 2026-09-01. Suggests which competency-catalog entries each unlinked JD
// line (duty/qualification text) most resembles, so HR can one-click
// accept the link instead of scanning the catalog by hand.
//
// Algorithm: TF-IDF-style text vectorization via ML.NET's FeaturizeText
// over the catalog's code+name+description, then cosine similarity of each
// query line's vector against every catalog vector; top-3 per line above a
// small floor. The featurizer combines word n-grams AND char n-grams —
// char trigrams are what make this work acceptably for Thai, which has no
// word spacing (word tokens alone would treat a whole Thai clause as one
// opaque token; character n-grams still overlap on shared substrings).
// The model is fit on the combined corpus (catalog + query lines) so both
// sides share one vocabulary; with L2 normalization, cosine similarity is
// a plain dot product.
//
// Everything runs in-process, on demand, per button click — no background
// scheduler (this app has none, deliberately) and no persisted model file:
// the catalog is small (tens–hundreds of rows), so fitting takes
// milliseconds and staleness is impossible.
//
// Static helper rather than a DI service — same convention as
// JobReportingLineHelper/EntitySearchHelper, and it keeps Program.cs
// untouched (no registration needed).
public static class JdCompetencySuggestionService
{
    public sealed record CatalogItem(long CompetencyId, string DisplayName, string Text);
    public sealed record QueryLine(string Key, string Text);
    public sealed record Suggestion(long CompetencyId, string DisplayName, double Score);

    private sealed class Doc
    {
        public string Text { get; set; } = "";
    }

    private sealed class DocVector
    {
        [VectorType]
        public float[] Features { get; set; } = Array.Empty<float>();
    }

    // Scores below this are noise (barely-overlapping char n-grams), not
    // worth showing as a chip.
    private const double MinScore = 0.08;
    private const int TopN = 3;

    public static Dictionary<string, List<Suggestion>> Suggest(
        IReadOnlyList<CatalogItem> catalog, IReadOnlyList<QueryLine> lines)
    {
        var result = new Dictionary<string, List<Suggestion>>();
        if (catalog.Count == 0 || lines.Count == 0) return result;

        var ml = new MLContext(seed: 0);

        // One corpus: catalog docs first, then query lines — index math below
        // relies on this ordering.
        var docs = catalog.Select(c => new Doc { Text = c.Text })
            .Concat(lines.Select(l => new Doc { Text = l.Text }))
            .ToList();
        var data = ml.Data.LoadFromEnumerable(docs);

        var pipeline = ml.Transforms.Text.FeaturizeText("Features",
            new TextFeaturizingEstimator.Options
            {
                // Word n-grams (helps English/code-mixed text) + char
                // trigrams (carries Thai, see class comment).
                WordFeatureExtractor = new WordBagEstimator.Options { NgramLength = 2, UseAllLengths = true },
                CharFeatureExtractor = new WordBagEstimator.Options { NgramLength = 3, UseAllLengths = false },
                Norm = TextFeaturizingEstimator.NormFunction.L2,
            },
            nameof(Doc.Text));

        var transformed = pipeline.Fit(data).Transform(data);
        var vectors = ml.Data.CreateEnumerable<DocVector>(transformed, reuseRowObject: false)
            .Select(v => v.Features)
            .ToList();

        for (var li = 0; li < lines.Count; li++)
        {
            var lineVector = vectors[catalog.Count + li];
            var scored = new List<Suggestion>();
            for (var ci = 0; ci < catalog.Count; ci++)
            {
                var score = Dot(lineVector, vectors[ci]); // == cosine (L2-normalized)
                if (score >= MinScore)
                    scored.Add(new Suggestion(catalog[ci].CompetencyId, catalog[ci].DisplayName, score));
            }
            result[lines[li].Key] = scored
                .OrderByDescending(s => s.Score)
                .Take(TopN)
                .ToList();
        }
        return result;
    }

    private static double Dot(float[] a, float[] b)
    {
        var n = Math.Min(a.Length, b.Length);
        double sum = 0;
        for (var i = 0; i < n; i++) sum += (double)a[i] * b[i];
        return sum;
    }
}
