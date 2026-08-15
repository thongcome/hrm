namespace HRM.Services.Pay.Calculators;

using Microsoft.ML;
using Microsoft.ML.Data;

// Pure, no DB access. Wraps ML.NET's IID spike detector (Microsoft.ML.TimeSeries)
// to answer one question: "is the last point in this ordered series a
// statistical spike relative to the rest of the series?"
//
// Deliberately NOT using a persisted/trained model file (unlike the legacy
// Services/Payroll/PayrollAnalysisService.cs, which loads pretrained SSA
// models from MLModels/*.zip against the old Hrpayroll table) — IidSpikeEstimator
// is designed to be fit fresh against whatever series you hand it, computing
// its p-values from that series alone, so there's no model-training or
// versioning step and no dependency on the old Hrpayroll schema. This suits
// payroll history well: each employee/company only has a handful of periods
// (tens at most), far short of what SSA's seasonality assumptions need.
public static class PayrollSpikeDetector
{
    public record SpikeResult(bool IsSpike, double Score, double PValue);

    // orderedValues must be in chronological order; the LAST element is the
    // point being tested against the rest of the series. Returns null (not a
    // false "not a spike") when there isn't enough history to say anything
    // meaningful — never fabricate a signal from too little data.
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

        // Prediction is a 3-vector: [Alert (0/1), Raw Score, P-Value]
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
