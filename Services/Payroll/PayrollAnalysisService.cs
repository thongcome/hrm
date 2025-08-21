using HRM.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries; // เพิ่ม using ตัวนี้
using System.Collections.Generic;
using System.IO;

namespace HRM.Services.Payroll
{
    // CLASS 1: คลาสสำหรับเก็บผลลัพธ์ที่เราจะส่งกลับไปให้ Blazor
    public class AnomalyResult
    {
        public string EmpNo { get; set; }
        public string FeatureName { get; set; } // เช่น "เงินเดือน", "OT"
        public decimal Value { get; set; }      // ค่าที่ผิดปกติ
        public string Description { get; set; } // คำอธิบาย
    }

    // CLASS 2: คลาสหลักของ Service
    public class PayrollAnalysisService
    {
        // --- ส่วนของ Input/Output สำหรับโมเดล ---
        private class PayrollFeatureInput
        {
            public float Value { get; set; }
        }

        private class SsaSpikePrediction
        {
            [ColumnName("Anomaly")]
            public VBuffer<double> Prediction { get; set; }
        }

        // --- ส่วนของการจัดการ Prediction Engines ---
        // เปลี่ยนชนิดของ Engine เป็น TimeSeriesPredictionEngine
        private readonly TimeSeriesPredictionEngine<PayrollFeatureInput, SsaSpikePrediction> _salaryEngine;
        private readonly TimeSeriesPredictionEngine<PayrollFeatureInput, SsaSpikePrediction> _overtimeEngine;
        private readonly TimeSeriesPredictionEngine<PayrollFeatureInput, SsaSpikePrediction> _otherIncomeEngine;
        private readonly TimeSeriesPredictionEngine<PayrollFeatureInput, SsaSpikePrediction> _deductionsEngine;

        public PayrollAnalysisService()
        {
            var mlContext = new MLContext();

            _salaryEngine = CreatePredictionEngine(mlContext, "salary_spike_model.zip");
            _overtimeEngine = CreatePredictionEngine(mlContext, "overtime_spike_model.zip");
            _otherIncomeEngine = CreatePredictionEngine(mlContext, "other_income_spike_model.zip");
            _deductionsEngine = CreatePredictionEngine(mlContext, "deductions_spike_model.zip");
        }

        // Helper method ที่แก้ไขแล้ว
        private TimeSeriesPredictionEngine<PayrollFeatureInput, SsaSpikePrediction> CreatePredictionEngine(MLContext mlContext, string modelFileName)
        {
            string executionDirectory = AppContext.BaseDirectory;
            var modelPath = Path.Combine(executionDirectory, "MLModels", modelFileName);

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"ไม่พบไฟล์โมเดลที่จำเป็น: {modelPath}. กรุณาตรวจสอบว่าได้ตั้งค่า 'Copy to Output Directory' เป็น 'Copy if newer' ให้กับไฟล์ {modelFileName} แล้ว");
            }

            var model = mlContext.Model.Load(modelPath, out _);

            // ใช้ Engine สำหรับ Time Series โดยเฉพาะ
            return model.CreateTimeSeriesEngine<PayrollFeatureInput, SsaSpikePrediction>(mlContext);
        }

        // --- เมธอดหลักสำหรับตรวจจับความผิดปกติ (ไม่ต้องแก้ไข) ---
        public List<AnomalyResult> DetectAnomalies(List<Hrpayroll> payrolls)
        {
            var anomalies = new List<AnomalyResult>();

            foreach (var payroll in payrolls)
            {
                if (string.IsNullOrEmpty(payroll.EmpNo)) continue;

                CheckFeature(payroll, (float)(payroll.SalarybaseAmt ?? 0), _salaryEngine, "เงินเดือนพื้นฐาน", anomalies);
                CheckFeature(payroll, (float)(payroll.SalaryothAmt ?? 0), _otherIncomeEngine, "รายได้อื่น", anomalies);
                CheckFeature(payroll, (float)(payroll.SalarysubtAmt ?? 0), _deductionsEngine, "รายการหัก", anomalies);
            }

            return anomalies;
        }

        // Helper method สำหรับตรวจสอบแต่ละ Feature (ไม่ต้องแก้ไข)
        private void CheckFeature(Hrpayroll payroll, float value, TimeSeriesPredictionEngine<PayrollFeatureInput, SsaSpikePrediction> engine, string featureName, List<AnomalyResult> anomalies)
        {
            var input = new PayrollFeatureInput { Value = value };
            var prediction = engine.Predict(input);
            var alert = prediction.Prediction.GetValues()[0];

            if (alert == 1)
            {
                if (payroll.EmpNo != null)
                {

                    anomalies.Add(new AnomalyResult
                    {
                        EmpNo = payroll.EmpNo,
                        FeatureName = featureName,
                        Value = (decimal)value,
                        Description = $"{featureName} มีค่าผิดปกติ: {value:N2}"
                    });
                }
            }
        }
    }
}