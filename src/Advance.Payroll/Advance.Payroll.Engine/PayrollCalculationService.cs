namespace Advance.Payroll.Engine;

using System.Text.Json;
using Advance.Payroll.Contracts;
using Advance.Payroll.Core;
using Advance.Payroll.Data;
using Advance.Payroll.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record PayrollRunCalculationSummary(int EmployeeCount, int NegativeNetPayCount, decimal TotalNetPay);

// Ported from HRM Services/Pay/PayrollCalculationService.cs (966 lines) — the flagship
// Phase 0 adaptation. Logic is UNCHANGED line-for-line; every external-module read is
// replaced with a call through one of the 7 Advance.Payroll.Contracts interfaces:
//
//   Hremployee (eligible-employee query + every emp.<Column> read)  -> IEmployeeSource
//   Att_DailyAttendance                                             -> IAttendanceFeed
//   HrwOt (via OvertimeEarningsCalculator)                          -> IOvertimeFeed
//   Kptempreceive/Kptempreceivedet (via LoanDeductionCalculator)    -> ILoanFeed
//   Wel_BenefitType/Wel_Entitlement/Pos_PositionSlot                -> IAllowanceFeed
//   Lve_CompanySetting/Lve_CompanyHoliday                           -> IHolidayCalendarSource
//   HRM.Services.Leave.LeaveDayCalculator.CalculateWorkingDays      -> Core.WorkingDaysCalculator (pure, re-implemented, see that file)
//
// No TODO(seam) markers remain in this file — every dependency this service used is now
// resolved. (Contrast with PayrollAnomalyDetectionService.cs, which still has one open
// seam: the onboarding-checklist check reads Hrd_LifecycleTaskInstance, an HRM-only
// concept with no equivalent in the 7-interface list — see that file and
// EXTRACTION-PLAN.md "seams beyond the original 7".)
public class PayrollCalculationService
{
    private readonly IDbContextFactory<PayrollDbContext> _dbFactory;
    private readonly ISocialSecurityRateProvider _socialSecurityRateProvider;
    private readonly IEmployeeSource _employeeSource;
    private readonly IAttendanceFeed _attendanceFeed;
    private readonly IOvertimeFeed _overtimeFeed;
    private readonly ILoanFeed _loanFeed;
    private readonly IAllowanceFeed _allowanceFeed;
    private readonly IHolidayCalendarSource _holidayCalendarSource;
    private readonly PayrollAnomalyDetectionService _anomalyDetectionService;
    private readonly ILogger<PayrollCalculationService> _logger;

    public PayrollCalculationService(
        IDbContextFactory<PayrollDbContext> dbFactory,
        ISocialSecurityRateProvider socialSecurityRateProvider,
        IEmployeeSource employeeSource,
        IAttendanceFeed attendanceFeed,
        IOvertimeFeed overtimeFeed,
        ILoanFeed loanFeed,
        IAllowanceFeed allowanceFeed,
        IHolidayCalendarSource holidayCalendarSource,
        PayrollAnomalyDetectionService anomalyDetectionService,
        ILogger<PayrollCalculationService> logger)
    {
        _dbFactory = dbFactory;
        _socialSecurityRateProvider = socialSecurityRateProvider;
        _employeeSource = employeeSource;
        _attendanceFeed = attendanceFeed;
        _overtimeFeed = overtimeFeed;
        _loanFeed = loanFeed;
        _allowanceFeed = allowanceFeed;
        _holidayCalendarSource = holidayCalendarSource;
        _anomalyDetectionService = anomalyDetectionService;
        _logger = logger;
    }

