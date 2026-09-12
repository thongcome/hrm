namespace Advance.Payroll.Engine;

using Microsoft.ML;
using Microsoft.ML.Data;

// Ported verbatim from HRM Services/Pay/Calculators/PayrollSpikeDetector.cs. Kept in
// Engine rather than Core: it's pure (no DB) but pulls in Microsoft.ML, which the task's
// explicit Core list (TaxBracketCalculator/ProrationCalculator/SocialSecurityCalculator/
// ProvidentFundCalculator/PayScheduleResolver/NetPayGuardService/BankFileTemplate/
// EFilingFormats) does not include — Core's whole point is proving a zero-extra-
// dependency compile, and ML.NET is a meaningfully heavy package to add to that story
// for one detector used only by PayrollAnomalyDetectionService.
public static class PayrollSpikeDetector
{
    public record SpikeResult(bool IsSpike, double Score, double PValue);

    public static SpikeResult? DetectLastPointSpike(IReadOnlyList<float> orderedValues, double confidence = 95.0)
    {
        if (orderedValues.Count < 4) return null;

        var mlContext = new MLContext(seed: 1);
        var data = mlContext.Data.LoadFromEnumerable(orderedValues.Select(v => new SpikeInput { Value = v }));

        var pipeline = mlContext.Transforms.DetectIidSpike(
            outputColumnName: nameof(SpikeOutput.Prediction),
            inputColumnName: nameof(SpikeInput.Value),
            confidence: confidence,
            pvalueHistoryLength: Math.Max(2, orderedValues.Count / 2));

        var transformed = pipeline.Fit(data).Transform(data);
        var rows = mlContext.Data.CreateEnumerable<SpikeOutput>(transformed, reuseRowObject: false).ToList();
        var last = rows[^1].Prediction;

        return new SpikeResult(IsSpike: last[0] == 1.0, Score: last[1], PValue: last[2]);
    }

    private class SpikeInput
    {
        public float Value { get; set; }
    }

    private class SpikeOutput
    {
        [VectorType(3)]
        public double[] Prediction { get; set; } = null!;
    }
}
