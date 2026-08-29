namespace HRM.Services.Pay;

using HRM.Models;
using HRM.Services.Pay.Calculators;
using Microsoft.EntityFrameworkCore;

// Runs automatically after every PayrollCalculationService.CalculateAsync
// (see the call site there). Purely advisory — flags are surfaced to HR on
// the run/employee detail pages (Pay_PayrollAnomaly.IsAcknowledged, same
// spirit as Pay_EmployeeInsuranceEnrollment.NeedsReview) and never block
// calculation, approval, or payment. Idempotent: re-running detection for
// the same run replaces its previous anomaly rows rather than accumulating
// duplicates, so "คำนวณใหม่" always reflects the latest data.
public class PayrollAnomalyDetectionService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    // System-reserved deduction pay item type ids (see HRMContext seed data):
    // 4=SSO, 5=PF, 6=TAX. Loan (7) is intentionally excluded — not every
    // employee has a loan, so its absence is normal, not anomalous.
    private static readonly int[] ExpectedDeductionTypeIds = { 4, 5, 6 };

    public PayrollAnomalyDetectionService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // compareAsOfPeriodStart lets HR pin the "จากงวดก่อน" comparison baseline
    // to a specific period, overriding the automatic default. Automatic mode
    // (null) restricts history to periods strictly before this run's own
    // PeriodStart — NOT simply "every other run this employee has". Without
    // that restriction, a run entered out of chronological order (e.g.
    // backfilling an early period after later ones already exist) would pull
    // *future* periods into its "previous period" comparison, since the old
    // query only excluded the current run by Id and then took the last item
    // after sorting everything else by PeriodStart. When HR picks an explicit
    // baseline period, history is capped at (and includes) that period's
    // PeriodStart instead, so the comparison uses exactly the period they chose.
    public async Task<int> DetectAnomaliesAsync(long payrollRunId, DateOnly? compareAsOfPeriodStart = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == payrollRunId, ct);
        if (run is null) return 0;

        var existing = context.Pay_PayrollAnomalies.Where(a => a.PayrollRunId == payrollRunId);
        context.Pay_PayrollAnomalies.RemoveRange(existing);

        var employees = await context.Pay_PayrollEmployees
            .Include(pe => pe.Hremployee)
            .Where(pe => pe.PayrollRunId == payrollRunId)
            .ToListAsync(ct);

        var periodStart = run.PeriodStart.ToDateTime(TimeOnly.MinValue);
        var periodEnd = run.PeriodEnd.ToDateTime(TimeOnly.MaxValue);
        var newRows = new List<Pay_PayrollAnomaly>();

        foreach (var emp in employees)
        {
            if (emp.NetPay <= 0)
            {
                newRows.Add(new Pay_PayrollAnomaly
                {
                    PayrollRunId = payrollRunId,
                    PayrollEmployeeId = emp.Id,
                    AnomalyType = PayrollAnomalyType.NetPayNegativeOrZero,
                    Severity = PayrollAnomalySeverity.Critical,
                    Description = $"เงินสุทธิของ {emp.EmpNo} เท่ากับ {emp.NetPay:N2} บาท (ติดลบหรือเป็นศูนย์)",
                    DetectedValue = emp.NetPay,
                });
            }

            var historyQuery = context.Pay_PayrollEmployees
                .Include(pe => pe.Pay_PayrollRun)
                .Where(pe => pe.HremployeeId == emp.HremployeeId
                    && pe.PayrollRunId != payrollRunId
                    && pe.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled);

            historyQuery = compareAsOfPeriodStart is DateOnly cutoff
                ? historyQuery.Where(pe => pe.Pay_PayrollRun.PeriodStart <= cutoff)
                : historyQuery.Where(pe => pe.Pay_PayrollRun.PeriodStart < run.PeriodStart);

            var history = await historyQuery
                .OrderBy(pe => pe.Pay_PayrollRun.PeriodStart)
                .ToListAsync(ct);

            if (history.Count == 0)
            {
                await CheckNewEmployeeAsync(context, emp, run, periodStart, newRows, ct);
            }
            else
            {
                await CheckNetPaySpikeAsync(context, emp, history, run, periodStart, periodEnd, newRows, ct);
                await CheckMissingStandardDeductionAsync(context, emp, history, newRows, ct);
            }
        }

        await CheckPeriodTotalAsync(context, run, employees, compareAsOfPeriodStart, newRows, ct);

        context.Pay_PayrollAnomalies.AddRange(newRows);
        await context.SaveChangesAsync(ct);
        return newRows.Count;
    }

    // (ก) พนักงานใหม่ — งวดนี้เป็นรายการเงินเดือนงวดแรก ตรวจสอบว่า onboarding
    // เริ่มไปหรือยัง และวันเริ่มงานสอดคล้องกับการที่เพิ่งมีเงินเดือนงวดแรกหรือไม่
    private static async Task CheckNewEmployeeAsync(HRMContext context, Pay_PayrollEmployee emp, Pay_PayrollRun run,
        DateTime periodStart, List<Pay_PayrollAnomaly> newRows, CancellationToken ct)
    {
        var hasOnboarding = await context.Hrd_LifecycleTaskInstances
            .AnyAsync(t => t.HremployeeId == emp.HremployeeId && t.Direction == LifecycleTaskDirection.Onboarding, ct);
        if (!hasOnboarding)
        {
            newRows.Add(new Pay_PayrollAnomaly
            {
                PayrollRunId = run.Id,
                PayrollEmployeeId = emp.Id,
                AnomalyType = PayrollAnomalyType.NewEmployeeOnboardingMismatch,
                Severity = PayrollAnomalySeverity.Warning,
                Description = $"{emp.EmpNo} มีรายการเงินเดือนงวดแรก (งวด {run.PayrollPeriod}) แต่ยังไม่มีการเริ่ม Onboarding checklist",
            });
        }

        var workDate = emp.Hremployee?.WorkDate;
        if (workDate.HasValue && (periodStart - workDate.Value).TotalDays > 45)
        {
            newRows.Add(new Pay_PayrollAnomaly
            {
                PayrollRunId = run.Id,
                PayrollEmployeeId = emp.Id,
                AnomalyType = PayrollAnomalyType.NewEmployeeOnboardingMismatch,
                Severity = PayrollAnomalySeverity.Warning,
                Description = $"{emp.EmpNo} วันเริ่มงาน {workDate.Value:dd/MM/yyyy} ห่างจากงวดเงินเดือนงวดแรก ({run.PayrollPeriod}) เกิน 45 วัน — ตรวจสอบว่าพลาดงวดก่อนหน้าหรือไม่",
            });
        }
    }

    // (ข) สุทธิเปลี่ยนแปลงผิดปกติจากงวดก่อน — ตรวจด้วย ML.NET spike detector
    // แล้วเช็คว่ามีคำอธิบาย (ปรับตำแหน่ง/พ้นทดลองงาน) รองรับหรือไม่ ก่อนตั้งระดับ
    private static async Task CheckNetPaySpikeAsync(HRMContext context, Pay_PayrollEmployee emp,
        List<Pay_PayrollEmployee> history, Pay_PayrollRun run, DateTime periodStart, DateTime periodEnd,
        List<Pay_PayrollAnomaly> newRows, CancellationToken ct)
    {
        var series = history.Select(h => (float)h.NetPay).ToList();
        series.Add((float)emp.NetPay);

        var spike = PayrollSpikeDetector.DetectLastPointSpike(series);
        if (spike is null || !spike.IsSpike) return;

        var prevNet = history[^1].NetPay;
        var pctText = prevNet == 0
            ? "จาก 0"
            : $"{Math.Round((emp.NetPay - prevNet) / prevNet * 100m, 2)}%";

        var description = $"เงินสุทธิของ {emp.EmpNo} เปลี่ยนแปลง {pctText} จากงวดก่อน ({prevNet:N2} → {emp.NetPay:N2} บาท)";
        var severity = PayrollAnomalySeverity.Warning;

        var salaryChange = await context.Pay_PositionSalaryHistories
            .Where(h => h.HremployeeId == emp.HremployeeId && h.ChangedDate >= periodStart && h.ChangedDate <= periodEnd)
            .OrderByDescending(h => h.ChangedDate)
            .FirstOrDefaultAsync(ct);

        if (salaryChange is not null)
        {
            severity = PayrollAnomalySeverity.Info;
            description += $" — สอดคล้องกับการปรับตำแหน่ง/เงินเดือน (คำสั่งเลขที่ {salaryChange.OrderNo ?? "-"} วันที่ {salaryChange.ChangedDate:dd/MM/yyyy})";
        }
        else if (emp.Hremployee?.ProbationConfirmedDate is DateTime pcd && pcd >= periodStart && pcd <= periodEnd)
        {
            severity = PayrollAnomalySeverity.Info;
            description += $" — สอดคล้องกับวันที่พ้นทดลองงาน ({pcd:dd/MM/yyyy}) ในงวดนี้";
        }

        newRows.Add(new Pay_PayrollAnomaly
        {
            PayrollRunId = run.Id,
            PayrollEmployeeId = emp.Id,
            AnomalyType = PayrollAnomalyType.NetPayAbnormalChange,
            Severity = severity,
            Description = description,
            DetectedValue = emp.NetPay,
            ReferenceValue = prevNet,
        });
    }

    // (ง) ขาดรายการหักมาตรฐาน — เทียบกับ 3 งวดก่อนหน้า ถ้ารายการหักที่เคย
    // ปรากฏส่วนใหญ่ (≥2/3) หายไปในงวดนี้ ให้แจ้งเตือน
    private static async Task CheckMissingStandardDeductionAsync(HRMContext context, Pay_PayrollEmployee emp,
        List<Pay_PayrollEmployee> history, List<Pay_PayrollAnomaly> newRows, CancellationToken ct)
    {
        var lastThreeIds = history.TakeLast(3).Select(h => h.Id).ToList();
        if (lastThreeIds.Count == 0) return;

        var priorDeductionTypeCounts = await context.Pay_PayrollLineItems
            .Where(li => lastThreeIds.Contains(li.PayrollEmployeeId) && ExpectedDeductionTypeIds.Contains(li.PayItemTypeId))
            .GroupBy(li => li.PayItemTypeId)
            .Select(g => new { PayItemTypeId = g.Key, Count = g.Select(li => li.PayrollEmployeeId).Distinct().Count() })
            .ToListAsync(ct);

        var thisPeriodTypeIds = await context.Pay_PayrollLineItems
            .Where(li => li.PayrollEmployeeId == emp.Id)
            .Select(li => li.PayItemTypeId)
            .ToListAsync(ct);

        var majorityThreshold = (lastThreeIds.Count + 1) / 2; // ceil(n/2)
        var missingTypeIds = priorDeductionTypeCounts
            .Where(x => x.Count >= majorityThreshold && !thisPeriodTypeIds.Contains(x.PayItemTypeId))
            .Select(x => x.PayItemTypeId)
            .ToList();

        if (missingTypeIds.Count == 0) return;

        var missingNames = await context.Pay_PayItemTypes
            .Where(t => missingTypeIds.Contains(t.Id))
            .Select(t => t.NameTh)
            .ToListAsync(ct);

        newRows.Add(new Pay_PayrollAnomaly
        {
            PayrollRunId = emp.PayrollRunId,
            PayrollEmployeeId = emp.Id,
            AnomalyType = PayrollAnomalyType.MissingStandardDeduction,
            Severity = PayrollAnomalySeverity.Warning,
            Description = $"{emp.EmpNo} งวดนี้ไม่มีรายการหัก: {string.Join(", ", missingNames)} ทั้งที่งวดก่อนหน้ามีเป็นปกติ",
        });
    }

    // (จ) ยอดรวมทั้งงวดผิดปกติจากค่าเฉลี่ยงวดก่อนๆ — ระดับทั้งบริษัท/ประเภทรอบเดียวกัน
    private static async Task CheckPeriodTotalAsync(HRMContext context, Pay_PayrollRun run,
        List<Pay_PayrollEmployee> employees, DateOnly? compareAsOfPeriodStart, List<Pay_PayrollAnomaly> newRows, CancellationToken ct)
    {
        var priorTotalsQuery = context.Pay_PayrollEmployees
            .Include(pe => pe.Pay_PayrollRun)
            .Where(pe => pe.Pay_PayrollRun.CompanyId == run.CompanyId
                && pe.Pay_PayrollRun.RunType == run.RunType
                && pe.Pay_PayrollRun.Id != run.Id
                && pe.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled);

        priorTotalsQuery = compareAsOfPeriodStart is DateOnly cutoff
            ? priorTotalsQuery.Where(pe => pe.Pay_PayrollRun.PeriodStart <= cutoff)
            : priorTotalsQuery.Where(pe => pe.Pay_PayrollRun.PeriodStart < run.PeriodStart);

        var priorTotals = await priorTotalsQuery
            .GroupBy(pe => new { pe.Pay_PayrollRun.Id, pe.Pay_PayrollRun.PeriodStart })
            .Select(g => new { g.Key.PeriodStart, Total = g.Sum(pe => pe.NetPay) })
            .OrderBy(x => x.PeriodStart)
            .ToListAsync(ct);

        var currentTotal = employees.Sum(e => e.NetPay);
        var series = priorTotals.Select(x => (float)x.Total).ToList();
        series.Add((float)currentTotal);

        var spike = PayrollSpikeDetector.DetectLastPointSpike(series);
        if (spike is null || !spike.IsSpike) return;

        var priorAverage = priorTotals.Count > 0 ? priorTotals.Average(x => x.Total) : 0m;
        newRows.Add(new Pay_PayrollAnomaly
        {
            PayrollRunId = run.Id,
            PayrollEmployeeId = null,
            AnomalyType = PayrollAnomalyType.PeriodTotalAbnormal,
            Severity = PayrollAnomalySeverity.Warning,
            Description = $"ยอดสุทธิรวมทั้งงวด {run.PayrollPeriod} ({currentTotal:N2} บาท) ต่างจากค่าเฉลี่ยงวดก่อนหน้า ({priorAverage:N2} บาท) ผิดปกติ",
            DetectedValue = currentTotal,
            ReferenceValue = priorAverage,
        });
    }
}
