namespace HRM.Services.Pay;

using System.Text.Json;
using HRM.Models;
using HRM.Services.Pay.Calculators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record PayrollRunCalculationSummary(int EmployeeCount, int NegativeNetPayCount, decimal TotalNetPay);

// Orchestrates one payroll run's Gross->Net calculation. Fixes, relative to
// the legacy Components\Pages\Payroll\PayrollProcess.razor CalculatePayroll():
//   - overtime and loan deductions are scoped to the pay period (see
//     OvertimeEarningsCalculator / LoanDeductionCalculator)
//   - withholding tax is a real cumulative progressive calculation (see
//     TaxBracketCalculator), not a single-bracket flat rate
//   - proration for mid-period joiners/leavers (never existed before)
//   - a negative-net-pay guard (never existed before)
//   - the per-employee tax-bracket breakdown is persisted (Pay_PayrollAuditLog)
//     instead of being built in-memory and discarded (legacy HREmpTaxRateDet)
public class PayrollCalculationService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly ISocialSecurityRateProvider _socialSecurityRateProvider;
    private readonly OvertimeEarningsCalculator _overtimeCalculator;
    private readonly LoanDeductionCalculator _loanCalculator;
    private readonly PayrollAnomalyDetectionService _anomalyDetectionService;
    private readonly ILogger<PayrollCalculationService> _logger;

    public PayrollCalculationService(
        IDbContextFactory<HRMContext> dbFactory,
        ISocialSecurityRateProvider socialSecurityRateProvider,
        OvertimeEarningsCalculator overtimeCalculator,
        LoanDeductionCalculator loanCalculator,
        PayrollAnomalyDetectionService anomalyDetectionService,
        ILogger<PayrollCalculationService> logger)
    {
        _dbFactory = dbFactory;
        _socialSecurityRateProvider = socialSecurityRateProvider;
        _overtimeCalculator = overtimeCalculator;
        _loanCalculator = loanCalculator;
        _anomalyDetectionService = anomalyDetectionService;
        _logger = logger;
    }

    // `progress` (optional) receives (done, total) as employees are processed —
    // throttled to ~100 reports per run so a 7,000-employee company doesn't
    // flood the caller. Callers that don't care pass null.
    public async Task<PayrollRunCalculationSummary> CalculateAsync(long payrollRunId, long actorUserId, IProgress<(int Done, int Total)>? progress = null, CancellationToken ct = default, bool runAnomalyDetection = true)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        // A 7,000-employee run commits ~23,000 rows (employees + line items + the
        // audit interceptor's rows) in one SaveChanges; on a loaded server that
        // legitimately exceeds the 30 s default and failed a real run with
        // "Execution Timeout Expired" while another job and a build shared the box.
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(15));

        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == payrollRunId, ct)
            ?? throw new InvalidOperationException($"Pay_PayrollRun {payrollRunId} not found.");

        if (run.Status != PayrollRunStatus.Draft && run.Status != PayrollRunStatus.Calculated)
            throw new InvalidOperationException($"Cannot calculate a run in status {run.Status}. Only Draft or Calculated runs can be (re)calculated.");

        // ── ชนิดของรอบกำหนดว่าคำนวณอะไร (audit C1/C2, 11 ก.ย. 2569) ─────────────────
        //   Regular    = เงินเดือนเต็มงวด
        //   Reversal   = ค่าลบของรอบต้นทาง สร้างสำเร็จรูปตอนกลับรายการ — ห้ามคำนวณใหม่เด็ดขาด
        //   Adjustment = คำนวณงวดนั้นใหม่ทั้งงวดหลังกลับรายการรอบเดิมแล้ว (reverse → adjust)
        //                ถ้ายังไม่กลับรายการ การคำนวณจะกลายเป็นจ่ายซ้ำ จึงต้องมี reversal ก่อน
        //   Bonus      = "รอบเสริม" — จ่ายเฉพาะรายการเฉพาะกิจที่ HR อนุมัติไว้ให้งวดนี้ (โบนัส
        //                คอมมิชชัน ฯลฯ) ไม่มีเงินเดือน/OT/สวัสดิการ/ประกันสังคม/กองทุน/เงินกู้
        //                ภาษีคิดแบบส่วนต่าง (ภาษีทั้งปีรวมโบนัส − ภาษีทั้งปีไม่รวมโบนัส)
        if (run.RunType == PayrollRunType.Reversal)
            throw new InvalidOperationException("รอบกลับรายการคำนวณใหม่ไม่ได้ — ยอดเป็นค่าลบของรอบต้นทางเสมอ ถ้าต้องแก้ ให้ยกเลิกรอบนี้แล้วกลับรายการใหม่");
        if (run.RunType == PayrollRunType.Adjustment)
        {
            var reversed = run.AdjustmentOfRunId is long origId && await context.Pay_PayrollRuns.AnyAsync(r =>
                r.AdjustmentOfRunId == origId && r.RunType == PayrollRunType.Reversal
                && r.Status != PayrollRunStatus.Cancelled, ct);
            if (!reversed)
                throw new InvalidOperationException(
                    $"รอบปรับปรุงคำนวณได้ต่อเมื่อรอบต้นทาง #{run.AdjustmentOfRunId} ถูกกลับรายการแล้ว — ไม่งั้นพนักงานจะได้เงินงวดนี้สองครั้ง");
        }
        var supplementary = run.RunType == PayrollRunType.Bonus;

        // ลำดับงวดต้องถูก (audit H3): ภาษีสะสมของงวดนี้อ่านจากงวดก่อนหน้าที่ "อนุมัติแล้ว" เท่านั้น
        // ดังนั้นงวดก่อนหน้าในปีเดียวกันต้องอนุมัติ/ยกเลิกให้หมดก่อน และห้ามคำนวณงวดเก่าซ้ำ
        // เมื่อมีงวดหลังจากนั้นอนุมัติไปแล้ว (ยอดสะสมที่งวดหลังใช้ไปจะไม่ตรงกับความจริง)
        var yearStartForOrder = new DateOnly(run.PeriodStart.Year, 1, 1);
        var openEarlier = await context.Pay_PayrollRuns
            .Where(r => r.CompanyId == run.CompanyId && r.Id != run.Id
                        && r.RunType != PayrollRunType.Reversal
                        && r.PeriodStart >= yearStartForOrder && r.PeriodStart < run.PeriodStart
                        && r.Status != PayrollRunStatus.Cancelled && r.Status < PayrollRunStatus.Approved)
            .Select(r => r.PayrollPeriod).Distinct().OrderBy(p => p).ToListAsync(ct);
        if (openEarlier.Count > 0)
            throw new InvalidOperationException(
                $"งวด {string.Join(", ", openEarlier)} ยังไม่ได้อนุมัติ — ต้องอนุมัติหรือยกเลิกงวดก่อนหน้าให้ครบก่อน ไม่งั้นภาษีสะสมของงวดนี้จะขาด");
        var approvedLater = await context.Pay_PayrollRuns
            .Where(r => r.CompanyId == run.CompanyId && r.Id != run.Id
                        && r.PeriodStart > run.PeriodStart && r.PeriodStart.Year == run.PeriodStart.Year
                        && r.Status >= PayrollRunStatus.Approved && r.Status != PayrollRunStatus.Cancelled)
            .Select(r => r.PayrollPeriod).Distinct().OrderBy(p => p).ToListAsync(ct);
        // รอบปรับปรุง = แก้งวดเก่าที่กลับรายการแล้ว ยอมให้ทำแม้งวดหลังอนุมัติไปแล้ว (ยอดสะสมของงวดหลัง
        // จะปรับตัวเองในรอบปกติถัดไป เพราะ YTD อ่านจากรอบที่อนุมัติทั้งหมด: ต้นทาง + กลับรายการ + ปรับปรุง)
        if (run.RunType == PayrollRunType.Regular && approvedLater.Count > 0)
            throw new InvalidOperationException(
                $"งวด {string.Join(", ", approvedLater)} อนุมัติไปแล้วโดยใช้ยอดสะสมจากงวดนี้ — คำนวณงวดนี้ใหม่ไม่ได้ ถ้าต้องแก้ให้กลับรายการงวดหลังก่อน");
        if (supplementary)
        {
            // รอบโบนัสอาศัยเงินเดือนงวดเดียวกันเป็นฐานประมาณการทั้งปี จึงต้องมีรอบปกติที่อนุมัติแล้ว
            var regularApproved = await context.Pay_PayrollRuns.AnyAsync(r =>
                r.CompanyId == run.CompanyId && r.PayrollPeriod == run.PayrollPeriod
                && r.RunType == PayrollRunType.Regular && r.Status >= PayrollRunStatus.Approved
                && r.Status != PayrollRunStatus.Cancelled, ct);
            if (!regularApproved)
                throw new InvalidOperationException($"รอบโบนัสของงวด {run.PayrollPeriod} คำนวณได้หลังรอบปกติของงวดเดียวกันอนุมัติแล้ว");
        }

        // ทั้งการล้างของเก่าและการเขียนของใหม่อยู่ใน transaction เดียว (audit H5): เดิมถ้าล้มกลางทาง
        // จะเหลือรอบว่างในสถานะ Calculated ซึ่งส่งตรวจ/อนุมัติ/post ได้
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        // idempotent while unlocked: wipe any existing employee/line-item rows for this run first
        var existingEmployeeIds = await context.Pay_PayrollEmployees
            .Where(e => e.PayrollRunId == payrollRunId)
            .Select(e => e.Id)
            .ToListAsync(ct);

        if (existingEmployeeIds.Count > 0)
        {
            // Bulk set-based deletes (ExecuteDeleteAsync) on purpose. The previous
            // RemoveRange + SaveChanges path change-tracked every row and the audit
            // interceptor wrote one AuditLog row per deleted entity — on a 6,788-
            // employee recalculation that is ~36,000 tracked deletes (employees +
            // line items + anomalies) and took minutes of CPU before the first
            // employee was even processed ("กำลังเตรียมข้อมูล" hanging). These rows
            // are derived calculation output, regenerated immediately below; the
            // run-level Pay_PayrollAuditLog transition row is the audit record.
            //
            // Order matters: Pay_PayrollAuditLog.PayrollEmployeeId and
            // Pay_PayrollAnomaly.PayrollEmployeeId are Restrict FKs (deliberately, to
            // avoid SQL Server's "multiple cascade paths" against Run->AuditLog), so
            // they must go before the employee rows or the delete throws.
            await context.Pay_PayrollLineItems
                .Where(li => li.Pay_PayrollEmployee.PayrollRunId == payrollRunId)
                .ExecuteDeleteAsync(ct);
            await context.Pay_PayrollAuditLogs
                .Where(a => a.PayrollEmployeeId != null && a.Pay_PayrollEmployee!.PayrollRunId == payrollRunId)
                .ExecuteDeleteAsync(ct);
            await context.Pay_PayrollAnomalies
                .Where(a => a.PayrollEmployeeId != null && a.Pay_PayrollEmployee!.PayrollRunId == payrollRunId)
                .ExecuteDeleteAsync(ct);
            await context.Pay_PayrollEmployees
                .Where(e => e.PayrollRunId == payrollRunId)
                .ExecuteDeleteAsync(ct);
        }

        var payItemTypes = await context.Pay_PayItemTypes.ToDictionaryAsync(t => t.Code, ct);
        var taxBrackets = await context.Pay_TaxBrackets
            .Where(b => b.EffectiveYear == run.PeriodStart.Year && b.IsActive)
            .ToListAsync(ct);
        // ตารางว่าง = ภาษี 0 ทุกคนเงียบ ๆ (audit H4) — ต้องหยุด ไม่ใช่เดาว่าไม่มีภาษี
        if (taxBrackets.Count == 0)
            throw new InvalidOperationException(
                $"ไม่มีตารางอัตราภาษีปี {run.PeriodStart.Year} (Pay_TaxBracket) — เพิ่มตารางของปีนี้ก่อนจึงคำนวณได้");

        // Standard/mandatory deduction parameters for this tax year — falls
        // back to the current legal defaults (60,000 personal allowance,
        // 50%/100,000 expense deduction) if HR hasn't seeded a row for this
        // year yet, so calculation never silently reverts to the old
        // zero-deduction bug just because a year's row is missing.
        var taxDeductionSetting = await context.Pay_TaxDeductionSettings
            .FirstOrDefaultAsync(s => s.EffectiveYear == run.PeriodStart.Year && s.IsActive, ct);
        var personalAllowancePerMonth = (taxDeductionSetting?.PersonalAllowancePerYear ?? 60000m) / 12m;
        var expenseDeductionRate = taxDeductionSetting?.ExpenseDeductionRate ?? 0.50m;
        var expenseDeductionCap = taxDeductionSetting?.ExpenseDeductionCap ?? 100000m;

        // Only elections the employee chose to apply monthly ("จ่ายให้น้อยสุด")
        // reduce withholding now — ApplyMonthly=false ("จ่ายก่อนขอคืน") rows
        // are intentionally excluded here; they exist for the employee's own
        // records only.
        var monthlyTaxElections = await context.Pay_EmployeeTaxDeductionElections
            .Include(e => e.Pay_TaxDeductionType)
            .Where(e => e.IsActive && e.ApplyMonthly && e.Pay_TaxDeductionType.EffectiveYear == run.PeriodStart.Year)
            .ToListAsync(ct);

        // Mid-year hires only — see Pay_EmployeePriorEmployerIncome.cs and
        // GetYtdAccumulatorsAsync/FoldPriorEmployerIncome below.
        var priorEmployerIncomes = await context.Pay_EmployeePriorEmployerIncomes
            .Where(p => p.IsActive && p.TaxYear == run.PeriodStart.Year)
            .ToListAsync(ct);

        // Attendance → money (HR gap wave 1). Policy is per company and OFF by
        // default; attendance rows are loaded once for the period and grouped.
        var attendancePolicy = await context.Pay_AttendanceDeductionPolicies
            .FirstOrDefaultAsync(p => p.CompanyId == run.CompanyId && p.IsActive, ct);
        var attendanceByEmployee = (await context.Att_DailyAttendances
                .Where(a => a.CompanyId == run.CompanyId && a.WorkDate >= run.PeriodStart && a.WorkDate <= run.PeriodEnd)
                .Select(a => new { a.HremployeeId, a.IsAbsent, a.LateMinutes })
                .ToListAsync(ct))
            .GroupBy(a => a.HremployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Salary advances recovered in this period: Approved/Paid ones, plus those
        // already consumed by THIS run so a recalculation re-picks them idempotently.
        var advances = await context.Pay_SalaryAdvances
            .Where(a => a.CompanyId == run.CompanyId && a.TargetPeriod == run.PayrollPeriod
                        && (a.Status == PaySalaryAdvanceStatus.Approved || a.Status == PaySalaryAdvanceStatus.Paid
                            || (a.Status == PaySalaryAdvanceStatus.Deducted && a.ConsumedByPayrollRunId == run.Id)))
            .ToListAsync(ct);

        var (ssoRate, ssoCap) = await _socialSecurityRateProvider.GetCurrentRateAsync(run.CompanyId, ct);

        var periodEndDt = run.PeriodEnd.ToDateTime(TimeOnly.MaxValue);
        var periodStartDt = run.PeriodStart.ToDateTime(TimeOnly.MinValue);

        var eligibleEmployees = await context.Hremployee
            .Where(e => e.companyid == run.CompanyId
                        && e.WorkDate != null && e.WorkDate <= periodEndDt
                        && (e.ResignDate == null || e.ResignDate >= periodStartDt))
            .ToListAsync(ct);

        // รอบเสริม: เฉพาะคนที่มีรายการเฉพาะกิจของงวดนี้ (ไม่สร้างแถวศูนย์ให้ทั้งบริษัท)
        // และดึงแถวรอบปกติของงวดเดียวกันไว้เป็นฐานประมาณการภาษี
        var regularRowsThisPeriod = new Dictionary<long, Pay_PayrollEmployee>();
        if (supplementary)
        {
            var withItems = (await context.Pay_AdhocPayItems
                    .Where(a => a.TargetPeriod == run.PayrollPeriod
                                && (a.Status == PayAdhocItemStatus.Approved
                                    || (a.Status == PayAdhocItemStatus.Consumed && a.ConsumedByPayrollRunId == run.Id)))
                    .Select(a => a.HremployeeId).Distinct().ToListAsync(ct)).ToHashSet();
            eligibleEmployees = eligibleEmployees.Where(e => withItems.Contains(e.id)).ToList();

            regularRowsThisPeriod = await context.Pay_PayrollEmployees
                .Where(pe => pe.Pay_PayrollRun.CompanyId == run.CompanyId
                             && pe.Pay_PayrollRun.PayrollPeriod == run.PayrollPeriod
                             && pe.Pay_PayrollRun.RunType == PayrollRunType.Regular
                             && pe.Pay_PayrollRun.Status >= PayrollRunStatus.Approved
                             && pe.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled)
                .ToDictionaryAsync(pe => pe.HremployeeId, ct);
        }

        // Phase B: employees HR placed on hold for THIS run (data not ready, dispute,
        // documents pending) are skipped here and calculated in a later run.
        var heldEmployeeIds = await context.Pay_PayrollRunHolds
            .Where(h => h.PayrollRunId == payrollRunId && h.IsActive)
            .Select(h => h.HremployeeId)
            .ToListAsync(ct);
        if (heldEmployeeIds.Count > 0)
            eligibleEmployees = eligibleEmployees.Where(e => !heldEmployeeIds.Contains(e.id)).ToList();

        var pfElections = await context.Pay_ProvidentFundElections
            .Where(pe => pe.IsActive
                         && pe.EffectiveFrom <= run.PeriodEnd
                         && (pe.EffectiveTo == null || pe.EffectiveTo >= run.PeriodStart))
            .ToListAsync(ct);

        var insuranceEnrollments = await context.Pay_EmployeeInsuranceEnrollments
            .Where(e => e.IsActive
                        && e.EffectiveFrom <= run.PeriodEnd
                        && (e.EffectiveTo == null || e.EffectiveTo >= run.PeriodStart))
            .ToListAsync(ct);

        // Company-wide (not per-employee) — resolves to at most one active,
        // enabled rate tier for this run's period. Every seeded policy row
        // ships with IsEnabled=false (see Pay_WelfareFundPolicy.cs), so this
        // is a no-op for every company until HR explicitly turns a tier on
        // via WelfareFundPolicyAdmin.razor.
        var welfareFundPolicy = await context.Pay_WelfareFundPolicies
            .Where(p => p.CompanyId == run.CompanyId
                        && p.IsEnabled
                        && p.EffectiveFrom <= run.PeriodEnd
                        && (p.EffectiveTo == null || p.EffectiveTo >= run.PeriodStart))
            .FirstOrDefaultAsync(ct);

        // Same company-wide resolution as welfareFundPolicy above. Per
        // มาตรา 130 พ.ร.บ.คุ้มครองแรงงาน, an employer with an active provident
        // fund is exempt from the mandatory Employee Welfare Fund — used
        // below to suppress the welfare fund deduction entirely when this
        // is present, rather than running both side by side.
        var providentFundPolicy = await context.Pay_ProvidentFundPolicies
            .Where(p => p.CompanyId == run.CompanyId
                        && p.IsEnabled
                        && p.EffectiveFrom <= run.PeriodEnd
                        && (p.EffectiveTo == null || p.EffectiveTo >= run.PeriodStart))
            .FirstOrDefaultAsync(ct);

        // assumes 12 monthly runs/year; remaining periods including this one
        var remainingPeriods = 13 - run.PeriodStart.Month;

        // Welfare monthly allowances (จ่ายประจำเข้า payroll — เช่น ค่ารถ): the
        // benefit types in MonthlyAllowance mode, their per-person override rules,
        // an Id-keyed pay-item-type map, and each employee's position — so the
        // resolver's pure Pick can give each person their own amount inside the
        // loop without opening a nested DbContext.
        var monthlyAllowanceBenefits = await context.Wel_BenefitTypes
            .Where(b => b.CompanyId == run.CompanyId && b.IsActive && b.EntitlementMode == WelfareEntitlementMode.MonthlyAllowance)
            .ToListAsync(ct);
        var allowanceBenefitIds = monthlyAllowanceBenefits.Select(b => b.Id).ToList();
        var allowanceRulesByBenefit = (allowanceBenefitIds.Count == 0
                ? new List<Wel_Entitlement>()
                : await context.Wel_Entitlements.Where(r => r.IsActive && allowanceBenefitIds.Contains(r.BenefitTypeId)).ToListAsync(ct))
            .GroupBy(r => r.BenefitTypeId).ToDictionary(g => g.Key, g => g.ToList());
        var payItemTypesById = payItemTypes.Values.ToDictionary(t => t.Id);
        var empPosExecTypes = allowanceBenefitIds.Count == 0
            ? new Dictionary<long, long>()
            : await context.Pos_PositionSlots
                .Where(s => s.IsActive && s.HremployeeId != null && s.PosExecTypeId != null)
                .GroupBy(s => s.HremployeeId!.Value)
                .Select(g => new { Emp = g.Key, Pos = g.Min(x => x.PosExecTypeId!.Value) })
                .ToDictionaryAsync(x => x.Emp, x => x.Pos, ct);

        var negativeCount = 0;
        var totalNet = 0m;

        // Progress reporting (Phase A): announce the total up front, then
        // report every ~1% of employees so the progress bar moves smoothly
        // without a report per row.
        var progressTotal = eligibleEmployees.Count;
        var progressEvery = Math.Max(1, progressTotal / 100);
        var progressDone = 0;
        progress?.Report((0, progressTotal));

        foreach (var emp in eligibleEmployees)
        {
            if (progressDone > 0 && (progressDone % progressEvery == 0))
                progress?.Report((progressDone, progressTotal));
            progressDone++;

            var proration = ProrationCalculator.Calculate(
                run.PeriodStart, run.PeriodEnd,
                emp.WorkDate.HasValue ? DateOnly.FromDateTime(emp.WorkDate.Value) : null,
                emp.ResignDate.HasValue ? DateOnly.FromDateTime(emp.ResignDate.Value) : null);

            var payEmp = new Pay_PayrollEmployee
            {
                PayrollRunId = run.Id,
                HremployeeId = emp.id,
                EmpNo = emp.EmpNo,
                CompanyId = emp.companyid,
                ProrationFactor = proration.ProrationFactor,
                WorkingDaysInPeriod = proration.WorkingDaysInPeriod,
                ActualWorkingDays = proration.ActualWorkingDays,
                BankCode = emp.SalexpBank,
                BankBranchCode = emp.SalexpBranch,
                BankAccountNo = emp.SalexpAccid,
                CostCenterCode = emp.CostCenterCode,
            };

            var lineItems = new List<Pay_PayrollLineItem>();
            var seq = 0;

            var empAttendance = attendanceByEmployee.TryGetValue(emp.id, out var attRows) ? attRows : null;

            // Daily-wage employees (DAILY_WAGE set, no monthly salary) are paid per
            // day instead of a pro-rated monthly amount. Which days count is policy:
            // calendar days in the period, or attended days once time clocks exist.
            var isDailyWage = (emp.DailyWage ?? 0m) > 0m && (emp.SalaryAmt ?? 0m) <= 0m;
            decimal baseSalary;
            if (supplementary)
            {
                baseSalary = 0m;   // รอบเสริมไม่มีเงินเดือน — จ่ายเฉพาะรายการเฉพาะกิจ
            }
            else if (isDailyWage)
            {
                var useAttendance = attendancePolicy?.DailyWageMode == PayDailyWageDaysMode.AttendanceDays && empAttendance is { Count: > 0 };
                var paidDays = useAttendance ? empAttendance!.Count(a => !a.IsAbsent) : proration.ActualWorkingDays;
                baseSalary = Math.Round(emp.DailyWage!.Value * paidDays, 2, MidpointRounding.AwayFromZero);
                lineItems.Add(NewLine(payItemTypes["BASE"], PayLineSourceType.Base, baseSalary, 1, ++seq, "HREMPLOYEE", emp.id,
                    $"ค่าจ้างรายวัน {emp.DailyWage.Value:N2} × {paidDays} วัน ({(useAttendance ? "วันที่มีการลงเวลา" : "วันตามปฏิทินในงวด")}) = {baseSalary:N2}"));
            }
            else
            {
                baseSalary = Math.Round((emp.SalaryAmt ?? 0m) * proration.ProrationFactor, 2, MidpointRounding.AwayFromZero);
                lineItems.Add(NewLine(payItemTypes["BASE"], PayLineSourceType.Base, baseSalary, 1, ++seq, "HREMPLOYEE", emp.id,
                    $"ฐานเงินเดือน {(emp.SalaryAmt ?? 0m):N2} × สัดส่วนวันทำงาน {proration.ActualWorkingDays}/{proration.WorkingDaysInPeriod} วัน ({proration.ProrationFactor:P2}) = {baseSalary:N2}"));
            }

            // Late / absence deductions from Att_DailyAttendance per policy. Monthly
            // staff only — a daily-wage employee's absent day is simply not paid above.
            var attendanceDeduction = 0m;
            if (!supplementary && !isDailyWage && attendancePolicy is not null && empAttendance is { Count: > 0 })
            {
                var monthly = emp.SalaryAmt ?? 0m;
                var dailyRate = attendancePolicy.DaysPerMonthDivisor > 0 ? monthly / attendancePolicy.DaysPerMonthDivisor : 0m;
                var hourlyRate = attendancePolicy.HoursPerDay > 0 ? dailyRate / attendancePolicy.HoursPerDay : 0m;

                if (attendancePolicy.LateMode != PayLateDeductionMode.None)
                {
                    var lateDays = empAttendance.Where(a => !a.IsAbsent && a.LateMinutes > attendancePolicy.LateGraceMinutes).ToList();
                    if (lateDays.Count > 0)
                    {
                        decimal lateAmount; string lateNote;
                        if (attendancePolicy.LateMode == PayLateDeductionMode.PerMinute)
                        {
                            var minutes = lateDays.Sum(a => a.LateMinutes - attendancePolicy.LateGraceMinutes);
                            var perMinute = attendancePolicy.LateAmountPerMinute ?? Math.Round(hourlyRate / 60m, 4);
                            lateAmount = Math.Round(minutes * perMinute, 2, MidpointRounding.AwayFromZero);
                            lateNote = $"มาสาย {lateDays.Count} วัน รวม {minutes} นาที (หลังหักผ่อนผัน {attendancePolicy.LateGraceMinutes} นาที/วัน) × {perMinute:N4} บาท/นาที = {lateAmount:N2}";
                        }
                        else
                        {
                            lateAmount = Math.Round(lateDays.Count * attendancePolicy.LateAmountPerOccurrence, 2, MidpointRounding.AwayFromZero);
                            lateNote = $"มาสาย {lateDays.Count} วัน × {attendancePolicy.LateAmountPerOccurrence:N2} บาท/ครั้ง = {lateAmount:N2}";
                        }
                        if (lateAmount > 0)
                        {
                            lineItems.Add(NewLine(payItemTypes["LATE"], PayLineSourceType.Adjustment, lateAmount, -1, ++seq, "Att_DailyAttendance", null, lateNote));
                            attendanceDeduction += lateAmount;
                        }
                    }
                }

                if (attendancePolicy.AbsentMode == PayAbsentDeductionMode.DailyRate)
                {
                    var absentDays = empAttendance.Count(a => a.IsAbsent);
                    if (absentDays > 0)
                    {
                        var absentAmount = Math.Round(absentDays * dailyRate, 2, MidpointRounding.AwayFromZero);
                        lineItems.Add(NewLine(payItemTypes["ABSENT"], PayLineSourceType.Adjustment, absentAmount, -1, ++seq, "Att_DailyAttendance", null,
                            $"ขาดงาน {absentDays} วัน × ค่าจ้างรายวัน {dailyRate:N2} (เงินเดือน {monthly:N2} ÷ {attendancePolicy.DaysPerMonthDivisor}) = {absentAmount:N2}"));
                        attendanceDeduction += absentAmount;
                    }
                }
                // Wages not earned can never exceed the wages of the period.
                attendanceDeduction = Math.Min(attendanceDeduction, baseSalary);
            }

            var otAmount = 0m;
            if (!supplementary)
            {
                var otRecords = await _overtimeCalculator.GetOvertimeForPeriodAsync(emp.companyid, emp.EmpNo, run.PeriodStart, run.PeriodEnd, ct);
                otAmount = OvertimeEarningsCalculator.SumAmount(otRecords);
                if (otAmount != 0)
                    lineItems.Add(NewLine(payItemTypes["OT"], PayLineSourceType.Overtime, otAmount, 1, ++seq, "HRW_OT", null,
                        $"รวมค่าล่วงเวลาจากรายการที่บันทึกไว้ {otRecords.Count} รายการในงวดนี้ = {otAmount:N2}"));
            }

            // Welfare monthly allowances — per-person amount via the resolver's
            // pure Pick (company default / position / individual). Emitted as
            // earning lines sourced from Wel_BenefitType.
            decimal welfareAllowanceTotal = 0m, welfareTaxableAllowance = 0m, ssoWageBaseAllowance = 0m;
            if (!supplementary && monthlyAllowanceBenefits.Count > 0)
            {
                long? empPos = empPosExecTypes.TryGetValue(emp.id, out var pv) ? pv : null;
                foreach (var wb in monthlyAllowanceBenefits)
                {
                    var rules = allowanceRulesByBenefit.TryGetValue(wb.Id, out var rs)
                        ? (IEnumerable<Wel_Entitlement>)rs : Array.Empty<Wel_Entitlement>();
                    var amt = HRM.Services.Welfare.WelfareEntitlementResolver.Pick(wb, rules, empPos, emp.id).Amount ?? 0m;
                    if (amt <= 0) continue;
                    var itemType = wb.PayItemTypeId is int pid && payItemTypesById.TryGetValue(pid, out var t) ? t : payItemTypes["ALLOWANCE"];
                    // Pay Element flag: a pro-rated element is scaled by the same working-day
                    // factor as base salary (a mid-month joiner gets a partial allowance).
                    var prorateNote = "";
                    if (itemType.IsProrated && proration.ProrationFactor != 1m)
                    {
                        amt = Math.Round(amt * proration.ProrationFactor, 2, MidpointRounding.AwayFromZero);
                        prorateNote = $" × สัดส่วนวันทำงาน {proration.ProrationFactor:P2}";
                    }
                    lineItems.Add(NewLine(itemType, PayLineSourceType.Allowance, amt, 1, ++seq, "Wel_BenefitType", wb.Id,
                        $"สวัสดิการจ่ายประจำ {wb.NameTh}{prorateNote} = {amt:N2}"));
                    welfareAllowanceTotal += amt;
                    if (wb.IsTaxable) welfareTaxableAllowance += amt;
                    if (itemType.IsSsoWageBase) ssoWageBaseAllowance += amt;
                }
            }

            var grossEarnings = baseSalary + otAmount + welfareAllowanceTotal;

            // Social-security wage base follows the Pay Element catalog flags: only
            // elements marked IsSsoWageBase count (base salary + regular allowances by
            // default; OT and one-off items are excluded, as Thai SSO defines ค่าจ้าง).
            var ssoWageBase = (payItemTypes["BASE"].IsSsoWageBase ? baseSalary - attendanceDeduction : 0m)
                            + (payItemTypes["OT"].IsSsoWageBase ? otAmount : 0m)
                            + ssoWageBaseAllowance;
            var ssoAmount = SocialSecurityCalculator.Calculate(ssoWageBase, ssoRate, ssoCap);
            if (ssoAmount != 0)
                lineItems.Add(NewLine(payItemTypes["SSO"], PayLineSourceType.SocialSecurity, ssoAmount, -1, ++seq, null, null,
                    $"{ssoRate:0.##}% ของฐานค่าจ้างประกันสังคม {ssoWageBase:N2} (เฉพาะรายการที่ตั้งธง \"ฐาน SSO\" ในแค็ตตาล็อก; เพดาน {ssoCap:N2}) = {ssoAmount:N2}"));

            var election = pfElections.FirstOrDefault(pe => pe.HremployeeId == emp.id);
            var pfEmployeeRate = election?.EmployeeContributionRate ?? emp.ProvfEmprate ?? 0m;
            var pfCompanyRate = election?.CompanyContributionRate ?? emp.ProvfCorprate ?? 0m;
            var pf = ProvidentFundCalculator.Calculate(grossEarnings, pfEmployeeRate, pfCompanyRate);
            if (pf.EmployeeAmount != 0)
                lineItems.Add(NewLine(payItemTypes["PF"], PayLineSourceType.ProvidentFund, pf.EmployeeAmount, -1, ++seq, "Pay_ProvidentFundElection", election?.Id,
                    $"อัตราสะสมพนักงาน {pfEmployeeRate:0.##}% × เงินได้ {grossEarnings:N2} = {pf.EmployeeAmount:N2} (บริษัทสมทบ {pfCompanyRate:0.##}% = {pf.CompanyAmount:N2})"));

            var empInsuranceEnrollments = supplementary
                ? new List<Pay_EmployeeInsuranceEnrollment>()   // เบี้ยประกันหักในรอบปกติแล้ว
                : insuranceEnrollments.Where(e => e.HremployeeId == emp.id).ToList();
            var insuranceEmployeeAmount = empInsuranceEnrollments.Sum(e => e.EmployeeAmount);
            var insuranceCompanyAmount = empInsuranceEnrollments.Sum(e => e.CompanyAmount);
            if (insuranceEmployeeAmount != 0)
                lineItems.Add(NewLine(payItemTypes["INSURANCE"], PayLineSourceType.Insurance, insuranceEmployeeAmount, -1, ++seq, "Pay_EmployeeInsuranceEnrollment", null,
                    $"รวมเบี้ยประกันกลุ่มที่พนักงานสมทบจาก {empInsuranceEnrollments.Count} กรมธรรม์ = {insuranceEmployeeAmount:N2} (บริษัทสมทบ {insuranceCompanyAmount:N2})"));

            var welfareFundEmployeeAmount = 0m;
            var welfareFundCompanyAmount = 0m;
            // มาตรา 130: an active provident fund exempts the company from
            // the mandatory welfare fund — skip entirely rather than
            // stacking both deductions.
            if (welfareFundPolicy is not null && providentFundPolicy is null)
            {
                var wf = WelfareFundCalculator.Calculate(grossEarnings, welfareFundPolicy.EmployeeContributionRate, welfareFundPolicy.CompanyContributionRate, welfareFundPolicy.WageCapPerMonth);
                welfareFundEmployeeAmount = wf.EmployeeAmount;
                welfareFundCompanyAmount = wf.CompanyAmount;
                if (welfareFundEmployeeAmount != 0)
                    lineItems.Add(NewLine(payItemTypes["WELFAREFUND"], PayLineSourceType.WelfareFund, welfareFundEmployeeAmount, -1, ++seq, "Pay_WelfareFundPolicy", welfareFundPolicy.Id,
                        $"อัตราสะสมพนักงาน {welfareFundPolicy.EmployeeContributionRate:0.##}% ของเงินได้ {grossEarnings:N2} (เพดาน {welfareFundPolicy.WageCapPerMonth?.ToString("N2") ?? "ไม่กำหนด"}) = {welfareFundEmployeeAmount:N2}"));
            }

            var loanAmount = 0m;
            if (!supplementary && !string.IsNullOrWhiteSpace(emp.RefMembno))
            {
                var loanDetails = await _loanCalculator.GetLoanDeductionsForPeriodAsync(emp.companyid, emp.RefMembno, run.PayrollPeriod, ct);
                loanAmount = LoanDeductionCalculator.SumAmount(loanDetails);
                if (loanAmount != 0)
                    lineItems.Add(NewLine(payItemTypes["LOAN"], PayLineSourceType.Loan, loanAmount, -1, ++seq, "KPTEMPRECEIVEDET", null,
                        $"หักเงินกู้สหกรณ์ตามรายการที่บันทึกไว้ (KPTEMPRECEIVEDET) ในงวดนี้ = {loanAmount:N2}"));
            }

            // HR-entered company loans (Pay_EmployeeLoan) — separate pathway
            // from the cooperative KPTEMPRECEIVE loan above; an employee
            // could have both types of deduction in the same period.
            var empLoanInstallments = supplementary
                ? new List<Pay_EmployeeLoanInstallment>()   // งวดผ่อนหักในรอบปกติแล้ว
                : await LoanDeductionCalculator.GetEmployeeLoanInstallmentsForPeriodAsync(context, emp.id, run.PayrollPeriod, run.Id, ct);
            foreach (var installment in empLoanInstallments)
            {
                lineItems.Add(NewLine(payItemTypes["LOAN"], PayLineSourceType.Loan, installment.Amount, -1, ++seq, "Pay_EmployeeLoanInstallment", installment.Id,
                    $"งวดผ่อนที่ {installment.InstallmentNo}/{installment.Pay_EmployeeLoan.TotalInstallments} ของเงินกู้ = {installment.Amount:N2} (คงเหลือหลังหัก {installment.BalanceAfter:N2})"));
                installment.Status = Pay_LoanInstallmentStatus.Consumed;
                installment.ConsumedByPayrollRunId = run.Id;
                installment.Pay_EmployeeLoan.RemainingBalance = installment.BalanceAfter;
                if (installment.InstallmentNo == installment.Pay_EmployeeLoan.TotalInstallments)
                    installment.Pay_EmployeeLoan.Status = Pay_EmployeeLoanStatus.PaidOff;
            }
            loanAmount += LoanDeductionCalculator.SumEmployeeLoanAmount(empLoanInstallments);

            // HR-entered ad-hoc items (bonus, commission, ad-hoc deduction, etc.)
            // approved and targeting this exact period. Query includes items
            // already consumed by THIS run so recalculation re-picks them up
            // idempotently rather than losing them.
            var adhocItems = await context.Pay_AdhocPayItems
                .Include(a => a.Pay_PayItemType)
                .Where(a => a.HremployeeId == emp.id
                            && a.TargetPeriod == run.PayrollPeriod
                            && (a.Status == PayAdhocItemStatus.Approved
                                || (a.Status == PayAdhocItemStatus.Consumed && a.ConsumedByPayrollRunId == run.Id)))
                .ToListAsync(ct);

            var adhocTaxableEarnings = 0m;
            var adhocNonTaxableEarnings = 0m;
            var adhocDeductions = 0m;
            foreach (var adhoc in adhocItems)
            {
                var signFlag = adhoc.Pay_PayItemType.DefaultSignFlag;
                lineItems.Add(NewLine(adhoc.Pay_PayItemType, PayLineSourceType.Adjustment, adhoc.Amount, signFlag, ++seq, "Pay_AdhocPayItem", adhoc.Id,
                    $"รายการเฉพาะกิจที่ HR อนุมัติ: {adhoc.Reason}"));

                if (signFlag > 0)
                {
                    if (adhoc.IsTaxable) adhocTaxableEarnings += adhoc.Amount;
                    else adhocNonTaxableEarnings += adhoc.Amount;
                }
                else
                {
                    adhocDeductions += adhoc.Amount;
                }

                adhoc.Status = PayAdhocItemStatus.Consumed;
                adhoc.ConsumedByPayrollRunId = run.Id;
            }

            grossEarnings += adhocTaxableEarnings + adhocNonTaxableEarnings;
            // Late/absence deductions are wages not earned, so they reduce taxable
            // income (and the SSO base above) rather than being after-tax deductions.
            var taxableGrossThisPeriod = Math.Max(0m, baseSalary - attendanceDeduction) + otAmount + adhocTaxableEarnings + welfareTaxableAllowance;

            var empMonthlyElections = monthlyTaxElections.Where(e => e.HremployeeId == emp.id).ToList();
            var electedMonthlyDeduction = empMonthlyElections.Sum(e => e.AnnualAmount) / 12m;
            // รอบเสริมไม่หักค่าลดหย่อนซ้ำ — ค่าลดหย่อนของเดือนนี้ใช้ไปในรอบปกติแล้ว
            var thisPeriodFlatDeduction = supplementary ? 0m
                : personalAllowancePerMonth + ssoAmount + pf.EmployeeAmount + electedMonthlyDeduction;

            var priorEmployerIncome = priorEmployerIncomes.FirstOrDefault(p => p.HremployeeId == emp.id);
            var (ytdIncome, ytdDeduction, ytdTax) = await GetYtdAccumulatorsAsync(context, emp.id, run, priorEmployerIncome, ct, includeSamePeriod: supplementary);
            decimal monthlyTax;
            TaxBracketCalculator.TaxCalculationResult annualCalc;
            if (supplementary)
            {
                // ภาษีโบนัสแบบส่วนต่าง: ฐานประมาณการทั้งปี = สะสม (รวมงวดนี้แล้ว) + เงินเดือนงวดนี้ × เดือนที่เหลือ
                regularRowsThisPeriod.TryGetValue(emp.id, out var regularRow);
                (monthlyTax, annualCalc) = TaxBracketCalculator.CalculateBonusWithholding(
                    ytdIncome, ytdDeduction, ytdTax,
                    regularRow?.GrossEarnings ?? 0m, regularRow?.TaxDeductionAmount ?? 0m,
                    taxableGrossThisPeriod, 12 - run.PeriodStart.Month,
                    expenseDeductionRate, expenseDeductionCap, taxBrackets);
            }
            else
            {
                (monthlyTax, annualCalc) = TaxBracketCalculator.CalculateMonthlyWithholding(
                    ytdIncome, taxableGrossThisPeriod, ytdDeduction, thisPeriodFlatDeduction,
                    expenseDeductionRate, expenseDeductionCap, remainingPeriods, ytdTax, taxBrackets);
            }
            if (monthlyTax != 0)
                lineItems.Add(NewLine(payItemTypes["TAX"], PayLineSourceType.Tax, monthlyTax, -1, ++seq, null, null,
                    "ภาษีหัก ณ ที่จ่ายประจำเดือน คำนวณจากเงินได้สะสมทั้งปีเทียบตารางอัตราภาษี — ดูรายละเอียดฉบับเต็มในหัวข้อ \"บันทึกการคำนวณภาษี\" ด้านล่าง"));

            // Salary advances targeted at this period are recovered in full (regular runs only).
            var advanceAmount = 0m;
            foreach (var adv in supplementary ? Enumerable.Empty<Pay_SalaryAdvance>() : advances.Where(a => a.HremployeeId == emp.id))
            {
                lineItems.Add(NewLine(payItemTypes["SAL_ADVANCE"], PayLineSourceType.Adjustment, adv.Amount, -1, ++seq, "Pay_SalaryAdvance", adv.Id,
                    $"หักคืนเงินเบิกล่วงหน้า {adv.Amount:N2} ({adv.Reason}; อนุมัติ {adv.ApprovedDate:dd/MM/yyyy})"));
                advanceAmount += adv.Amount;
                adv.Status = PaySalaryAdvanceStatus.Deducted;
                adv.ConsumedByPayrollRunId = run.Id;
            }

            var totalDeductions = ssoAmount + pf.EmployeeAmount + insuranceEmployeeAmount + welfareFundEmployeeAmount + loanAmount + adhocDeductions + monthlyTax
                                  + attendanceDeduction + advanceAmount;
            var netPayResult = NetPayGuardService.Ensure(grossEarnings - totalDeductions);

            payEmp.GrossEarnings = grossEarnings;
            payEmp.TotalDeductions = totalDeductions;
            payEmp.NetPay = netPayResult.AdjustedNetPay;
            payEmp.TaxAmount = monthlyTax;
            payEmp.TaxDeductionAmount = thisPeriodFlatDeduction;
            payEmp.SocialSecurityAmount = ssoAmount;
            payEmp.ProvidentFundEmployeeAmount = pf.EmployeeAmount;
            payEmp.ProvidentFundCompanyAmount = pf.CompanyAmount;
            payEmp.InsuranceEmployeeAmount = insuranceEmployeeAmount;
            payEmp.InsuranceCompanyAmount = insuranceCompanyAmount;
            payEmp.WelfareFundEmployeeAmount = welfareFundEmployeeAmount;
            payEmp.WelfareFundCompanyAmount = welfareFundCompanyAmount;
            payEmp.IsNegativeNetPayFlag = netPayResult.WasNegative;
            payEmp.Pay_PayrollLineItems = lineItems;

            context.Pay_PayrollEmployees.Add(payEmp);

            context.Pay_PayrollAuditLogs.Add(new Pay_PayrollAuditLog
            {
                PayrollRunId = run.Id,
                Pay_PayrollEmployee = payEmp, // navigation, not PayrollEmployeeId: payEmp.Id isn't assigned until SaveChanges
                EventType = PayAuditEventType.TaxCalculationDetail,
                ActorUserId = actorUserId,
                DetailJson = JsonSerializer.Serialize(new
                {
                    emp.EmpNo,
                    GrossEarnings = grossEarnings,
                    YtdIncomeBeforeThisPeriod = ytdIncome,
                    YtdDeductionBeforeThisPeriod = ytdDeduction,
                    RemainingPeriods = remainingPeriods,
                    PriorEmployerIncomeIncluded = priorEmployerIncome is null ? null : new
                    {
                        priorEmployerIncome.PriorEmployerName,
                        priorEmployerIncome.IncomeAmount,
                        priorEmployerIncome.DeductionAmount,
                        priorEmployerIncome.TaxWithheldAmount,
                    },
                    DeductionBreakdown = new
                    {
                        PersonalAllowancePerMonth = personalAllowancePerMonth,
                        SocialSecurity = ssoAmount,
                        ProvidentFund = pf.EmployeeAmount,
                        ElectedMonthlyDeductions = electedMonthlyDeduction,
                        ThisPeriodFlatDeductionTotal = thisPeriodFlatDeduction,
                        ExpenseDeductionRate = expenseDeductionRate,
                        ExpenseDeductionCap = expenseDeductionCap,
                    },
                    AnnualCalculation = annualCalc,
                    MonthlyWithholding = monthlyTax,
                }),
            });

            if (netPayResult.WasNegative) negativeCount++;
            totalNet += netPayResult.AdjustedNetPay;
        }

        var fromStatus = run.Status;
        run.Status = PayrollRunStatus.Calculated;
        run.CalculatedByUserId = actorUserId;
        run.CalculatedDate = DateTime.Now;

        context.Pay_PayrollAuditLogs.Add(new Pay_PayrollAuditLog
        {
            PayrollRunId = run.Id,
            EventType = PayAuditEventType.StatusTransition,
            FromStatus = fromStatus,
            ToStatus = PayrollRunStatus.Calculated,
            ActorUserId = actorUserId,
        });

        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Progress reporting: the loop throttles every ~1%, so the last report
        // lands just short of 100% — announce the true total here, before the
        // (possibly slow) anomaly pass, so the bar reaches 100% instead of
        // stalling at 99.x% while ML.NET runs.
        progress?.Report((progressTotal, progressTotal));

        // Best-effort — anomaly detection is purely advisory and must never
        // stop a payroll run from being calculated. If ML.NET or a query in
        // here throws, log it and let the calculation stand. The background job
        // (PayrollCalcJobService) passes runAnomalyDetection:false and runs this
        // pass itself AFTER signalling Done, so the 2-3 min ML tail no longer
        // sits on the critical path of a 7,000-employee run.
        if (runAnomalyDetection)
        {
            try
            {
                await _anomalyDetectionService.DetectAnomaliesAsync(payrollRunId, ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Anomaly detection failed for payroll run {PayrollRunId}", payrollRunId);
            }
        }

        return new PayrollRunCalculationSummary(eligibleEmployees.Count, negativeCount, totalNet);
    }

    private static Pay_PayrollLineItem NewLine(Pay_PayItemType itemType, PayLineSourceType sourceType, decimal amount, int signFlag, int seq, string? sourceRefTable, long? sourceRefId, string? description = null)
    {
        return new Pay_PayrollLineItem
        {
            PayItemTypeId = itemType.Id,
            SourceType = sourceType,
            SourceRefTable = sourceRefTable,
            SourceRefId = sourceRefId,
            Amount = amount,
            SignFlag = signFlag,
            SeqNo = seq,
            Description = description,
        };
    }

    // YTD figures are derived from previously-calculated Pay_PayrollEmployee rows
    // in the same calendar year, plus (for a mid-year hire) whatever prior-
    // employer income HR entered at Pay/admin/prior-employer-income — never a
    // separate running-accumulator table.
    // ยอดสะสมนับเฉพาะรอบที่ "อนุมัติแล้ว" (audit H3) — รอบที่ยังแก้ได้ไม่ใช่ข้อเท็จจริง
    // includeSamePeriod = รอบเสริม (โบนัส) ต้องนับรอบปกติของงวดเดียวกันด้วย
    private static async Task<(decimal YtdIncome, decimal YtdDeduction, decimal YtdTax)> GetYtdAccumulatorsAsync(
        HRMContext context, long hremployeeId, Pay_PayrollRun run, Pay_EmployeePriorEmployerIncome? priorEmployerIncome,
        CancellationToken ct, bool includeSamePeriod = false)
    {
        var yearStart = new DateOnly(run.PeriodStart.Year, 1, 1);

        var priorRows = await context.Pay_PayrollEmployees
            .Include(e => e.Pay_PayrollRun)
            .Where(e => e.HremployeeId == hremployeeId
                        && e.PayrollRunId != run.Id
                        && e.Pay_PayrollRun.PeriodStart >= yearStart
                        && (includeSamePeriod ? e.Pay_PayrollRun.PeriodStart <= run.PeriodStart
                                              : e.Pay_PayrollRun.PeriodStart < run.PeriodStart)
                        && e.Pay_PayrollRun.Status >= PayrollRunStatus.Approved
                        && e.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled)
            .ToListAsync(ct);

        return FoldPriorEmployerIncome(
            priorRows.Sum(r => r.GrossEarnings), priorRows.Sum(r => r.TaxDeductionAmount), priorRows.Sum(r => r.TaxAmount),
            priorEmployerIncome);
    }

    // Pure and unit-testable on purpose (mirrors TaxBracketCalculator's own
    // separation of math from EF orchestration) — folds a mid-year hire's
    // prior-employer income/deduction/tax-withheld (from
    // Pay_EmployeePriorEmployerIncome, entered once from the certificate the
    // employee brings in) into this company's own YTD accumulators, so the
    // withholding projection reflects the employee's TRUE annual income.
    public static (decimal YtdIncome, decimal YtdDeduction, decimal YtdTax) FoldPriorEmployerIncome(
        decimal ytdIncome, decimal ytdDeduction, decimal ytdTax, Pay_EmployeePriorEmployerIncome? prior)
        => prior is null
            ? (ytdIncome, ytdDeduction, ytdTax)
            : (ytdIncome + prior.IncomeAmount, ytdDeduction + prior.DeductionAmount, ytdTax + prior.TaxWithheldAmount);
}