    public async Task<PayrollRunCalculationSummary> CalculateAsync(long payrollRunId, long actorUserId, IProgress<(int Done, int Total)>? progress = null, CancellationToken ct = default, bool runAnomalyDetection = true)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(15));

        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == payrollRunId, ct)
            ?? throw new InvalidOperationException($"Pay_PayrollRun {payrollRunId} not found.");

        if (run.Status != PayrollRunStatus.Draft && run.Status != PayrollRunStatus.Calculated)
            throw new InvalidOperationException($"Cannot calculate a run in status {run.Status}. Only Draft or Calculated runs can be (re)calculated.");

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
        if (run.RunType == PayrollRunType.Regular && approvedLater.Count > 0)
            throw new InvalidOperationException(
                $"งวด {string.Join(", ", approvedLater)} อนุมัติไปแล้วโดยใช้ยอดสะสมจากงวดนี้ — คำนวณงวดนี้ใหม่ไม่ได้ ถ้าต้องแก้ให้กลับรายการงวดหลังก่อน");
        if (supplementary)
        {
            var regularApproved = await context.Pay_PayrollRuns.AnyAsync(r =>
                r.CompanyId == run.CompanyId && r.PayrollPeriod == run.PayrollPeriod
                && r.RunType == PayrollRunType.Regular && r.Status >= PayrollRunStatus.Approved
                && r.Status != PayrollRunStatus.Cancelled, ct);
            if (!regularApproved)
                throw new InvalidOperationException($"รอบโบนัสของงวด {run.PayrollPeriod} คำนวณได้หลังรอบปกติของงวดเดียวกันอนุมัติแล้ว");
        }

        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var existingEmployeeIds = await context.Pay_PayrollEmployees
            .Where(e => e.PayrollRunId == payrollRunId)
            .Select(e => e.Id)
            .ToListAsync(ct);

        var carriedExclusions = await context.Pay_PayrollEmployees
            .Where(e => e.PayrollRunId == payrollRunId && e.IsExcluded)
            .Select(e => new { e.HremployeeId, e.ExcludeReason })
            .ToDictionaryAsync(e => e.HremployeeId, e => e.ExcludeReason, ct);

        if (existingEmployeeIds.Count > 0)
        {
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
            .Select(b => new TaxBracket { Step = b.Step, MinIncome = b.MinIncome, MaxIncome = b.MaxIncome, RatePercent = b.RatePercent, IsActive = b.IsActive })
            .ToListAsync(ct);
        if (taxBrackets.Count == 0)
            throw new InvalidOperationException(
                $"ไม่มีตารางอัตราภาษีปี {run.PeriodStart.Year} (Pay_TaxBracket) — เพิ่มตารางของปีนี้ก่อนจึงคำนวณได้");

        var taxDeductionSetting = await context.Pay_TaxDeductionSettings
            .FirstOrDefaultAsync(s => s.EffectiveYear == run.PeriodStart.Year && s.IsActive, ct);
        var personalAllowancePerYear = taxDeductionSetting?.PersonalAllowancePerYear ?? 60000m;
        var expenseDeductionRate = taxDeductionSetting?.ExpenseDeductionRate ?? 0.50m;
        var expenseDeductionCap = taxDeductionSetting?.ExpenseDeductionCap ?? 100000m;
        var providentFundDeductionCap = taxDeductionSetting?.ProvidentFundDeductionCapPerYear ?? 500000m;

        var monthlyTaxElections = await context.Pay_EmployeeTaxDeductionElections
            .Include(e => e.Pay_TaxDeductionType)
            .Where(e => e.IsActive && e.ApplyMonthly && e.Pay_TaxDeductionType.EffectiveYear == run.PeriodStart.Year)
            .ToListAsync(ct);

        var priorEmployerIncomes = await context.Pay_EmployeePriorEmployerIncomes
            .Where(p => p.IsActive && p.TaxYear == run.PeriodStart.Year)
            .ToListAsync(ct);

        var paySchedules = await context.Pay_PaySchedules
            .Where(s => s.CompanyId == run.CompanyId && s.IsActive)
            .Select(s => new PaySchedule { Id = s.Id, Code = s.Code, PeriodsPerMonth = s.PeriodsPerMonth, SecondTermStartDay = s.SecondTermStartDay, AppliesTo = (Advance.Payroll.Core.PayScheduleGroup)s.AppliesTo, EffectiveFrom = s.EffectiveFrom, EffectiveTo = s.EffectiveTo, IsActive = s.IsActive })
            .ToListAsync(ct);
        var payScheduleOverrides = await context.Pay_EmployeePayScheduleOverrides
            .Where(o => o.IsActive)
            .Select(o => new EmployeePayScheduleOverride { HremployeeId = o.HremployeeId, PayScheduleId = o.PayScheduleId, EffectiveFrom = o.EffectiveFrom, EffectiveTo = o.EffectiveTo, IsActive = o.IsActive })
            .ToListAsync(ct);

        var monthStart = new DateOnly(run.PeriodStart.Year, run.PeriodStart.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var sameMonthRows = await context.Pay_PayrollEmployees
            .Where(e => e.PayrollRunId != run.Id
                        && e.Pay_PayrollRun.CompanyId == run.CompanyId
                        && e.Pay_PayrollRun.RunType != PayrollRunType.Bonus
                        && e.Pay_PayrollRun.PeriodStart >= monthStart && e.Pay_PayrollRun.PeriodStart <= monthEnd
                        && e.Pay_PayrollRun.Status >= PayrollRunStatus.Approved
                        && e.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled)
            .Select(e => new { e.Id, e.HremployeeId, e.SocialSecurityAmount, e.SocialSecurityCompanyAmount })
            .ToListAsync(ct);
        var sameMonthSsoByEmp = sameMonthRows.GroupBy(r => r.HremployeeId).ToDictionary(g => g.Key, g => g.Sum(r => r.SocialSecurityAmount));
        var sameMonthSsoCompanyByEmp = sameMonthRows.GroupBy(r => r.HremployeeId).ToDictionary(g => g.Key, g => g.Sum(r => r.SocialSecurityCompanyAmount));
        var sameMonthIds = sameMonthRows.Select(r => r.Id).ToList();
        var sameMonthSsoBaseByEmp = new Dictionary<long, decimal>();
        if (sameMonthIds.Count > 0)
        {
            var ssoBaseLines = await context.Pay_PayrollLineItems
                .Where(li => sameMonthIds.Contains(li.PayrollEmployeeId) && li.SignFlag > 0)
                .Join(context.Pay_PayItemTypes.Where(t => t.IsSsoWageBase), li => li.PayItemTypeId, t => t.Id,
                    (li, t) => new { li.PayrollEmployeeId, li.Amount })
                .ToListAsync(ct);
            var empByRow = sameMonthRows.ToDictionary(r => r.Id, r => r.HremployeeId);
            foreach (var line in ssoBaseLines)
                if (empByRow.TryGetValue(line.PayrollEmployeeId, out var hid))
                    sameMonthSsoBaseByEmp[hid] = sameMonthSsoBaseByEmp.GetValueOrDefault(hid) + line.Amount;
        }

        var attendancePolicy = await context.Pay_AttendanceDeductionPolicies
            .FirstOrDefaultAsync(p => p.CompanyId == run.CompanyId && p.IsActive, ct);

        // วันทำงาน/วันหยุด — ผ่าน IHolidayCalendarSource (เดิม: Lve_CompanySettings/Lve_CompanyHolidays โดยตรง)
        var calendar = await _holidayCalendarSource.GetCalendarAsync(run.CompanyId, run.PeriodStart, run.PeriodEnd, ct);
        var companyWorkDaysMask = calendar.WorkDaysMask;
        var companyHolidays = calendar.Holidays;
        var prorationDivisor = attendancePolicy is { ProrationMode: PayProrationMode.DaysPerMonthDivisor, DaysPerMonthDivisor: > 0 }
            ? attendancePolicy.DaysPerMonthDivisor
            : (int?)null;

        // เดิม: context.Att_DailyAttendances โดยตรง — ตอนนี้ผ่าน IAttendanceFeed
        var attendanceByEmployee = await _attendanceFeed.GetAttendanceForPeriodAsync(run.CompanyId, run.PeriodStart, run.PeriodEnd, ct);

        var advances = await context.Pay_SalaryAdvances
            .Where(a => a.CompanyId == run.CompanyId && a.TargetPeriod == run.PayrollPeriod
                        && (a.Status == PaySalaryAdvanceStatus.Approved || a.Status == PaySalaryAdvanceStatus.Paid
                            || (a.Status == PaySalaryAdvanceStatus.Deducted && a.ConsumedByPayrollRunId == run.Id)))
            .ToListAsync(ct);

        var (ssoRate, ssoEmployerRate, ssoCap) = await _socialSecurityRateProvider.GetCurrentRatesAsync(run.CompanyId, ct);

        // เดิม: OvertimeEarningsCalculator (HrwOt) / LoanDeductionCalculator (Kptempreceivedet) — ตอนนี้ผ่าน feed interfaces
        var otByEmpNo = await _overtimeFeed.GetOvertimeForPeriodAsync(run.CompanyId, run.PeriodStart, run.PeriodEnd, ct);
        var loanByMember = await _loanFeed.GetCooperativeLoanDeductionsByMemberNoAsync(run.CompanyId, run.PayrollPeriod, ct);
        var ytdByEmployee = await LoadYtdRowsAsync(context, run, ct);

        // เดิม: context.Hremployee โดยตรง — ตอนนี้ผ่าน IEmployeeSource
        var eligibleEmployees = (await _employeeSource.GetEligibleEmployeesAsync(run.CompanyId, run.PeriodStart, run.PeriodEnd, ct)).ToList();

        var regularRowsThisPeriod = new Dictionary<long, (decimal Gross, decimal FlatDeduction, int Terms)>();
        if (supplementary)
        {
            var withItems = (await context.Pay_AdhocPayItems
                    .Where(a => a.TargetPeriod == run.PayrollPeriod
                                && a.TargetRunType == PayrollRunType.Bonus
                                && (a.Status == PayAdhocItemStatus.Approved
                                    || (a.Status == PayAdhocItemStatus.Consumed && a.ConsumedByPayrollRunId == run.Id)))
                    .Select(a => a.HremployeeId).Distinct().ToListAsync(ct)).ToHashSet();
            eligibleEmployees = eligibleEmployees.Where(e => withItems.Contains(e.HremployeeId)).ToList();

            regularRowsThisPeriod = (await context.Pay_PayrollEmployees
                    .Where(pe => pe.Pay_PayrollRun.CompanyId == run.CompanyId
                                 && pe.Pay_PayrollRun.PayrollPeriod == run.PayrollPeriod
                                 && pe.Pay_PayrollRun.RunType != PayrollRunType.Bonus
                                 && pe.Pay_PayrollRun.Status >= PayrollRunStatus.Approved
                                 && pe.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled)
                    .Select(pe => new { pe.HremployeeId, pe.GrossEarnings, pe.TaxDeductionAmount, pe.Pay_PayrollRun.TermNo })
                    .ToListAsync(ct))
                .GroupBy(pe => pe.HremployeeId)
                .ToDictionary(g => g.Key, g => (
                    Gross: g.Sum(x => x.GrossEarnings),
                    FlatDeduction: g.Sum(x => x.TaxDeductionAmount),
                    Terms: Math.Max(1, g.Select(x => x.TermNo).Distinct().Count())));
        }

        var heldEmployeeIds = await context.Pay_PayrollRunHolds
            .Where(h => h.PayrollRunId == payrollRunId && h.IsActive)
            .Select(h => h.HremployeeId)
            .ToListAsync(ct);
        if (heldEmployeeIds.Count > 0)
            eligibleEmployees = eligibleEmployees.Where(e => !heldEmployeeIds.Contains(e.HremployeeId)).ToList();

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

        var welfareFundPolicy = await context.Pay_WelfareFundPolicies
            .Where(p => p.CompanyId == run.CompanyId
                        && p.IsEnabled
                        && p.EffectiveFrom <= run.PeriodEnd
                        && (p.EffectiveTo == null || p.EffectiveTo >= run.PeriodStart))
            .FirstOrDefaultAsync(ct);

        var providentFundPolicy = await context.Pay_ProvidentFundPolicies
            .Where(p => p.CompanyId == run.CompanyId
                        && p.IsEnabled
                        && p.EffectiveFrom <= run.PeriodEnd
                        && (p.EffectiveTo == null || p.EffectiveTo >= run.PeriodStart))
            .FirstOrDefaultAsync(ct);

        // เดิม: Wel_BenefitType/Wel_Entitlement/Pos_PositionSlot + WelfareEntitlementResolver.Pick โดยตรง —
        // ตอนนี้ผ่าน IAllowanceFeed ซึ่งคืนยอดที่ resolve แล้วต่อคน (ผู้ implement เป็นคนตัดสิน "default/position/individual")
        IReadOnlyDictionary<long, IReadOnlyList<AllowanceLine>> monthlyAllowancesByEmployee = supplementary
            ? new Dictionary<long, IReadOnlyList<AllowanceLine>>()
            : await _allowanceFeed.GetMonthlyAllowancesAsync(run.CompanyId, run.PeriodStart, ct);

        var payItemTypesById = payItemTypes.Values.ToDictionary(t => t.Id);

        var adhocByEmployee = (await context.Pay_AdhocPayItems
                .Include(a => a.Pay_PayItemType)
                .Where(a => a.TargetPeriod == run.PayrollPeriod
                            && (supplementary ? a.TargetRunType == PayrollRunType.Bonus : a.TargetRunType != PayrollRunType.Bonus)
                            && (a.TargetTermNo == null || a.TargetTermNo == run.TermNo)
                            && (a.Status == PayAdhocItemStatus.Approved
                                || (a.Status == PayAdhocItemStatus.Consumed && a.ConsumedByPayrollRunId == run.Id)))
                .ToListAsync(ct))
            .GroupBy(a => a.HremployeeId).ToDictionary(g => g.Key, g => g.ToList());
        var installmentsByEmployee = (await context.Pay_EmployeeLoanInstallments
                .Include(i => i.Pay_EmployeeLoan)
                .Where(i => i.Period == run.PayrollPeriod
                            && (i.Status == Pay_LoanInstallmentStatus.Pending
                                || (i.Status == Pay_LoanInstallmentStatus.Consumed && i.ConsumedByPayrollRunId == run.Id)))
                .ToListAsync(ct))
            .GroupBy(i => i.Pay_EmployeeLoan.HremployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var negativeCount = 0;
        var totalNet = 0m;

        var progressTotal = eligibleEmployees.Count;
        var progressEvery = Math.Max(1, progressTotal / 100);
        var progressDone = 0;
        progress?.Report((0, progressTotal));

        foreach (var emp in eligibleEmployees)
        {
            if (progressDone > 0 && (progressDone % progressEvery == 0))
                progress?.Report((progressDone, progressTotal));
            progressDone++;

            var joinDate = emp.WorkDate.HasValue ? DateOnly.FromDateTime(emp.WorkDate.Value) : (DateOnly?)null;
            var resignDate = emp.ResignDate.HasValue ? DateOnly.FromDateTime(emp.ResignDate.Value) : (DateOnly?)null;
            var proration = ProrationCalculator.Calculate(run.PeriodStart, run.PeriodEnd, joinDate, resignDate, prorationDivisor);

            var payEmp = new Pay_PayrollEmployee
            {
                PayrollRunId = run.Id,
                HremployeeId = emp.HremployeeId,
                EmpNo = emp.EmpNo,
                CompanyId = emp.CompanyId,
                ProrationFactor = proration.ProrationFactor,
                WorkingDaysInPeriod = proration.WorkingDaysInPeriod,
                ActualWorkingDays = proration.ActualWorkingDays,
                BankCode = emp.SalexpBank,
                BankBranchCode = emp.SalexpBranch,
                BankAccountNo = emp.SalexpAccid,
                CostCenterCode = emp.CostCenterCode,
                IsExcluded = carriedExclusions.ContainsKey(emp.HremployeeId),
                ExcludeReason = carriedExclusions.GetValueOrDefault(emp.HremployeeId),
            };

            var lineItems = new List<Pay_PayrollLineItem>();
            var seq = 0;

            var empAttendance = attendanceByEmployee.TryGetValue(emp.HremployeeId, out var attRows) ? attRows : null;

            var isDailyWage = (emp.DailyWage ?? 0m) > 0m && (emp.SalaryAmt ?? 0m) <= 0m;
            var schedule = PayScheduleResolver.Resolve(emp.HremployeeId,
                isDailyWage ? Advance.Payroll.Core.PayScheduleGroup.DailyWage : Advance.Payroll.Core.PayScheduleGroup.MonthlySalaried,
                run.PeriodStart, paySchedules, payScheduleOverrides);
            if (run.TermNo >= 2 && schedule.PeriodsPerMonth == 1 && !supplementary)
                continue;
            decimal baseSalary;
            if (supplementary)
            {
                baseSalary = 0m;
            }
            else if (isDailyWage)
            {
                var dailyMode = attendancePolicy?.DailyWageMode ?? PayDailyWageDaysMode.WorkingDays;
                var useAttendance = dailyMode == PayDailyWageDaysMode.AttendanceDays && empAttendance is { Count: > 0 };
                int paidDays;
                string paidDaysNote;
                if (useAttendance)
                {
                    paidDays = empAttendance!.Count(a => !a.IsAbsent);
                    paidDaysNote = "วันที่มีการลงเวลา";
                }
                else if (dailyMode == PayDailyWageDaysMode.CalendarDays)
                {
                    paidDays = proration.ActualWorkingDays;
                    paidDaysNote = "วันตามปฏิทินในงวด";
                }
                else
                {
                    var spanStart = joinDate is DateOnly j && j > run.PeriodStart ? j : run.PeriodStart;
                    var spanEnd = resignDate is DateOnly r && r < run.PeriodEnd ? r : run.PeriodEnd;
                    paidDays = spanEnd < spanStart ? 0
                        : WorkingDaysCalculator.CalculateWorkingDays(spanStart, spanEnd, companyHolidays, companyWorkDaysMask);
                    paidDaysNote = "วันทำงานของบริษัทในงวด (ไม่นับวันหยุดประจำสัปดาห์และวันหยุดบริษัท)";
                }
                baseSalary = Math.Round(emp.DailyWage!.Value * paidDays, 2, MidpointRounding.AwayFromZero);
                lineItems.Add(NewLine(payItemTypes["BASE"], PayLineSourceType.Base, baseSalary, 1, ++seq, "HREMPLOYEE", emp.HremployeeId,
                    $"ค่าจ้างรายวัน {emp.DailyWage.Value:N2} × {paidDays} วัน ({paidDaysNote}) = {baseSalary:N2}"));
            }
            else
            {
                var termNote = schedule.MonthFraction != 1m ? $" × ส่วนของเดือน (งวดที่ {schedule.TermNo}/{schedule.PeriodsPerMonth}) {schedule.MonthFraction:0.##}" : "";
                baseSalary = Math.Round((emp.SalaryAmt ?? 0m) * schedule.MonthFraction * proration.ProrationFactor, 2, MidpointRounding.AwayFromZero);
                lineItems.Add(NewLine(payItemTypes["BASE"], PayLineSourceType.Base, baseSalary, 1, ++seq, "HREMPLOYEE", emp.HremployeeId,
                    $"ฐานเงินเดือน {(emp.SalaryAmt ?? 0m):N2}{termNote} × สัดส่วนวันทำงาน {proration.ActualWorkingDays}/{(prorationDivisor is int pd && proration.ActualWorkingDays < proration.WorkingDaysInPeriod ? pd : proration.WorkingDaysInPeriod)} วัน ({proration.ProrationFactor:P2}) = {baseSalary:N2}"));
            }

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
                attendanceDeduction = Math.Min(attendanceDeduction, baseSalary);
            }

            var otAmount = 0m;
            if (!supplementary)
            {
                var otSummary = otByEmpNo.GetValueOrDefault(emp.EmpNo);
                otAmount = otSummary?.Amount ?? 0m;
                if (otAmount != 0)
                    lineItems.Add(NewLine(payItemTypes["OT"], PayLineSourceType.Overtime, otAmount, 1, ++seq, "HRW_OT", null,
                        $"รวมค่าล่วงเวลาจากรายการที่บันทึกไว้ {otSummary?.RecordCount ?? 0} รายการในงวดนี้ = {otAmount:N2}"));
            }

            decimal welfareAllowanceTotal = 0m, welfareTaxableAllowance = 0m, ssoWageBaseAllowance = 0m, pfWageBaseAllowance = 0m;
            if (!supplementary && monthlyAllowancesByEmployee.TryGetValue(emp.HremployeeId, out var allowanceLines))
            {
                foreach (var al in allowanceLines)
                {
                    var amt = al.Amount;
                    if (amt <= 0) continue;
                    var itemType = al.PayItemTypeCode is string code && payItemTypes.TryGetValue(code, out var t) ? t : payItemTypes["ALLOWANCE"];
                    var prorateNote = "";
                    if (al.IsProrated && (proration.ProrationFactor != 1m || schedule.MonthFraction != 1m))
                    {
                        amt = Math.Round(amt * schedule.MonthFraction * proration.ProrationFactor, 2, MidpointRounding.AwayFromZero);
                        prorateNote = $" × สัดส่วนวันทำงาน {proration.ProrationFactor:P2}{(schedule.MonthFraction != 1m ? $" × ส่วนของเดือน {schedule.MonthFraction:0.##}" : "")}";
                    }
                    lineItems.Add(NewLine(itemType, PayLineSourceType.Allowance, amt, 1, ++seq, al.SourceRefTable, al.SourceRefId,
                        $"สวัสดิการจ่ายประจำ {al.Name}{prorateNote} = {amt:N2}"));
                    welfareAllowanceTotal += amt;
                    if (al.IsTaxable) welfareTaxableAllowance += amt;
                    if (al.IsSsoWageBase) ssoWageBaseAllowance += amt;
                    if (al.IsProvidentFundWageBase) pfWageBaseAllowance += amt;
                }
            }

            var grossEarnings = baseSalary + otAmount + welfareAllowanceTotal;

            var ssoWageBase = (payItemTypes["BASE"].IsSsoWageBase ? baseSalary - attendanceDeduction : 0m)
                            + (payItemTypes["OT"].IsSsoWageBase ? otAmount : 0m)
                            + ssoWageBaseAllowance;
            var priorMonthSsoBase = sameMonthSsoBaseByEmp.GetValueOrDefault(emp.HremployeeId);
            var priorMonthSso = sameMonthSsoByEmp.GetValueOrDefault(emp.HremployeeId);
            var ssoAmount = Math.Max(0m,
                SocialSecurityCalculator.Calculate(ssoWageBase + priorMonthSsoBase, ssoRate, ssoCap) - priorMonthSso);
            var ssoCompanyAmount = Math.Max(0m,
                SocialSecurityCalculator.Calculate(ssoWageBase + priorMonthSsoBase, ssoEmployerRate, ssoCap) - sameMonthSsoCompanyByEmp.GetValueOrDefault(emp.HremployeeId));
            if (ssoAmount != 0)
                lineItems.Add(NewLine(payItemTypes["SSO"], PayLineSourceType.SocialSecurity, ssoAmount, -1, ++seq, null, null,
                    $"{ssoRate:0.##}% ของฐานค่าจ้างประกันสังคม {ssoWageBase:N2} (เฉพาะรายการที่ตั้งธง \"ฐาน SSO\" ในแค็ตตาล็อก; เพดาน {ssoCap:N2}) = {ssoAmount:N2}"));

            var election = pfElections.FirstOrDefault(pe => pe.HremployeeId == emp.HremployeeId);
            var pfEmployeeRate = election?.EmployeeContributionRate ?? emp.ProvfEmprate ?? 0m;
            var pfCompanyRate = election?.CompanyContributionRate ?? emp.ProvfCorprate ?? 0m;
            var pfWageBase = (payItemTypes["BASE"].IsProvidentFundWageBase ? Math.Max(0m, baseSalary - attendanceDeduction) : 0m)
                           + (payItemTypes["OT"].IsProvidentFundWageBase ? otAmount : 0m)
                           + pfWageBaseAllowance;
            var pf = ProvidentFundCalculator.Calculate(pfWageBase, pfEmployeeRate, pfCompanyRate);
            if (pf.EmployeeAmount != 0)
                lineItems.Add(NewLine(payItemTypes["PF"], PayLineSourceType.ProvidentFund, pf.EmployeeAmount, -1, ++seq, "Pay_ProvidentFundElection", election?.Id,
                    $"อัตราสะสมพนักงาน {pfEmployeeRate:0.##}% × ค่าจ้างฐานกองทุน {pfWageBase:N2} (เฉพาะรายการที่ตั้งธง \"ฐานกองทุน\" ในแค็ตตาล็อก) = {pf.EmployeeAmount:N2} (บริษัทสมทบ {pfCompanyRate:0.##}% = {pf.CompanyAmount:N2})"));

            var empInsuranceEnrollments = supplementary
                ? new List<Pay_EmployeeInsuranceEnrollment>()
                : insuranceEnrollments.Where(e => e.HremployeeId == emp.HremployeeId).ToList();
            var insuranceEmployeeAmount = empInsuranceEnrollments.Sum(e => e.EmployeeAmount);
            var insuranceCompanyAmount = empInsuranceEnrollments.Sum(e => e.CompanyAmount);
            if (insuranceEmployeeAmount != 0)
                lineItems.Add(NewLine(payItemTypes["INSURANCE"], PayLineSourceType.Insurance, insuranceEmployeeAmount, -1, ++seq, "Pay_EmployeeInsuranceEnrollment", null,
                    $"รวมเบี้ยประกันกลุ่มที่พนักงานสมทบจาก {empInsuranceEnrollments.Count} กรมธรรม์ = {insuranceEmployeeAmount:N2} (บริษัทสมทบ {insuranceCompanyAmount:N2})"));

            var welfareFundEmployeeAmount = 0m;
            var welfareFundCompanyAmount = 0m;
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
                loanAmount = loanByMember.GetValueOrDefault(emp.RefMembno!);
                if (loanAmount != 0)
                    lineItems.Add(NewLine(payItemTypes["LOAN"], PayLineSourceType.Loan, loanAmount, -1, ++seq, "KPTEMPRECEIVEDET", null,
                        $"หักเงินกู้สหกรณ์ตามรายการที่บันทึกไว้ (KPTEMPRECEIVEDET) ในงวดนี้ = {loanAmount:N2}"));
            }

            var empLoanInstallments = supplementary || !installmentsByEmployee.TryGetValue(emp.HremployeeId, out var instRows)
                ? new List<Pay_EmployeeLoanInstallment>()
                : instRows;
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
            loanAmount += empLoanInstallments.Sum(x => x.Amount);

            var adhocItems = adhocByEmployee.TryGetValue(emp.HremployeeId, out var adhocRows) ? adhocRows : new List<Pay_AdhocPayItem>();
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
            var taxableGrossThisPeriod = Math.Max(0m, baseSalary - attendanceDeduction) + otAmount + adhocTaxableEarnings + welfareTaxableAllowance;

            var empMonthlyElections = monthlyTaxElections.Where(e => e.HremployeeId == emp.HremployeeId).ToList();
            var electedAnnualDeduction = empMonthlyElections.Sum(e => e.AnnualAmount);
            var annualFixedDeduction = supplementary ? 0m : personalAllowancePerYear + electedAnnualDeduction;

            var priorEmployerIncome = priorEmployerIncomes.FirstOrDefault(p => p.HremployeeId == emp.HremployeeId);
            var (ytdIncome, ytdDeduction, ytdTax, ytdProvidentFund) = FoldYtd(ytdByEmployee.GetValueOrDefault(emp.HremployeeId), run.PeriodStart, includeSamePeriod: supplementary, priorEmployerIncome);

            var pfDeductible = Math.Min(pf.EmployeeAmount, Math.Max(0m, providentFundDeductionCap - ytdProvidentFund));
            var thisPeriodFlatDeduction = ssoAmount + pfDeductible;
            decimal monthlyTax;
            TaxBracketCalculator.TaxCalculationResult annualCalc;
            if (supplementary)
            {
                regularRowsThisPeriod.TryGetValue(emp.HremployeeId, out var regularRow);
                var monthlyFactor = (decimal)schedule.PeriodsPerMonth / Math.Max(1, regularRow.Terms);
                (monthlyTax, annualCalc) = TaxBracketCalculator.CalculateBonusWithholding(
                    ytdIncome, ytdDeduction, ytdTax,
                    regularRow.Gross * monthlyFactor, regularRow.FlatDeduction * monthlyFactor,
                    taxableGrossThisPeriod, schedule.RemainingMonthsAfterThis,
                    expenseDeductionRate, expenseDeductionCap, taxBrackets,
                    annualFixedDeduction: personalAllowancePerYear + electedAnnualDeduction);
            }
            else
            {
                var monthBaseProjected = schedule.PeriodsPerMonth > 1
                    ? (priorMonthSsoBase + ssoWageBase) * schedule.PeriodsPerMonth / schedule.TermNo
                    : ssoWageBase;
                var ssoMonthlyProjected = SocialSecurityCalculator.Calculate(monthBaseProjected, ssoRate, ssoCap);
                var termsLeftThisMonth = schedule.PeriodsPerMonth - schedule.TermNo;
                var restOfMonthFlat = Math.Max(0m, ssoMonthlyProjected - priorMonthSso - ssoAmount) + pfDeductible * termsLeftThisMonth;
                var monthsAfterThis = Math.Max(0m, schedule.RemainingMonthsIncludingThis - (termsLeftThisMonth + 1) * schedule.MonthFraction);
                var projectedRemainingFlat = thisPeriodFlatDeduction + restOfMonthFlat
                                             + (ssoMonthlyProjected + pfDeductible * schedule.PeriodsPerMonth) * monthsAfterThis;

                (monthlyTax, annualCalc) = TaxBracketCalculator.CalculatePeriodWithholding(
                    ytdIncome, taxableGrossThisPeriod, ytdDeduction, thisPeriodFlatDeduction,
                    expenseDeductionRate, expenseDeductionCap,
                    schedule.RemainingMonthsIncludingThis, schedule.RemainingPeriodsIncludingThis, schedule.PeriodsPerMonth,
                    ytdTax, taxBrackets,
                    annualFixedDeduction: annualFixedDeduction,
                    thisPeriodOneOffIncome: adhocTaxableEarnings,
                    projectedRemainingFlatDeduction: projectedRemainingFlat);
            }
            if (monthlyTax != 0)
                lineItems.Add(NewLine(payItemTypes["TAX"], PayLineSourceType.Tax, monthlyTax, -1, ++seq, null, null,
                    "ภาษีหัก ณ ที่จ่ายประจำเดือน คำนวณจากเงินได้สะสมทั้งปีเทียบตารางอัตราภาษี — ดูรายละเอียดฉบับเต็มในหัวข้อ \"บันทึกการคำนวณภาษี\" ด้านล่าง"));

            var advanceAmount = 0m;
            foreach (var adv in supplementary ? Enumerable.Empty<Pay_SalaryAdvance>() : advances.Where(a => a.HremployeeId == emp.HremployeeId))
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
            payEmp.TaxableIncome = taxableGrossThisPeriod;
            payEmp.TaxDeductionAmount = thisPeriodFlatDeduction;
            payEmp.SocialSecurityAmount = ssoAmount;
            payEmp.SocialSecurityCompanyAmount = ssoCompanyAmount;
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
                Pay_PayrollEmployee = payEmp,
                EventType = PayAuditEventType.TaxCalculationDetail,
                ActorUserId = actorUserId,
                DetailJson = JsonSerializer.Serialize(new
                {
                    emp.EmpNo,
                    GrossEarnings = grossEarnings,
                    YtdIncomeBeforeThisPeriod = ytdIncome,
                    YtdDeductionBeforeThisPeriod = ytdDeduction,
                    RemainingPeriods = schedule.RemainingPeriodsIncludingThis,
                    RemainingMonths = schedule.RemainingMonthsIncludingThis,
                    PeriodsPerMonth = schedule.PeriodsPerMonth,
                    PaySchedule = schedule.ScheduleCode,
                    ProvidentFundWageBase = pfWageBase,
                    ProvidentFundDeductible = pfDeductible,
                    OneOffTaxableIncome = supplementary ? 0m : adhocTaxableEarnings,
                    PriorEmployerIncomeIncluded = priorEmployerIncome is null ? null : new
                    {
                        priorEmployerIncome.PriorEmployerName,
                        priorEmployerIncome.IncomeAmount,
                        priorEmployerIncome.DeductionAmount,
                        priorEmployerIncome.TaxWithheldAmount,
                    },
                    DeductionBreakdown = new
                    {
                        PersonalAllowancePerYear = personalAllowancePerYear,
                        SocialSecurity = ssoAmount,
                        SocialSecurityCompany = ssoCompanyAmount,
                        ProvidentFund = pf.EmployeeAmount,
                        ElectedAnnualDeductions = electedAnnualDeduction,
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

        progress?.Report((progressTotal, progressTotal));

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

    public sealed record YtdRow(DateOnly PeriodStart, decimal Income, decimal FlatDeduction, decimal Tax, decimal ProvidentFund);

    private static async Task<Dictionary<long, List<YtdRow>>> LoadYtdRowsAsync(PayrollDbContext context, Pay_PayrollRun run, CancellationToken ct)
    {
        var yearStart = new DateOnly(run.PeriodStart.Year, 1, 1);
        var rows = await context.Pay_PayrollEmployees
            .Where(e => e.CompanyId == run.CompanyId
                        && e.PayrollRunId != run.Id
                        && e.Pay_PayrollRun.PeriodStart >= yearStart
                        && e.Pay_PayrollRun.PeriodStart <= run.PeriodStart
                        && e.Pay_PayrollRun.Status >= PayrollRunStatus.Approved
                        && e.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled)
            .Select(e => new { e.HremployeeId, e.Pay_PayrollRun.PeriodStart, e.TaxableIncome, e.SocialSecurityAmount, e.ProvidentFundEmployeeAmount, e.TaxAmount })
            .ToListAsync(ct);
        return rows
            .GroupBy(r => r.HremployeeId)
            .ToDictionary(g => g.Key, g => g
                .Select(r => new YtdRow(r.PeriodStart, r.TaxableIncome, r.SocialSecurityAmount + r.ProvidentFundEmployeeAmount, r.TaxAmount, r.ProvidentFundEmployeeAmount))
                .ToList());
    }

    public static (decimal YtdIncome, decimal YtdDeduction, decimal YtdTax, decimal YtdProvidentFund) FoldYtd(
        IReadOnlyList<YtdRow>? rows, DateOnly periodStart, bool includeSamePeriod, Pay_EmployeePriorEmployerIncome? priorEmployerIncome)
    {
        var prior = (rows ?? Array.Empty<YtdRow>())
            .Where(r => includeSamePeriod ? r.PeriodStart <= periodStart : r.PeriodStart < periodStart)
            .ToList();
        var folded = FoldPriorEmployerIncome(prior.Sum(r => r.Income), prior.Sum(r => r.FlatDeduction), prior.Sum(r => r.Tax), priorEmployerIncome);
        return (folded.YtdIncome, folded.YtdDeduction, folded.YtdTax, prior.Sum(r => r.ProvidentFund));
    }

    public static (decimal YtdIncome, decimal YtdDeduction, decimal YtdTax) FoldPriorEmployerIncome(
        decimal ytdIncome, decimal ytdDeduction, decimal ytdTax, Pay_EmployeePriorEmployerIncome? prior)
        => prior is null
            ? (ytdIncome, ytdDeduction, ytdTax)
            : (ytdIncome + prior.IncomeAmount, ytdDeduction + prior.DeductionAmount, ytdTax + prior.TaxWithheldAmount);
}
