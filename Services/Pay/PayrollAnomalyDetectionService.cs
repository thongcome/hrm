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
//
// (audit M14, 11 ก.ย. 2569) ทุกอย่างที่ต้องอ่านจากฐานข้อมูลถูกโหลดครั้งเดียวต่อรอบก่อนวนลูป
// เดิมยิง 3–5 query ต่อพนักงาน = 20,000–35,000 query ต่อรอบสำหรับบริษัท 7,000 คน
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

    private sealed record HistoryRow(long Id, long HremployeeId, DateOnly PeriodStart, decimal NetPay);

    // compareAsOfPeriodStart lets HR pin the "จากงวดก่อน" comparison baseline
    // to a specific period, overriding the automatic default. Automatic mode
    // (null) restricts history to periods strictly before this run's own
    // PeriodStart — NOT simply "every other run this employee has". Without
    // that restriction, a run entered out of chronological order (e.g.
    // backfilling an early period after later ones already exist) would pull
    // *future* periods into its "previous period" comparison. When HR picks an
    // explicit baseline period, history is capped at (and includes) that
    // period's PeriodStart instead, so the comparison uses exactly the period they chose.
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
        var employeeIds = employees.Select(e => e.HremployeeId).Distinct().ToList();

        // ── โหลดครั้งเดียว ───────────────────────────────────────────────────
        // ประวัติงวดก่อนของทุกคนในรอบ (เรียงตามงวด) — ตัดที่ baseline ที่ HR เลือก หรือก่อนงวดนี้
        var historyQuery = context.Pay_PayrollEmployees
            .Where(pe => employeeIds.Contains(pe.HremployeeId)
                && pe.PayrollRunId != payrollRunId
                && pe.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled);
        historyQuery = compareAsOfPeriodStart is DateOnly cutoff
            ? historyQuery.Where(pe => pe.Pay_PayrollRun.PeriodStart <= cutoff)
            : historyQuery.Where(pe => pe.Pay_PayrollRun.PeriodStart < run.PeriodStart);
        var historyByEmployee = (await historyQuery
                .Select(pe => new HistoryRow(pe.Id, pe.HremployeeId, pe.Pay_PayrollRun.PeriodStart, pe.NetPay))
                .ToListAsync(ct))
            .GroupBy(h => h.HremployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.PeriodStart).ToList());

        // พนักงานใหม่: ใครเริ่ม onboarding แล้วบ้าง
        var newEmployeeIds = employeeIds.Where(id => !historyByEmployee.ContainsKey(id)).ToList();
        var withOnboarding = newEmployeeIds.Count == 0
            ? new HashSet<long>()
            : (await context.Hrd_LifecycleTaskInstances
                .Where(t => newEmployeeIds.Contains(t.HremployeeId) && t.Direction == LifecycleTaskDirection.Onboarding)
                .Select(t => t.HremployeeId).Distinct().ToListAsync(ct)).ToHashSet();

        // คำสั่งปรับเงินเดือนในงวดนี้ (ใช้อธิบายยอดกระโดด)
        var salaryChangeByEmployee = (await context.Pay_PositionSalaryHistories
                .Where(h => employeeIds.Contains(h.HremployeeId) && h.ChangedDate >= periodStart && h.ChangedDate <= periodEnd)
                .Select(h => new { h.HremployeeId, h.ChangedDate, h.OrderNo })
                .ToListAsync(ct))
            .GroupBy(h => h.HremployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.ChangedDate).First());

        // รายการหักมาตรฐาน: ประเภทที่ปรากฏใน 3 งวดก่อนของแต่ละคน และประเภทที่มีในงวดนี้
        var lastThreeIdsByEmployee = historyByEmployee.ToDictionary(kv => kv.Key, kv => kv.Value.TakeLast(3).Select(h => h.Id).ToList());
        var priorRowIds = lastThreeIdsByEmployee.Values.SelectMany(x => x).ToList();
        var priorTypeRows = priorRowIds.Count == 0
            ? new List<(long PayrollEmployeeId, int PayItemTypeId)>()
            : (await context.Pay_PayrollLineItems
                .Where(li => priorRowIds.Contains(li.PayrollEmployeeId) && ExpectedDeductionTypeIds.Contains(li.PayItemTypeId))
                .Select(li => new { li.PayrollEmployeeId, li.PayItemTypeId })
                .Distinct().ToListAsync(ct))
              .Select(x => (x.PayrollEmployeeId, x.PayItemTypeId)).ToList();
        var priorTypesByRow = priorTypeRows.GroupBy(x => x.PayrollEmployeeId).ToDictionary(g => g.Key, g => g.Select(x => x.PayItemTypeId).ToHashSet());

        var thisRunRowIds = employees.Select(e => e.Id).ToList();
        var thisTypesByRow = (await context.Pay_PayrollLineItems
                .Where(li => thisRunRowIds.Contains(li.PayrollEmployeeId))
                .Select(li => new { li.PayrollEmployeeId, li.PayItemTypeId })
                .Distinct().ToListAsync(ct))
            .GroupBy(x => x.PayrollEmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PayItemTypeId).ToHashSet());

        var typeNames = await context.Pay_PayItemTypes
            .Where(t => ExpectedDeductionTypeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.NameTh, ct);

        // ── ตรวจรายคน (ไม่มี query ในลูปแล้ว) ───────────────────────────────
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

            if (!historyByEmployee.TryGetValue(emp.HremployeeId, out var history) || history.Count == 0)
            {
                CheckNewEmployee(emp, run, periodStart, withOnboarding.Contains(emp.HremployeeId), newRows);
                continue;
            }

            salaryChangeByEmployee.TryGetValue(emp.HremployeeId, out var salaryChange);
            CheckNetPaySpike(emp, history, run, periodStart, periodEnd,
                salaryChange is null ? null : (salaryChange.ChangedDate, salaryChange.OrderNo), newRows);

            var lastThree = lastThreeIdsByEmployee[emp.HremployeeId];
            CheckMissingStandardDeduction(emp, lastThree, priorTypesByRow, thisTypesByRow.GetValueOrDefault(emp.Id), typeNames, newRows);
        }

        await CheckPeriodTotalAsync(context, run, employees, compareAsOfPeriodStart, newRows, ct);

        context.Pay_PayrollAnomalies.AddRange(newRows);
        await context.SaveChangesAsync(ct);
        return newRows.Count;
    }

    // (ก) พนักงานใหม่ — งวดนี้เป็นรายการเงินเดือนงวดแรก ตรวจสอบว่า onboarding
    // เริ่มไปหรือยัง และวันเริ่มงานสอดคล้องกับการที่เพิ่งมีเงินเดือนงวดแรกหรือไม่
    private static void CheckNewEmployee(Pay_PayrollEmployee emp, Pay_PayrollRun run, DateTime periodStart, bool hasOnboarding,
        List<Pay_PayrollAnomaly> newRows)
    {
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
    private static void CheckNetPaySpike(Pay_PayrollEmployee emp, List<HistoryRow> history, Pay_PayrollRun run,
        DateTime periodStart, DateTime periodEnd, (DateTime ChangedDate, string? OrderNo)? salaryChange,
        List<Pay_PayrollAnomaly> newRows)
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

        if (salaryChange is { } sc)
        {
            severity = PayrollAnomalySeverity.Info;
            description += $" — สอดคล้องกับการปรับตำแหน่ง/เงินเดือน (คำสั่งเลขที่ {sc.OrderNo ?? "-"} วันที่ {sc.ChangedDate:dd/MM/yyyy})";
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
    // ปรากฏส่วนใหญ่ (≥ ครึ่ง) หายไปในงวดนี้ ให้แจ้งเตือน
    private static void CheckMissingStandardDeduction(Pay_PayrollEmployee emp, List<long> lastThreeIds,
        Dictionary<long, HashSet<int>> priorTypesByRow, HashSet<int>? thisPeriodTypeIds,
        Dictionary<int, string> typeNames, List<Pay_PayrollAnomaly> newRows)
    {
        if (lastThreeIds.Count == 0) return;

        var priorDeductionTypeCounts = lastThreeIds
            .SelectMany(id => priorTypesByRow.GetValueOrDefault(id) ?? new HashSet<int>())
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());

        var majorityThreshold = (lastThreeIds.Count + 1) / 2; // ceil(n/2)
        var present = thisPeriodTypeIds ?? new HashSet<int>();
        var missingTypeIds = priorDeductionTypeCounts
            .Where(x => x.Value >= majorityThreshold && !present.Contains(x.Key))
            .Select(x => x.Key)
            .ToList();

        if (missingTypeIds.Count == 0) return;

        var missingNames = missingTypeIds.Select(id => typeNames.GetValueOrDefault(id, $"#{id}"));

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
