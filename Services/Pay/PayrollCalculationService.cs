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

    // Wages not earned that the engine itself deducts before tax/SSO/PF (late, absence, unpaid leave)
    private static readonly string[] WageReductionCodes = { "LATE", "ABSENT", "LEAVE_UNPAID" };

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

        // ── ชนิดของรอบกำหนดว่าคำนวณอะไร ─────────────────────────────────────────────
        //   Regular = เงินเดือนเต็มงวด
        //   Bonus   = "รอบเสริม" — จ่ายเฉพาะรายการเฉพาะกิจที่ HR อนุมัติไว้ให้งวดนี้ (โบนัส
        //             คอมมิชชัน ฯลฯ) ไม่มีเงินเดือน/OT/สวัสดิการ/ประกันสังคม/กองทุน/เงินกู้
        //             ภาษีคิดแบบส่วนต่าง (ภาษีทั้งปีรวมโบนัส − ภาษีทั้งปีไม่รวมโบนัส)
        // ไม่มีรอบกลับรายการ/รอบปรับปรุง (CEO, 17 ก.ย. 2569): งวดที่ปิดแล้วไม่ถูกเปิดหรือหักล้างทั้งงวด
        // ถ้าข้อมูลของใครผิด แก้ด้วยเงินได้/เงินหักรายครั้ง (Pay_AdhocPayItem) ในงวดถัดไปเฉพาะคนนั้น
        if (!PayrollRunTypes.IsSupported(run.RunType))
            throw new InvalidOperationException(PayrollRunTypes.UnsupportedMessage);
        var supplementary = run.RunType == PayrollRunType.Bonus;
        // รอบจ่ายคนออก = คิดเหมือนรอบปกติ (เงินเดือนถึงวันออก OT ประกันสังคม กองทุน ภาษี) แต่เฉพาะคนที่ระบุไว้
        var finalPay = run.RunType == PayrollRunType.FinalPay;

        // ลำดับงวดต้องถูก (audit H3): ภาษีสะสมของงวดนี้อ่านจากงวดก่อนหน้าที่ "อนุมัติแล้ว" เท่านั้น
        // ดังนั้นงวดก่อนหน้าในปีเดียวกันต้องอนุมัติ/ยกเลิกให้หมดก่อน และห้ามคำนวณงวดเก่าซ้ำ
        // เมื่อมีงวดหลังจากนั้นอนุมัติไปแล้ว (ยอดสะสมที่งวดหลังใช้ไปจะไม่ตรงกับความจริง)
        // ภาษีเงินเดือนเป็นเกณฑ์เงินสด (ปีภาษี = ปีที่จ่ายจริง) — ปี ยอดสะสม และลำดับงวดอิงวันจ่าย ไม่ใช่วันเริ่มงวด (audit M-01)
        // (งวด ธ.ค. ที่จ่ายเดือน ม.ค. เป็นเงินได้ของปีใหม่) · งวดที่จ่ายในเดือนเดียวกับงวด ผลเท่าเดิมทุกประการ
        var taxYear = run.PayDate.Year;
        var yearStartForOrder = new DateOnly(taxYear, 1, 1);
        var openEarlier = await context.Pay_PayrollRuns
            .Where(r => r.CompanyId == run.CompanyId && r.Id != run.Id
                        && (r.RunType == PayrollRunType.Regular || r.RunType == PayrollRunType.Bonus || r.RunType == PayrollRunType.FinalPay)
                        && r.PayDate >= yearStartForOrder && (r.PayDate < run.PayDate || (r.PayDate == run.PayDate && r.PeriodStart < run.PeriodStart))
                        && r.Status != PayrollRunStatus.Cancelled && r.Status < PayrollRunStatus.Approved)
            .Select(r => r.PayrollPeriod).Distinct().OrderBy(p => p).ToListAsync(ct);
        if (openEarlier.Count > 0)
            throw new InvalidOperationException(
                $"งวด {string.Join(", ", openEarlier)} ยังไม่ได้อนุมัติ — ต้องอนุมัติหรือยกเลิกงวดก่อนหน้าให้ครบก่อน ไม่งั้นภาษีสะสมของงวดนี้จะขาด");
        var approvedLater = await context.Pay_PayrollRuns
            .Where(r => r.CompanyId == run.CompanyId && r.Id != run.Id
                        && (r.PayDate > run.PayDate || (r.PayDate == run.PayDate && r.PeriodStart > run.PeriodStart)) && r.PayDate.Year == taxYear
                        && (r.Status == PayrollRunStatus.Approved || r.Status == PayrollRunStatus.Posted || r.Status == PayrollRunStatus.Paid))
            .Select(r => r.PayrollPeriod).Distinct().OrderBy(p => p).ToListAsync(ct);
        if (run.RunType == PayrollRunType.Regular && approvedLater.Count > 0)
            throw new InvalidOperationException(
                $"งวด {string.Join(", ", approvedLater)} อนุมัติไปแล้วโดยใช้ยอดสะสมจากงวดนี้ — คำนวณงวดนี้ใหม่ไม่ได้ ถ้าข้อมูลงวดนี้ผิด ให้บันทึกเงินได้/เงินหักรายครั้งในงวดที่ยังเปิดอยู่");
        if (supplementary)
        {
            // รอบโบนัสอาศัยเงินเดือนงวดเดียวกันเป็นฐานประมาณการทั้งปี จึงต้องมีรอบปกติที่อนุมัติแล้ว
            var regularApproved = await context.Pay_PayrollRuns.Where(PayrollRunFilters.RunIsFinal).AnyAsync(r =>
                r.CompanyId == run.CompanyId && r.PayrollPeriod == run.PayrollPeriod
                && r.RunType == PayrollRunType.Regular, ct);
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

        // ธง "กันออก" ต้องรอดการคำนวณใหม่ (CEO, 11 ก.ย. 2569) — แถวผลลัพธ์ด้านล่างถูกลบแล้วสร้างใหม่ทั้งหมด
        // เดิมธงหายไปด้วย คนที่ HR กันออกกลับเข้ารอบเงียบ ๆ และไหลเข้าไฟล์ธนาคาร
        var carriedExclusions = await context.Pay_PayrollEmployees
            .Where(e => e.PayrollRunId == payrollRunId && e.IsExcluded)
            .Select(e => new { e.HremployeeId, e.ExcludeReason })
            .ToDictionaryAsync(e => e.HremployeeId, e => e.ExcludeReason, ct);

        // คืนรายการที่รอบนี้เคยกินไว้ทั้งหมดก่อน แล้วปล่อยให้ลูปด้านล่างหยิบใหม่เอง — คนที่ถูก "พักการจ่าย"
        // (hold) หรือหลุดจากเงื่อนไขไปแล้ว จะได้ไม่ทิ้งโบนัส/งวดผ่อน/เงินเบิกล่วงหน้าค้างสถานะ Consumed
        // ชี้รอบนี้ตลอดไป ซึ่งไม่มีรอบไหนหยิบได้อีก (รอบอื่นหยิบเฉพาะ Approved/Pending)
        await PayrollItemConsumption.ReleaseAsync(context, payrollRunId, ct: ct);
        await context.SaveChangesAsync(ct);

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
            .Where(b => b.EffectiveYear == taxYear && b.IsActive)
            .ToListAsync(ct);
        // ตารางว่าง = ภาษี 0 ทุกคนเงียบ ๆ (audit H4) — ต้องหยุด ไม่ใช่เดาว่าไม่มีภาษี
        if (taxBrackets.Count == 0)
            throw new InvalidOperationException(
                $"ไม่มีตารางอัตราภาษีปี {taxYear} (Pay_TaxBracket) — เพิ่มตารางของปีนี้ก่อนจึงคำนวณได้");
        var paidByOldSystem = await OpeningBalanceGuard.OverlappingEmpNosAsync(context, run, ct);
        if (paidByOldSystem.Count > 0)
            throw new InvalidOperationException(OpeningBalanceGuard.Message(run, paidByOldSystem));

        // Standard/mandatory deduction parameters for this tax year — falls
        // back to the current legal defaults (60,000 personal allowance,
        // 50%/100,000 expense deduction) if HR hasn't seeded a row for this
        // year yet, so calculation never silently reverts to the old
        // zero-deduction bug just because a year's row is missing.
        var taxDeductionSetting = await context.Pay_TaxDeductionSettings
            .FirstOrDefaultAsync(s => s.EffectiveYear == taxYear && s.IsActive, ct);
        var personalAllowancePerYear = taxDeductionSetting?.PersonalAllowancePerYear ?? 60000m;
        var expenseDeductionRate = taxDeductionSetting?.ExpenseDeductionRate ?? 0.50m;
        var expenseDeductionCap = taxDeductionSetting?.ExpenseDeductionCap ?? 100000m;
        var providentFundDeductionCap = taxDeductionSetting?.ProvidentFundDeductionCapPerYear ?? 500000m;

        // Only elections the employee chose to apply monthly ("จ่ายให้น้อยสุด")
        // reduce withholding now — ApplyMonthly=false ("จ่ายก่อนขอคืน") rows
        // are intentionally excluded here; they exist for the employee's own
        // records only.
        var monthlyTaxElections = await context.Pay_EmployeeTaxDeductionElections
            .Include(e => e.Pay_TaxDeductionType)
            .Where(e => e.IsActive && e.ApplyMonthly && e.Pay_TaxDeductionType.EffectiveYear == taxYear)
            .ToListAsync(ct);

        // Mid-year hires only — see Pay_EmployeePriorEmployerIncome.cs and
        // GetYtdAccumulatorsAsync/FoldPriorEmployerIncome below.
        var priorEmployerIncomes = await context.Pay_EmployeePriorEmployerIncomes
            .Where(p => p.IsActive && p.TaxYear == taxYear)
            .ToListAsync(ct);

        // รอบจ่าย (audit M8, CEO 11 ก.ย. 2569): กี่งวดต่อเดือนเป็น config ต่อกลุ่มพนักงาน ทับรายคนได้ เปลี่ยนกลางปีได้
        // ตัวคำนวณอ่านปฏิทินจากตรงนี้ต่อพนักงานแต่ละคน — ไม่ใช่ "13 − เดือน" ที่ถูกเฉพาะบริษัทจ่ายเดือนละงวด
        var paySchedules = await context.Pay_PaySchedules
            .Where(s => s.CompanyId == run.CompanyId && s.IsActive).ToListAsync(ct);
        var payScheduleOverrides = await context.Pay_EmployeePayScheduleOverrides
            .Where(o => o.IsActive).ToListAsync(ct);

        // เพดานประกันสังคมเป็นรายเดือน: งวดครึ่งเดือนต้องรู้ว่ารอบปกติที่อนุมัติแล้วในเดือนเดียวกันหักไปเท่าไร
        // จากฐานเท่าไร แล้วคิดจากฐานทั้งเดือนหักส่วนที่หักไปแล้ว — ไม่ใช่หักเต็มเพดานทั้งสองงวด
        var monthStart = new DateOnly(run.PeriodStart.Year, run.PeriodStart.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        // เฉพาะรอบปกติที่จ่ายจริง (ไม่นับโบนัส และไม่นับคนที่ถูกกันออก — เงินสมทบของคนนั้นไม่ได้ถูกหักจริง)
        var sameMonthRows = await context.Pay_PayrollEmployees
            .Where(PayrollRunFilters.RowWasPaid)
            .Where(e => e.PayrollRunId != run.Id
                        && e.Pay_PayrollRun.CompanyId == run.CompanyId
                        && e.Pay_PayrollRun.RunType == PayrollRunType.Regular
                        && e.Pay_PayrollRun.PeriodStart >= monthStart && e.Pay_PayrollRun.PeriodStart <= monthEnd)
            .Select(e => new { e.Id, e.HremployeeId, e.SocialSecurityAmount, e.SocialSecurityCompanyAmount, e.WelfareFundEmployeeAmount, e.WelfareFundCompanyAmount })
            .ToListAsync(ct);
        var sameMonthWfByEmp = sameMonthRows.GroupBy(r => r.HremployeeId).ToDictionary(g => g.Key, g => g.Sum(r => r.WelfareFundEmployeeAmount));
        var sameMonthWfCompanyByEmp = sameMonthRows.GroupBy(r => r.HremployeeId).ToDictionary(g => g.Key, g => g.Sum(r => r.WelfareFundCompanyAmount));
        var sameMonthSsoByEmp = sameMonthRows.GroupBy(r => r.HremployeeId).ToDictionary(g => g.Key, g => g.Sum(r => r.SocialSecurityAmount));
        var sameMonthSsoCompanyByEmp = sameMonthRows.GroupBy(r => r.HremployeeId).ToDictionary(g => g.Key, g => g.Sum(r => r.SocialSecurityCompanyAmount));
        var sameMonthIds = sameMonthRows.Select(r => r.Id).ToList();
        // จ่ายเดือนละ 2 งวด (audit H-07): รายการ "รายเดือน" (เบี้ยเลี้ยงที่ไม่คิดตามสัดส่วน, เบี้ยประกันกลุ่ม, หักสหกรณ์)
        // เข้าเฉพาะงวดแรกของเดือนที่พนักงานคนนั้นได้รับเงินจริง — คนที่มีแถวจ่ายแล้วในงวดก่อนของเดือนนี้ไม่ใส่ซ้ำ
        var paidEarlierThisMonth = sameMonthRows.Select(r => r.HremployeeId).ToHashSet();
        var sameMonthSsoBaseByEmp = new Dictionary<long, decimal>();
        if (sameMonthIds.Count > 0)
        {
            // นิยามเดียวกับงวดนี้: รายการได้ที่ตั้งธงค่าจ้าง ลบค่าจ้างที่ไม่ได้ทำงาน (สาย/ขาด/ลาไม่รับค่าจ้าง) ถ้าเงินเดือนเป็นฐาน
            // — เดิมงวดก่อนนับเงินเดือนเต็มก่อนหักขาด/สาย ฐานเดือนจึงสูงกว่าที่งวดนั้นหักจริง (audit M-05)
            var baseCountsForSso = payItemTypes["BASE"].IsSsoWageBase;
            var ssoBaseLines = await context.Pay_PayrollLineItems
                .Where(li => sameMonthIds.Contains(li.PayrollEmployeeId))
                .Join(context.Pay_PayItemTypes, li => li.PayItemTypeId, t => t.Id,
                    (li, t) => new { li.PayrollEmployeeId, li.Amount, li.SignFlag, li.SourceRefTable, t.IsSsoWageBase, t.Code })
                .Where(x => (x.SignFlag > 0 && x.IsSsoWageBase)
                            || (baseCountsForSso && x.SignFlag < 0 && WageReductionCodes.Contains(x.Code) && x.SourceRefTable != "Pay_AdhocPayItem"))
                .Select(x => new { x.PayrollEmployeeId, Amount = x.SignFlag > 0 ? x.Amount : -x.Amount })
                .ToListAsync(ct);
            var empByRow = sameMonthRows.ToDictionary(r => r.Id, r => r.HremployeeId);
            foreach (var line in ssoBaseLines)
                if (empByRow.TryGetValue(line.PayrollEmployeeId, out var hid))
                    sameMonthSsoBaseByEmp[hid] = sameMonthSsoBaseByEmp.GetValueOrDefault(hid) + line.Amount;
        }

        // Attendance → money (HR gap wave 1). Policy is per company and OFF by
        // default; attendance rows are loaded once for the period and grouped.
        var attendancePolicy = await context.Pay_AttendanceDeductionPolicies
            .FirstOrDefaultAsync(p => p.CompanyId == run.CompanyId && p.IsActive, ct);

        // วันทำงานของบริษัท (ค่าจ้างรายวันแบบ WorkingDays — audit M6): ตารางวันทำงานจากตั้งค่าการลา + วันหยุดบริษัท
        var companyWorkDaysMask = await context.Lve_CompanySettings
            .Where(s => s.CompanyId == run.CompanyId).Select(s => s.WorkDaysMask).FirstOrDefaultAsync(ct);
        var companyHolidays = (await context.Lve_CompanyHolidays
                .Where(h => h.CompanyId == run.CompanyId && h.IsActive && h.HolidayDate >= run.PeriodStart && h.HolidayDate <= run.PeriodEnd)
                .Select(h => h.HolidayDate).ToListAsync(ct))
            .ToHashSet();
        var prorationDivisor = attendancePolicy is { ProrationMode: PayProrationMode.DaysPerMonthDivisor, DaysPerMonthDivisor: > 0 }
            ? attendancePolicy.DaysPerMonthDivisor
            : (int?)null;
        // รอบตัดเวลา (PST, 21 ก.ย. 2569): เงินเดือนคิดตามงวดปฏิทิน แต่สาย/ขาด/เบี้ยขยันอ่านจากรอบตัดเวลา (เช่น 26–25)
        // ไม่ตั้งวันตัด = ช่วงเดียวกับงวด ผลเท่าเดิม
        var (attFrom, attTo) = AttendanceRuleCalculator.AttendanceWindow(run.PeriodStart, run.PeriodEnd, attendancePolicy?.AttendanceCutoffDay);
        var attendanceWindowNote = attendancePolicy?.AttendanceCutoffDay is int ? $" [รอบตัดเวลา {attFrom:dd/MM}–{attTo:dd/MM/yyyy}]" : "";
        var attendanceRules = await context.Pay_AttendanceRules
            .Where(r => r.CompanyId == run.CompanyId && r.IsActive)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
            .ToListAsync(ct);
        // วิธีนับวันของค่าจ้างรายวันตั้งทับรายประเภทพนักงานได้ (รายวันแบบประจำ 22 วัน vs รายวันทั่วไป)
        // พนักงาน → ประเภท ด้วย Hremployee.EmployeeTypeId (FK)
        var dailyWageByEmpType = await context.Pos_EmployeeTypes
            .Where(x => x.CompanyId == run.CompanyId && x.IsActive && x.DailyWageMode != null)
            .Select(x => new { x.Id, x.DailyWageMode, x.DailyWageFixedDays })
            .ToDictionaryAsync(x => x.Id, ct);
        var attendanceByEmployee = (await context.Att_DailyAttendances
                .Where(a => a.CompanyId == run.CompanyId && a.WorkDate >= attFrom && a.WorkDate <= attTo)
                .Select(a => new { a.HremployeeId, a.WorkDate, a.IsAbsent, a.LateMinutes })
                .ToListAsync(ct))
            .GroupBy(a => a.HremployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // ลาที่อนุมัติแล้วในรอบตัดเวลา: ได้ค่าจ้างหรือไม่ตาม Lve_LeavePolicy.IsPaid ของบริษัท (ไม่มี policy = ได้ค่าจ้าง
        // แบบเดียวกับหน้าคำขอลา) · คำขอที่เคยถูก "ผลักเข้าระบบเงินเดือน" เป็นรายการเฉพาะกิจแล้วไม่นับซ้ำ
        var windowHolidays = (await context.Lve_CompanyHolidays
                .Where(h => h.CompanyId == run.CompanyId && h.IsActive && h.HolidayDate >= attFrom && h.HolidayDate <= attTo)
                .Select(h => h.HolidayDate).ToListAsync(ct))
            .ToHashSet();
        var unpaidLeaveTypeIds = (await context.Lve_LeavePolicies
                .Where(p => p.CompanyId == run.CompanyId && !p.IsPaid)
                .Select(p => p.LeaveTypeId).ToListAsync(ct))
            .ToHashSet();
        var leaveCompleted = HRM.Services.Workflow.WorkflowEngineService.StatusCompleted;
        var leaveDaysByEmployee = (await context.Lve_LeaveRequests
                .Where(l => l.Hremployee.companyid == run.CompanyId && l.JobMasterId != null && l.AdhocPayItemId == null
                            && l.StartDate <= attTo && l.EndDate >= attFrom
                            && context.job_masters.Any(j => j.jobmasterid == l.JobMasterId && j.status == leaveCompleted))
                .Select(l => new { l.HremployeeId, l.StartDate, l.EndDate, l.IsHalfDay, l.LeaveTypeId })
                .ToListAsync(ct))
            .GroupBy(l => l.HremployeeId)
            .ToDictionary(g => g.Key, g => LeavePayCalculator.DaysInWindow(
                g.Select(l => new LeavePayCalculator.ApprovedLeave(l.HremployeeId, l.StartDate, l.EndDate, l.IsHalfDay, !unpaidLeaveTypeIds.Contains(l.LeaveTypeId))),
                attFrom, attTo, windowHolidays, companyWorkDaysMask));

        // Salary advances recovered in this period: Approved/Paid ones, plus those
        // already consumed by THIS run so a recalculation re-picks them idempotently.
        var advances = await context.Pay_SalaryAdvances
            .Where(a => a.CompanyId == run.CompanyId && a.TargetPeriod == run.PayrollPeriod
                        && (a.Status == PaySalaryAdvanceStatus.Approved || a.Status == PaySalaryAdvanceStatus.Paid
                            || (a.Status == PaySalaryAdvanceStatus.Deducted && a.ConsumedByPayrollRunId == run.Id)))
            .ToListAsync(ct);

        var (ssoRate, ssoEmployerRate, ssoCap) = await _socialSecurityRateProvider.GetCurrentRatesAsync(run.CompanyId, run.PeriodStart, ct);

        // (audit M14) โหลดครั้งเดียวต่อรอบ แทนการยิงฐานข้อมูลรายคน: OT, เงินกู้สหกรณ์, ยอดสะสมทั้งปี
        var otByEmpNo = await _overtimeCalculator.GetOvertimeForPeriodByEmployeeAsync(run.CompanyId, run.PeriodStart, run.PeriodEnd, ct);
        var loanByMember = await _loanCalculator.GetLoanDeductionsForPeriodByMemberAsync(run.CompanyId, run.PayrollPeriod, ct);
        var ytdByEmployee = await LoadYtdRowsAsync(context, run, ct);

        var periodEndDt = run.PeriodEnd.ToDateTime(TimeOnly.MaxValue);
        var periodStartDt = run.PeriodStart.ToDateTime(TimeOnly.MinValue);

        var nonPayrollTypeIds = await PayrollEligibility.LoadNonPayrollTypeIdsAsync(context, run.CompanyId, ct);
        var eligibleEmployees = await context.Hremployee
            .Where(PayrollEligibility.InPeriod(run.CompanyId, periodStartDt, periodEndDt, nonPayrollTypeIds))
            .ToListAsync(ct);

        // รอบเสริม: เฉพาะคนที่มีรายการเฉพาะกิจของงวดนี้ (ไม่สร้างแถวศูนย์ให้ทั้งบริษัท)
        // และดึงแถวรอบปกติของงวดเดียวกันไว้เป็นฐานประมาณการภาษี
        var regularRowsThisPeriod = new Dictionary<long, (decimal Gross, decimal FlatDeduction, int Terms)>();
        if (supplementary)
        {
            var withItems = (await context.Pay_AdhocPayItems
                    .Where(a => a.TargetPeriod == run.PayrollPeriod
                                && a.TargetRunType == PayrollRunType.Bonus
                                && (a.Status == PayAdhocItemStatus.Approved
                                    || (a.Status == PayAdhocItemStatus.Consumed && a.ConsumedByPayrollRunId == run.Id)))
                    .Select(a => a.HremployeeId).Distinct().ToListAsync(ct)).ToHashSet();
            eligibleEmployees = eligibleEmployees.Where(e => withItems.Contains(e.id)).ToList();

            // ฐานของรอบโบนัส = รอบปกติของงวดนี้ที่จ่ายจริง
            regularRowsThisPeriod = (await context.Pay_PayrollEmployees
                    .Where(PayrollRunFilters.RowWasPaid)
                    .Where(pe => pe.Pay_PayrollRun.CompanyId == run.CompanyId
                                 && pe.Pay_PayrollRun.PayrollPeriod == run.PayrollPeriod
                                 && pe.Pay_PayrollRun.RunType == PayrollRunType.Regular)
                    // Recurring taxable income only (audit H-10): the bonus difference method projects the
                    // regular month over the rest of the year, so it must not include non-taxable items
                    // (GrossEarnings did) nor this period's one-off taxable ad-hoc earnings (commission etc.).
                    .Select(pe => new
                    {
                        pe.HremployeeId,
                        pe.TaxableIncome,
                        // เท่ากับ TaxableBasis ของรายการเฉพาะกิจที่ยึดโยงกับแถวนี้ (หลังหักส่วนยกเว้น/ค่าใช้จ่ายของค่าชดเชย)
                        OneOffTaxable = context.Pay_AdhocPayItems
                            .Where(a => a.IsTaxable && pe.Pay_PayrollLineItems.Any(li =>
                                li.SourceRefTable == "Pay_AdhocPayItem" && li.SignFlag > 0 && li.SourceRefId == a.Id))
                            .Sum(a => (decimal?)(a.Amount - (a.TaxExemptAmount ?? 0m) - (a.TaxExpenseDeductionAmount ?? 0m))) ?? 0m,
                        pe.TaxDeductionAmount,
                        pe.Pay_PayrollRun.TermNo,
                    })
                    .ToListAsync(ct))
                .GroupBy(pe => pe.HremployeeId)
                .ToDictionary(g => g.Key, g => (
                    Gross: g.Sum(x => x.TaxableIncome - x.OneOffTaxable),
                    FlatDeduction: g.Sum(x => x.TaxDeductionAmount),
                    // งวดที่มีเงินจริงในเดือนนี้ (บริษัท 2 งวด: ถ้าอนุมัติแล้วทั้งสองงวด Σ คือทั้งเดือน ห้ามคูณ 2 ซ้ำ)
                    Terms: Math.Max(1, g.Select(x => x.TermNo).Distinct().Count())));
        }

        // รอบจ่ายคนออก: เฉพาะพนักงานที่ระบุในรอบ · รอบปกติ: ข้ามคนที่จ่ายในรอบจ่ายคนออกของงวดเดียวกันแล้ว
        if (finalPay)
        {
            var members = (await context.Pay_PayrollRunMembers.Where(m => m.PayrollRunId == run.Id)
                .Select(m => m.HremployeeId).ToListAsync(ct)).ToHashSet();
            eligibleEmployees = eligibleEmployees.Where(e => members.Contains(e.id)).ToList();
        }
        else if (!supplementary)
        {
            var settled = await PayrollEligibility.PaidInFinalPayRunAsync(context, run, ct);
            if (settled.Count > 0)
                eligibleEmployees = eligibleEmployees.Where(e => !settled.ContainsKey(e.id)).ToList();
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

        // จำนวนงวด/เดือนที่เหลือของปีอ่านต่อพนักงานจากปฏิทินจ่าย (PayScheduleResolver) ในลูปด้านล่าง

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

        // โหลดครั้งเดียวต่อรอบแทน query ต่อพนักงาน (audit M14: 7,000 คน = 35,000 round-trip)
        // รายการเฉพาะกิจของงวดนี้ที่อนุมัติแล้ว หรือที่รอบนี้เคยใช้ไปแล้ว (คำนวณซ้ำหยิบเดิมได้)
        // รายการระบุรอบ (12 ก.ย. 2569): รอบโบนัสเห็นเฉพาะรายการที่ตั้ง "รอบโบนัส"; รอบปกติ/ปรับปรุงเห็นที่เหลือ
        // และบริษัทจ่าย 2 งวด: รายการที่ระบุงวดที่ของเดือนไปเฉพาะงวดนั้น (ไม่ระบุ = รอบแรกที่คำนวณ)
        var adhocByEmployee = (await context.Pay_AdhocPayItems
                .Include(a => a.Pay_PayItemType)
                .Where(a => a.TargetPeriod == run.PayrollPeriod
                            // รอบจ่ายคนออกหยิบรายการของรอบปกติด้วย (คนนี้ไม่อยู่ในรอบปกติของงวดนี้แล้ว) — รอบปกติไม่หยิบรายการของรอบจ่ายคนออก
                            && (supplementary ? a.TargetRunType == PayrollRunType.Bonus
                                : finalPay ? a.TargetRunType == PayrollRunType.Regular || a.TargetRunType == PayrollRunType.FinalPay
                                : a.TargetRunType != PayrollRunType.Bonus && a.TargetRunType != PayrollRunType.FinalPay)
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
        // นับเฉพาะแถวที่สร้างจริง — ปฏิทินผสม (บริษัทรายเดือนงวดเดียว + รายวัน 2 งวด) ข้ามพนักงานรายเดือนในงวดที่ 2
        // กลางลูป (ดูจุด "continue" ด้านล่าง) ทำให้ eligibleEmployees.Count เดิมนับเกินจำนวนแถวที่บันทึกจริง
        var processedCount = 0;

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

            var joinDate = emp.WorkDate.HasValue ? DateOnly.FromDateTime(emp.WorkDate.Value) : (DateOnly?)null;
            var resignDate = emp.ResignDate.HasValue ? DateOnly.FromDateTime(emp.ResignDate.Value) : (DateOnly?)null;
            // Daily-wage employees (DAILY_WAGE set, no monthly salary) are paid per
            // day instead of a pro-rated monthly amount. Which days count is policy:
            // calendar days in the period, or attended days once time clocks exist.
            var isDailyWage = (emp.DailyWage ?? 0m) > 0m && (emp.SalaryAmt ?? 0m) <= 0m;
            var schedule = PayScheduleResolver.Resolve(emp.id,
                isDailyWage ? PayScheduleGroup.DailyWage : PayScheduleGroup.MonthlySalaried,
                run.PeriodStart, paySchedules, payScheduleOverrides);
            // งวดสุดท้ายของคนที่ออก (วันออกอยู่ในงวดนี้): ไม่มีเงินได้หลังจากนี้แล้ว ภาษีทั้งปีจึงคิดจากเงินได้จริง
            // (สะสม + งวดนี้) แล้วหักส่วนที่ยังขาดทั้งหมดในงวดนี้ — ไม่ประมาณการเงินเดือนต่อถึงสิ้นปีแล้วหักแค่ส่วนเดียว
            var leavesThisPeriod = resignDate is DateOnly lastDay && lastDay <= run.PeriodEnd && !supplementary;
            if (leavesThisPeriod)
                schedule = schedule with { RemainingMonthsIncludingThis = schedule.MonthFraction, RemainingPeriodsIncludingThis = 1 };
            // เกณฑ์เงินสด (audit M-01): เดือนที่เหลือในปีภาษีนับจากเดือนที่จ่ายจริง — งวด ธ.ค. ที่จ่าย ม.ค. คืองวดแรกของปีใหม่
            // ใช้เฉพาะตัวเลขประมาณการภาษี (Remaining*) · ส่วนของเดือน/งวดที่ของเงินเดือนยังอิงงวดเดิม
            var payMonthShift = (run.PayDate.Year * 12 + run.PayDate.Month) - (run.PeriodStart.Year * 12 + run.PeriodStart.Month);
            if (payMonthShift > 0 && !leavesThisPeriod)
            {
                var byPayMonth = PayScheduleResolver.Resolve(emp.id,
                    isDailyWage ? PayScheduleGroup.DailyWage : PayScheduleGroup.MonthlySalaried,
                    run.PeriodStart.AddMonths(payMonthShift), paySchedules, payScheduleOverrides);
                schedule = schedule with
                {
                    RemainingMonthsIncludingThis = byPayMonth.RemainingMonthsIncludingThis,
                    RemainingPeriodsIncludingThis = byPayMonth.RemainingPeriodsIncludingThis,
                };
            }
            var proration = ProrationCalculator.Calculate(run.PeriodStart, run.PeriodEnd, joinDate, resignDate, prorationDivisor, schedule.MonthFraction);

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
                // คำนวณให้ตามปกติ (ตัวเลขยังดูได้) แต่ยังถูกกันออกจากไฟล์ธนาคาร/สลิป/GL เหมือนก่อนคำนวณใหม่
                IsExcluded = carriedExclusions.ContainsKey(emp.id),
                ExcludeReason = carriedExclusions.GetValueOrDefault(emp.id),
            };

            var lineItems = new List<Pay_PayrollLineItem>();
            var seq = 0;

            var empAttendance = attendanceByEmployee.TryGetValue(emp.id, out var attRows) ? attRows : null;

            // บริษัทผสม (เช่น รายเดือนจ่ายเดือนละงวด รายวันจ่าย 2 งวด): คนที่ปฏิทินเป็นเดือนละงวดรับเต็มเดือนในงวดที่ 1
            // และต้องไม่ถูกจ่ายซ้ำในงวดที่ 2 ของเดือนเดียวกัน
            if (run.TermNo >= 2 && schedule.PeriodsPerMonth == 1 && !supplementary)
                continue;   // progressDone ถูกนับไว้ต้นลูปแล้ว
            decimal baseSalary;
            if (supplementary)
            {
                baseSalary = 0m;   // รอบเสริมไม่มีเงินเดือน — จ่ายเฉพาะรายการเฉพาะกิจ
            }
            else if (isDailyWage)
            {
                // นับวันจ่ายตามนโยบาย (audit M6): ค่าเริ่มต้น = วันทำงานจริงของบริษัท (ไม่นับเสาร์-อาทิตย์/วันหยุดบริษัท)
                // ไม่ใช่วันตามปฏิทินซึ่งจ่ายเกินให้พนักงานรายวัน — วันตามปฏิทินยังเลือกได้ถ้าบริษัทจ่ายแบบนั้นจริง
                var typeOverride = emp.EmployeeTypeId is long etid && dailyWageByEmpType.TryGetValue(etid, out var dwo) ? dwo : null;
                var dailyMode = typeOverride?.DailyWageMode ?? attendancePolicy?.DailyWageMode ?? PayDailyWageDaysMode.WorkingDays;
                var fixedDays = typeOverride?.DailyWageFixedDays ?? attendancePolicy?.DailyWageFixedDays;
                var useAttendance = dailyMode == PayDailyWageDaysMode.AttendanceDays && empAttendance is { Count: > 0 };
                decimal paidDays;
                string paidDaysNote;
                // ลาที่อนุมัติแล้ว: วันลาแบบได้ค่าจ้างเป็นวันที่ได้เงินแม้ไม่มีการลงเวลา (ม.57 ป่วย · ม.30 พักร้อน · ม.34 กิจ)
                // วันลาไม่รับค่าจ้างไม่ได้เงิน — เดิมโหมดวันทำงาน/จำนวนวันคงที่จ่ายเต็ม ส่วนโหมดวันลงเวลาไม่จ่ายวันลาป่วย
                var empLeaveDaysD = leaveDaysByEmployee.GetValueOrDefault(emp.id) ?? new List<LeavePayCalculator.LeaveDay>();
                var unpaidLeaveD = empLeaveDaysD.Sum(d => d.UnpaidFraction);
                var unpaidLeaveNote = unpaidLeaveD > 0 ? $" หักลาไม่รับค่าจ้าง {unpaidLeaveD:0.#} วัน" : "";
                var spanStartD = joinDate is DateOnly jd && jd > run.PeriodStart ? jd : run.PeriodStart;
                var spanEndD = resignDate is DateOnly rd && rd < run.PeriodEnd ? rd : run.PeriodEnd;
                // ม.56 (audit H-09): วันหยุดตามประเพณีจ่ายค่าจ้างให้ลูกจ้างรายวันด้วย — นับเฉพาะที่ตรงกับวันทำงานของบริษัท
                // (วันหยุดที่ตรงวันหยุดประจำสัปดาห์มีวันหยุดชดเชยในรายการอยู่แล้ว จึงไม่นับซ้ำ)
                var paidHolidays = spanEndD < spanStartD ? 0 : companyHolidays.Count(h => h >= spanStartD && h <= spanEndD
                    && HRM.Services.Leave.LeaveDayCalculator.CalculateWorkingDays(h, h, new HashSet<DateOnly>(), companyWorkDaysMask) > 0);
                if (useAttendance)
                {
                    var workedDates = empAttendance!.Where(a => !a.IsAbsent).Select(a => a.WorkDate).ToHashSet();
                    var paidLeaveD = empLeaveDaysD.Where(d => !workedDates.Contains(d.Date)).Sum(d => d.PaidFraction);
                    paidDays = workedDates.Count + paidHolidays + paidLeaveD;
                    paidDaysNote = $"วันที่มีการลงเวลา + วันหยุดตามประเพณี {paidHolidays} วัน{(paidLeaveD > 0 ? $" + วันลาที่ได้ค่าจ้าง {paidLeaveD:0.#} วัน" : "")}";
                }
                else if (dailyMode == PayDailyWageDaysMode.FixedDaysPerMonth && fixedDays is int fd && fd > 0)
                {
                    // รายวันแบบประจำ: จำนวนวันคงที่ต่อเดือน (เช่น 22) − วันขาดงานในรอบตัดเวลา
                    // เข้า/ออกกลางงวด: จ่ายตามวันทำงานจริงของช่วงที่อยู่ แต่ไม่เกินจำนวนวันคงที่
                    var fullPeriod = spanStartD <= run.PeriodStart && spanEndD >= run.PeriodEnd;
                    var spanWorkdays = spanEndD < spanStartD ? 0
                        : (int)HRM.Services.Leave.LeaveDayCalculator.CalculateWorkingDays(spanStartD, spanEndD, new HashSet<DateOnly>(), companyWorkDaysMask);
                    var entitled = fullPeriod ? (int)Math.Round(fd * schedule.MonthFraction, MidpointRounding.AwayFromZero) : Math.Min(fd, spanWorkdays);
                    var absentFixed = empAttendance?.Count(a => a.IsAbsent) ?? 0;
                    paidDays = Math.Max(0m, entitled - absentFixed - unpaidLeaveD);
                    paidDaysNote = $"จำนวนวันคงที่ {fd} วัน/เดือน{(fullPeriod ? "" : $" — อยู่ไม่เต็มงวด จ่าย {entitled} วัน")}{(absentFixed > 0 ? $" หักขาดงาน {absentFixed} วัน" : "")}{unpaidLeaveNote}{attendanceWindowNote}";
                }
                else if (dailyMode == PayDailyWageDaysMode.CalendarDays)
                {
                    paidDays = Math.Max(0m, proration.ActualWorkingDays - unpaidLeaveD);
                    paidDaysNote = $"วันตามปฏิทินในงวด{unpaidLeaveNote}";
                }
                else
                {
                    // วันทำงานของบริษัท (รวมวันหยุดตามประเพณีที่ตรงวันทำงาน) − วันที่ขาดงาน (audit H-09: เดิมวันหยุดไม่ได้ค่าจ้าง
                    // แต่วันขาดงานได้ค่าจ้าง ซึ่งกลับด้านกับกฎหมาย)
                    var workdays = spanEndD < spanStartD ? 0
                        : (int)HRM.Services.Leave.LeaveDayCalculator.CalculateWorkingDays(spanStartD, spanEndD, new HashSet<DateOnly>(), companyWorkDaysMask);
                    var absentDays = empAttendance?.Count(a => a.IsAbsent) ?? 0;
                    paidDays = Math.Max(0m, workdays - absentDays - unpaidLeaveD);
                    paidDaysNote = $"วันทำงานของบริษัทในงวด รวมวันหยุดตามประเพณี {paidHolidays} วัน{(absentDays > 0 ? $" หักขาดงาน {absentDays} วัน" : "")}{unpaidLeaveNote}";
                }
                baseSalary = Math.Round(emp.DailyWage!.Value * paidDays, 2, MidpointRounding.AwayFromZero);
                lineItems.Add(NewLine(payItemTypes["BASE"], PayLineSourceType.Base, baseSalary, 1, ++seq, "HREMPLOYEE", emp.id,
                    $"ค่าจ้างรายวัน {emp.DailyWage.Value:N2} × {paidDays:0.#} วัน ({paidDaysNote}) = {baseSalary:N2}"));
            }
            else
            {
                // งวดครึ่งเดือน (ปฏิทินจ่าย 2 งวด/เดือน) จ่ายเงินเดือน × ส่วนของเดือน (½) — พบจากเทสทั้งปี 2568 ว่าเดิมจ่ายเต็มเดือนทั้งสองงวด
                var termNote = schedule.MonthFraction != 1m ? $" × ส่วนของเดือน (งวดที่ {schedule.TermNo}/{schedule.PeriodsPerMonth}) {schedule.MonthFraction:0.##}" : "";
                // ExactFactor, not the 4-decimal display factor (audit L-02); the divisor shrinks with the
                // period (30 × ½) so a half-month is not prorated twice (audit H-06).
                baseSalary = Math.Round((emp.SalaryAmt ?? 0m) * schedule.MonthFraction * proration.ExactFactor, 2, MidpointRounding.AwayFromZero);
                var divisorNote = prorationDivisor is int pd && proration.ActualWorkingDays < proration.WorkingDaysInPeriod
                    ? (pd * schedule.MonthFraction).ToString("0.##")
                    : proration.WorkingDaysInPeriod.ToString();
                lineItems.Add(NewLine(payItemTypes["BASE"], PayLineSourceType.Base, baseSalary, 1, ++seq, "HREMPLOYEE", emp.id,
                    $"ฐานเงินเดือน {(emp.SalaryAmt ?? 0m):N2}{termNote} × สัดส่วนวันทำงาน {proration.ActualWorkingDays}/{divisorNote} วัน ({proration.ProrationFactor:P2}) = {baseSalary:N2}"));
            }

            // Late / absence deductions from Att_DailyAttendance per policy. Monthly
            // staff only — a daily-wage employee's absent day is simply not paid above.
            var attendanceDeduction = 0m;
            // ม.76: สิ่งที่ตัดจากเงินเดือนได้คือค่าจ้างของเวลาที่ไม่ได้ทำงาน ไม่ใช่ค่าปรับ — ทุกกติกา (นโยบายเดิม + กติกาตั้งค่าได้)
            // รวมกันต้องไม่เกินค่าจ้างของนาทีที่สาย/วันที่ขาดจริง (AttendanceRuleCalculator.TimeNotWorkedValue)
            decimal lateTaken = 0m, absentTaken = 0m;
            var (lateCap, absentCap) = !supplementary && !isDailyWage && empAttendance is { Count: > 0 }
                ? AttendanceRuleCalculator.TimeNotWorkedValue(emp.SalaryAmt ?? 0m, attendancePolicy?.DaysPerMonthDivisor ?? 30, attendancePolicy?.HoursPerDay ?? 8m,
                    empAttendance.Where(a => !a.IsAbsent).Sum(a => a.LateMinutes), empAttendance.Count(a => a.IsAbsent))
                : (0m, 0m);
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
                        var cappedLate = AttendanceRuleCalculator.CapToTimeNotWorked(lateAmount, lateCap, lateTaken);
                        if (cappedLate < lateAmount) lateNote += $"{AttendanceRuleCalculator.CappedNote} เหลือ {cappedLate:N2}";
                        lateAmount = cappedLate;
                        if (lateAmount > 0)
                        {
                            lineItems.Add(NewLine(payItemTypes["LATE"], PayLineSourceType.Adjustment, lateAmount, -1, ++seq, "Att_DailyAttendance", null, lateNote));
                            attendanceDeduction += lateAmount;
                            lateTaken += lateAmount;
                        }
                    }
                }

                if (attendancePolicy.AbsentMode == PayAbsentDeductionMode.DailyRate)
                {
                    var absentDays = empAttendance.Count(a => a.IsAbsent);
                    if (absentDays > 0)
                    {
                        var absentAmount = AttendanceRuleCalculator.CapToTimeNotWorked(Math.Round(absentDays * dailyRate, 2, MidpointRounding.AwayFromZero), absentCap, absentTaken);
                        absentTaken += absentAmount;
                        lineItems.Add(NewLine(payItemTypes["ABSENT"], PayLineSourceType.Adjustment, absentAmount, -1, ++seq, "Att_DailyAttendance", null,
                            $"ขาดงาน {absentDays} วัน × ค่าจ้างรายวัน {dailyRate:N2} (เงินเดือน {monthly:N2} ÷ {attendancePolicy.DaysPerMonthDivisor}) = {absentAmount:N2}"));
                        attendanceDeduction += absentAmount;
                    }
                }
                // Wages not earned can never exceed the wages of the period.
                attendanceDeduction = Math.Min(attendanceDeduction, baseSalary);
            }

            // กติกาแบบตั้งค่าได้ (Pay_AttendanceRule) — ข้อเท็จจริงเวลาทำงานของคนนี้ในรอบตัดเวลา
            var ruleFacts = new AttendanceRuleCalculator.Facts(
                empAttendance?.Where(a => !a.IsAbsent && a.LateMinutes > 0).Select(a => a.LateMinutes).ToList() ?? new List<int>(),
                empAttendance?.Count(a => a.IsAbsent) ?? 0);
            if (!supplementary && !isDailyWage && attendanceRules.Count > 0)
            {
                foreach (var rule in attendanceRules.Where(r => r.Target == PayAttendanceTarget.BaseSalary))
                {
                    var outcome = AttendanceRuleCalculator.Apply(rule, ruleFacts, baseSalary, baseSalary - attendanceDeduction);
                    if (outcome.Deduction <= 0m) continue;
                    var isLate = rule.Trigger == PayAttendanceTrigger.Late;
                    var allowed = AttendanceRuleCalculator.CapToTimeNotWorked(outcome.Deduction, isLate ? lateCap : absentCap, isLate ? lateTaken : absentTaken);
                    if (allowed <= 0m) continue;
                    var ruleLineNote = outcome.Note + (allowed < outcome.Deduction ? $"{AttendanceRuleCalculator.CappedNote} เหลือ {allowed:N2}" : "");
                    lineItems.Add(NewLine(payItemTypes[isLate ? "LATE" : "ABSENT"], PayLineSourceType.Adjustment,
                        allowed, -1, ++seq, "Pay_AttendanceRule", rule.Id, ruleLineNote + attendanceWindowNote));
                    attendanceDeduction += allowed;
                    if (isLate) lateTaken += allowed; else absentTaken += allowed;
                }
            }

            // ลาไม่รับค่าจ้าง (รายเดือน): ค่าจ้างที่ไม่ได้ทำงาน อัตราต่อวันเดียวกับขาดงาน หักก่อนภาษีและก่อนฐานประกันสังคม/กองทุน
            // (เดิมต้องกด "ผลักเข้าระบบเงินเดือน" เป็นรายการหักหลังภาษี ÷ วันในเดือน — พนักงานเสียภาษีและประกันสังคมจากเงินที่ไม่ได้รับ)
            if (!supplementary && !isDailyWage)
            {
                var unpaidLeaveDays = (leaveDaysByEmployee.GetValueOrDefault(emp.id) ?? new List<LeavePayCalculator.LeaveDay>()).Sum(d => d.UnpaidFraction);
                if (unpaidLeaveDays > 0)
                {
                    var divisor = attendancePolicy is { DaysPerMonthDivisor: > 0 } ? attendancePolicy.DaysPerMonthDivisor : 30;
                    var leaveDayRate = (emp.SalaryAmt ?? 0m) / divisor;
                    var unpaidLeaveAmount = Math.Min(Math.Round(unpaidLeaveDays * leaveDayRate, 2, MidpointRounding.AwayFromZero),
                        Math.Max(0m, baseSalary - attendanceDeduction));
                    if (unpaidLeaveAmount > 0)
                    {
                        lineItems.Add(NewLine(payItemTypes["LEAVE_UNPAID"], PayLineSourceType.Adjustment, unpaidLeaveAmount, -1, ++seq, "Lve_LeaveRequest", null,
                            $"ลาไม่รับค่าจ้าง {unpaidLeaveDays:0.#} วัน × ค่าจ้างรายวัน {leaveDayRate:N2} (เงินเดือน {(emp.SalaryAmt ?? 0m):N2} ÷ {divisor}) = {unpaidLeaveAmount:N2}{attendanceWindowNote}"));
                        attendanceDeduction += unpaidLeaveAmount;
                    }
                }
            }

            var otAmount = 0m;
            if (!supplementary)
            {
                var otRecords = otByEmpNo.GetValueOrDefault(emp.EmpNo) ?? new List<HrwOt>();
                otAmount = OvertimeEarningsCalculator.SumAmount(otRecords);
                if (otAmount != 0)
                    lineItems.Add(NewLine(payItemTypes["OT"], PayLineSourceType.Overtime, otAmount, 1, ++seq, "HRW_OT", null,
                        $"รวมค่าล่วงเวลาจากรายการที่บันทึกไว้ {otRecords.Count} รายการในงวดนี้ = {otAmount:N2}"));
            }

            // Welfare monthly allowances — per-person amount via the resolver's
            // pure Pick (company default / position / individual). Emitted as
            // earning lines sourced from Wel_BenefitType.
            decimal welfareAllowanceTotal = 0m, welfareTaxableAllowance = 0m, ssoWageBaseAllowance = 0m, pfWageBaseAllowance = 0m;
            var takeMonthlyItems = !supplementary && !paidEarlierThisMonth.Contains(emp.id);
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
                    // A flat (non-prorated) monthly allowance is paid once a month, not once per term.
                    if (!itemType.IsProrated && !takeMonthlyItems) continue;
                    if (itemType.IsProrated && (proration.ProrationFactor != 1m || schedule.MonthFraction != 1m))
                    {
                        amt = Math.Round(amt * schedule.MonthFraction * proration.ExactFactor, 2, MidpointRounding.AwayFromZero);
                        prorateNote = $" × สัดส่วนวันทำงาน {proration.ProrationFactor:P2}{(schedule.MonthFraction != 1m ? $" × ส่วนของเดือน {schedule.MonthFraction:0.##}" : "")}";
                    }
                    // กติกาเวลาทำงานที่ผูกกับรายได้ตัวนี้ (เช่น เบี้ยขยัน: สายในรอบตัดเวลา → ไม่จ่าย) — ยอดเต็มก่อนหักอยู่ในคำอธิบาย
                    var ruleNote = "";
                    if (attendanceRules.Count > 0)
                    {
                        var fullAmt = amt;
                        foreach (var rule in attendanceRules.Where(r => r.Target == PayAttendanceTarget.RecurringEarning && r.WelBenefitTypeId == wb.Id))
                        {
                            var outcome = AttendanceRuleCalculator.Apply(rule, ruleFacts, fullAmt, amt);
                            if (outcome.Deduction <= 0m) continue;
                            amt -= outcome.Deduction;
                            ruleNote += $" · {outcome.Note}";
                        }
                        if (ruleNote.Length > 0) ruleNote = $" (ยอดเต็ม {fullAmt:N2}{ruleNote}{attendanceWindowNote})";
                    }
                    // ถูกตัดจนเหลือ 0 ยังลงบรรทัดไว้ให้พนักงานเห็นเหตุผลบนสลิป ไม่ใช่หายไปเฉย ๆ
                    if (amt <= 0 && ruleNote.Length == 0) continue;
                    lineItems.Add(NewLine(itemType, PayLineSourceType.Allowance, amt, 1, ++seq, "Wel_BenefitType", wb.Id,
                        $"สวัสดิการจ่ายประจำ {wb.NameTh}{prorateNote}{ruleNote} = {amt:N2}"));
                    welfareAllowanceTotal += amt;
                    if (wb.IsTaxable) welfareTaxableAllowance += amt;
                    if (itemType.IsSsoWageBase) ssoWageBaseAllowance += amt;
                    if (itemType.IsProvidentFundWageBase) pfWageBaseAllowance += amt;
                }
            }

            var grossEarnings = baseSalary + otAmount + welfareAllowanceTotal;

            // Social-security wage base follows the Pay Element catalog flags: only
            // elements marked IsSsoWageBase count (base salary + regular allowances by
            // default; OT and one-off items are excluded, as Thai SSO defines ค่าจ้าง).
            // รายการเฉพาะกิจที่ตั้งธงเป็นค่าจ้าง (เช่น ค่าคอมมิชชั่น) นับเข้าฐานด้วย — เดิมไม่นับในงวดนี้ แต่งวดถัดไปของเดือน
            // นับจากบรรทัดที่บันทึกไว้ ฐานสองงวดจึงนิยามไม่ตรงกัน (audit M-05/M-14)
            var adhocForBase = supplementary ? new List<Pay_AdhocPayItem>() : adhocByEmployee.GetValueOrDefault(emp.id) ?? new List<Pay_AdhocPayItem>();
            var adhocSsoWage = adhocForBase.Where(a => a.Pay_PayItemType.DefaultSignFlag > 0 && a.Pay_PayItemType.IsSsoWageBase).Sum(a => a.Amount);
            var adhocPfWage = adhocForBase.Where(a => a.Pay_PayItemType.DefaultSignFlag > 0 && a.Pay_PayItemType.IsProvidentFundWageBase).Sum(a => a.Amount);
            var ssoWageBase = (payItemTypes["BASE"].IsSsoWageBase ? baseSalary - attendanceDeduction : 0m)
                            + (payItemTypes["OT"].IsSsoWageBase ? otAmount : 0m)
                            + ssoWageBaseAllowance + adhocSsoWage;
            // เพดานประกันสังคมเป็นรายเดือน (audit M8): คิดจากฐานทั้งเดือน (งวดก่อนของเดือนนี้ที่อนุมัติแล้ว + งวดนี้)
            // แล้วหักส่วนที่งวดก่อนหักไปแล้ว — บริษัทจ่ายเดือนละงวดตัวเลขทั้งสองเป็น 0 ผลเท่าเดิม
            var priorMonthSsoBase = sameMonthSsoBaseByEmp.GetValueOrDefault(emp.id);
            var priorMonthSso = sameMonthSsoByEmp.GetValueOrDefault(emp.id);
            var ssoAmount = Math.Max(0m,
                SocialSecurityCalculator.Calculate(ssoWageBase + priorMonthSsoBase, ssoRate, ssoCap) - priorMonthSso);
            // ฝั่งนายจ้าง (audit M10): อัตรานายจ้างจาก config (ว่าง = เท่าลูกจ้าง) ฐานและเพดานเดือนเดียวกัน — ไม่ขึ้นสลิป แต่ลงบัญชีและนำส่ง
            var ssoCompanyAmount = Math.Max(0m,
                SocialSecurityCalculator.Calculate(ssoWageBase + priorMonthSsoBase, ssoEmployerRate, ssoCap) - sameMonthSsoCompanyByEmp.GetValueOrDefault(emp.id));
            if (ssoAmount != 0)
                lineItems.Add(NewLine(payItemTypes["SSO"], PayLineSourceType.SocialSecurity, ssoAmount, -1, ++seq, null, null,
                    $"{ssoRate:0.##}% ของฐานค่าจ้างประกันสังคม {ssoWageBase:N2} (เฉพาะรายการที่ตั้งธง \"ฐาน SSO\" ในแค็ตตาล็อก; เพดาน {ssoCap:N2}) = {ssoAmount:N2}"));

            // เปลี่ยนอัตรากลางงวด: ใช้แถวที่มีผลล่าสุด ไม่ใช่แถวแรกที่ฐานข้อมูลคืนมา (audit M-08)
            var election = pfElections.Where(pe => pe.HremployeeId == emp.id)
                .OrderByDescending(pe => pe.EffectiveFrom).ThenByDescending(pe => pe.Id).FirstOrDefault();
            var pfEmployeeRate = election?.EmployeeContributionRate ?? emp.ProvfEmprate ?? 0m;
            var pfCompanyRate = election?.CompanyContributionRate ?? emp.ProvfCorprate ?? 0m;
            // ฐานกองทุนสำรองเลี้ยงชีพ = "ค่าจ้าง" ตามธง IsProvidentFundWageBase ในแค็ตตาล็อก (ค่าเริ่มต้น: เงินเดือน/ค่าจ้างรายวัน)
            // ไม่ใช่เงินได้รวม OT/สวัสดิการ (audit M2) — เดิมเดือนที่มี OT หักสะสมพนักงานและสมทบบริษัทเกิน
            var pfWageBase = (payItemTypes["BASE"].IsProvidentFundWageBase ? Math.Max(0m, baseSalary - attendanceDeduction) : 0m)
                           + (payItemTypes["OT"].IsProvidentFundWageBase ? otAmount : 0m)
                           + pfWageBaseAllowance + adhocPfWage;
            var pf = ProvidentFundCalculator.Calculate(pfWageBase, pfEmployeeRate, pfCompanyRate);
            if (pf.EmployeeAmount != 0)
                lineItems.Add(NewLine(payItemTypes["PF"], PayLineSourceType.ProvidentFund, pf.EmployeeAmount, -1, ++seq, "Pay_ProvidentFundElection", election?.Id,
                    $"อัตราสะสมพนักงาน {pfEmployeeRate:0.##}% × ค่าจ้างฐานกองทุน {pfWageBase:N2} (เฉพาะรายการที่ตั้งธง \"ฐานกองทุน\" ในแค็ตตาล็อก) = {pf.EmployeeAmount:N2} (บริษัทสมทบ {pfCompanyRate:0.##}% = {pf.CompanyAmount:N2})"));

            var empInsuranceEnrollments = !takeMonthlyItems
                ? new List<Pay_EmployeeInsuranceEnrollment>()   // เบี้ยรายเดือน: หักแล้วในรอบปกติ/งวดก่อนของเดือนนี้
                : insuranceEnrollments.Where(e => e.HremployeeId == emp.id).ToList();
            var insuranceEmployeeAmount = empInsuranceEnrollments.Sum(e => e.EmployeeAmount);
            var insuranceCompanyAmount = empInsuranceEnrollments.Sum(e => e.CompanyAmount);
            if (insuranceEmployeeAmount != 0)
                lineItems.Add(NewLine(payItemTypes["INSURANCE"], PayLineSourceType.Insurance, insuranceEmployeeAmount, -1, ++seq, "Pay_EmployeeInsuranceEnrollment", null,
                    $"รวมเบี้ยประกันกลุ่มที่พนักงานสมทบจาก {empInsuranceEnrollments.Count} กรมธรรม์ = {insuranceEmployeeAmount:N2} (บริษัทสมทบ {insuranceCompanyAmount:N2})"));

            var welfareFundEmployeeAmount = 0m;
            var welfareFundCompanyAmount = 0m;
            // กองทุนสงเคราะห์ลูกจ้าง (audit M-07): ยกเว้นเฉพาะคนที่เป็นสมาชิกกองทุนสำรองเลี้ยงชีพจริง (สะสมอยู่) ไม่ใช่ยกเว้นทั้งบริษัท
            // ที่มีกองทุน · ฐาน = ค่าจ้าง นิยามเดียวกับประกันสังคม (ไม่รวม OT/โบนัส หลังหักขาด/สาย) · เพดานเป็นรายเดือน
            // (งวดที่ 2 ของเดือนหักเฉพาะส่วนที่ยังไม่ถึงเพดาน) · ไม่คิดในรอบเสริม/โบนัส
            var isPvdMember = providentFundPolicy is not null && pfEmployeeRate > 0m;
            if (welfareFundPolicy is not null && !isPvdMember && !supplementary)
            {
                var wfMonthBase = ssoWageBase + priorMonthSsoBase;
                var wf = WelfareFundCalculator.Calculate(wfMonthBase, welfareFundPolicy.EmployeeContributionRate, welfareFundPolicy.CompanyContributionRate, welfareFundPolicy.WageCapPerMonth);
                welfareFundEmployeeAmount = Math.Max(0m, wf.EmployeeAmount - sameMonthWfByEmp.GetValueOrDefault(emp.id));
                welfareFundCompanyAmount = Math.Max(0m, wf.CompanyAmount - sameMonthWfCompanyByEmp.GetValueOrDefault(emp.id));
                if (welfareFundEmployeeAmount != 0)
                    lineItems.Add(NewLine(payItemTypes["WELFAREFUND"], PayLineSourceType.WelfareFund, welfareFundEmployeeAmount, -1, ++seq, "Pay_WelfareFundPolicy", welfareFundPolicy.Id,
                        $"อัตราสะสมพนักงาน {welfareFundPolicy.EmployeeContributionRate:0.##}% ของค่าจ้างทั้งเดือน {wfMonthBase:N2} (เพดาน {welfareFundPolicy.WageCapPerMonth?.ToString("N2") ?? "ไม่กำหนด"}) = {welfareFundEmployeeAmount:N2}"));
            }

            var loanAmount = 0m;
            if (takeMonthlyItems && !string.IsNullOrWhiteSpace(emp.RefMembno))
            {
                var loanDetails = loanByMember.GetValueOrDefault(emp.RefMembno!) ?? new List<Kptempreceivedet>();
                loanAmount = LoanDeductionCalculator.SumAmount(loanDetails);
                if (loanAmount != 0)
                    lineItems.Add(NewLine(payItemTypes["LOAN"], PayLineSourceType.Loan, loanAmount, -1, ++seq, "KPTEMPRECEIVEDET", null,
                        $"หักเงินกู้สหกรณ์ตามรายการที่บันทึกไว้ (KPTEMPRECEIVEDET) ในงวดนี้ = {loanAmount:N2}"));
            }

            // HR-entered company loans (Pay_EmployeeLoan) — separate pathway
            // from the cooperative KPTEMPRECEIVE loan above; an employee
            // could have both types of deduction in the same period.
            var empLoanInstallments = supplementary || !installmentsByEmployee.TryGetValue(emp.id, out var instRows)
                ? new List<Pay_EmployeeLoanInstallment>()   // งวดผ่อนหักในรอบปกติแล้ว
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
            loanAmount += LoanDeductionCalculator.SumEmployeeLoanAmount(empLoanInstallments);

            // HR-entered ad-hoc items (bonus, commission, ad-hoc deduction, etc.)
            // approved and targeting this exact period. Query includes items
            // already consumed by THIS run so recalculation re-picks them up
            // idempotently rather than losing them.
            var adhocItems = adhocByEmployee.TryGetValue(emp.id, out var adhocRows) ? adhocRows : new List<Pay_AdhocPayItem>();

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
                    // TaxableBasis = Amount หลังหักส่วนยกเว้น/ค่าใช้จ่าย (ค่าชดเชยเลิกจ้าง H-01) — รายการอื่น = Amount เท่าเดิม
                    var basis = adhoc.TaxableBasis;
                    adhocTaxableEarnings += basis;
                    adhocNonTaxableEarnings += adhoc.Amount - basis;
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

            // หนึ่งคนมีได้หลายแถวต่อปี (นายจ้างเดิม + ยอดยกมาของบริษัทนี้) — ต้องรวมทั้งหมด ห้ามเลือกแค่แถวเดียว
            var priorEmployerIncome = CombinePriorIncome(priorEmployerIncomes.Where(p => p.HremployeeId == emp.id).ToList());
            var (ytdIncome, ytdDeduction, ytdTax, ytdProvidentFund) = FoldYtd(ytdByEmployee.GetValueOrDefault(emp.id), run.PeriodStart, includeSamePeriod: supplementary, priorEmployerIncome, run.PayDate);

            // ลดหย่อนที่พนักงานแจ้ง (ล.ย.01) คิดตามวิธีของแต่ละรายการในปีนั้น (TaxDeductionRules): จำนวนตายตัว / ต่อคน / % ของเงินได้
            // + เพดานกลุ่ม — เงินได้ทั้งปีที่ใช้คิด % = สะสม + งวดนี้ × งวดที่เหลือ (เงินได้ครั้งเดียวนับครั้งเดียว)
            regularRowsThisPeriod.TryGetValue(emp.id, out var regularForProjection);
            var projectedAnnualIncome = supplementary
                ? ytdIncome + taxableGrossThisPeriod + regularForProjection.Gross * ((decimal)schedule.PeriodsPerMonth / Math.Max(1, regularForProjection.Terms)) * schedule.RemainingMonthsAfterThis
                : ytdIncome + (taxableGrossThisPeriod - adhocTaxableEarnings) * schedule.RemainingPeriodsIncludingThis + adhocTaxableEarnings;
            var empElections = monthlyTaxElections.Where(e => e.HremployeeId == emp.id)
                .Select(e => new TaxDeductionRules.Election(e.Pay_TaxDeductionType.SortOrder, e.Pay_TaxDeductionType.Code, e.Pay_TaxDeductionType.NameTh,
                    e.Pay_TaxDeductionType.CalcMethod, e.AnnualAmount, e.PersonCount, e.Pay_TaxDeductionType.MaxAmountPerYear,
                    e.Pay_TaxDeductionType.AmountPerPerson, e.Pay_TaxDeductionType.MaxPersons, e.Pay_TaxDeductionType.PercentCap,
                    e.Pay_TaxDeductionType.PercentBase, e.Pay_TaxDeductionType.CapGroup, e.Pay_TaxDeductionType.GroupCapPerYear))
                .ToList();
            var (incomeBasedItems, retirementElected) = TaxDeductionRules.ResolveIncomeBased(empElections, projectedAnnualIncome);

            // เงินสะสมกองทุนลดหย่อนภาษีได้ไม่เกิน 15% ของค่าจ้าง และรวมกับ RMF/SSF/ประกันบำนาญที่แจ้งไว้ไม่เกิน 500,000 บาท/ปี
            // (audit M-02) — ส่วนเกินยังหักเข้ากองทุน แต่ไม่ลดฐานภาษี
            var pvdRoom = ProvidentFundTaxDeduction.AnnualRoom(providentFundDeductionCap, retirementElected, ytdProvidentFund);
            var pfDeductible = ProvidentFundTaxDeduction.Deductible(pf.EmployeeAmount, pfWageBase, pvdRoom);
            var thisPeriodFlatDeduction = ssoAmount + pfDeductible;

            // เงินบริจาค: ไม่เกิน 10% ของเงินได้หลังหักค่าใช้จ่ายและลดหย่อนอื่นทั้งหมด (ประมาณการทั้งปี)
            var projectedExpense = Math.Min(Math.Round(projectedAnnualIncome * expenseDeductionRate, 2, MidpointRounding.AwayFromZero), expenseDeductionCap);
            var projectedFlat = ytdDeduction + thisPeriodFlatDeduction * Math.Max(1, schedule.RemainingPeriodsIncludingThis);
            var netBasedItems = TaxDeductionRules.ResolveNetBased(empElections,
                projectedAnnualIncome - projectedExpense - personalAllowancePerYear - incomeBasedItems.Sum(i => i.Amount) - projectedFlat);
            var electedAnnualDeduction = incomeBasedItems.Sum(i => i.Amount) + netBasedItems.Sum(i => i.Amount);
            // รายการหักรายเดือนที่คูณเดือนที่เหลือ = เฉพาะประกันสังคม+กองทุน (audit M1) ส่วนลดหย่อนส่วนตัว
            // และรายการที่พนักงานแจ้งเป็น "รายปี" ได้เต็มไม่ว่าเข้างานเดือนไหน — นับครั้งเดียวใน annualFixedDeduction
            // (รอบเสริมทั้งสองเป็น 0 อยู่แล้ว เพราะ SSO/PF ไม่คิดในรอบเสริม และลดหย่อนรายปีถูกใช้ผ่านฐานรอบปกติ)
            var annualFixedDeduction = supplementary ? 0m : personalAllowancePerYear + electedAnnualDeduction;
            decimal monthlyTax;
            TaxBracketCalculator.TaxCalculationResult annualCalc;
            if (supplementary)
            {
                // ภาษีโบนัสแบบส่วนต่าง: ฐานประมาณการทั้งปี = สะสม (รวมงวดนี้แล้ว) + เงินเดือนงวดนี้ × เดือนที่เหลือ
                regularRowsThisPeriod.TryGetValue(emp.id, out var regularRow);
                // เงินเดือนของรอบปกติงวดเดียวกันแปลงเป็นรายเดือนก่อน: Σ งวดที่อนุมัติแล้ว × (งวด/เดือน ÷ งวดที่มีแล้ว)
                // งวดเดียวของบริษัท 2 งวด → × 2; ครบสองงวดแล้ว → × 1; บริษัทเดือนละงวด → × 1 เหมือนเดิม
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
                // ประมาณการรายการหักคงที่ที่เหลือทั้งปีจาก "ประกันสังคมทั้งเดือน" ไม่ใช่ "งวดนี้ × จำนวนงวด" — เพดาน 750 เป็นรายเดือน
                // (บริษัทจ่ายครึ่งเดือน: งวด 1 หัก 750 งวด 2 หัก 0) ฐานทั้งเดือน = ฐานที่หักไปแล้วในเดือนนี้ + งวดนี้ ปรับตามงวดที่ผ่านมา
                // บริษัทจ่ายเดือนละงวดตัวเลขเท่าสูตรเดิมทุกประการ
                var monthBaseProjected = schedule.PeriodsPerMonth > 1
                    ? (priorMonthSsoBase + ssoWageBase) * schedule.PeriodsPerMonth / schedule.TermNo
                    : ssoWageBase;
                var ssoMonthlyProjected = SocialSecurityCalculator.Calculate(monthBaseProjected, ssoRate, ssoCap);
                var termsLeftThisMonth = schedule.PeriodsPerMonth - schedule.TermNo;
                var monthsAfterThis = Math.Max(0m, schedule.RemainingMonthsIncludingThis - (termsLeftThisMonth + 1) * schedule.MonthFraction);
                var restOfMonthSso = Math.Max(0m, ssoMonthlyProjected - priorMonthSso - ssoAmount);
                // กองทุนที่ประมาณการต่อถึงสิ้นปีก็ต้องไม่เกินเพดานที่เหลือ — เดิมคูณงวดที่เหลือตรง ๆ ต้นปีภาษีจึงหักขาดสำหรับคนเงินเดือนสูง
                var futurePvd = Math.Min(pfDeductible * (termsLeftThisMonth + schedule.PeriodsPerMonth * monthsAfterThis),
                    Math.Max(0m, pvdRoom - pfDeductible));
                var projectedRemainingFlat = leavesThisPeriod
                    ? thisPeriodFlatDeduction   // no later periods for a leaver
                    : thisPeriodFlatDeduction + restOfMonthSso + ssoMonthlyProjected * monthsAfterThis + futurePvd;

                // รายการเฉพาะกิจที่ต้องเสียภาษี (ค่าคอมฯ/โบนัสที่จ่ายในรอบปกติ) = เงินได้ครั้งเดียว คิดภาษีแบบส่วนต่าง ไม่คูณเดือนที่เหลือ
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
            payEmp.TaxableIncome = taxableGrossThisPeriod;   // 50 ทวิ / ภ.ง.ด.1 อ่านจากตรงนี้ ไม่ต้องย้อนคำนวณจาก gross
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
            processedCount++;

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
                        ElectedDeductionItems = incomeBasedItems.Concat(netBasedItems).Select(i => i.Note).ToList(),
                        ProjectedAnnualIncomeForDeductions = projectedAnnualIncome,
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

        return new PayrollRunCalculationSummary(processedCount, negativeCount, totalNet);
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
    // PayDate = วันที่จ่ายจริง (ลำดับตามเกณฑ์เงินสด) · null = ใช้ PeriodStart แทน (ผลเหมือนก่อนมีเกณฑ์วันจ่าย)
    public sealed record YtdRow(DateOnly PeriodStart, decimal Income, decimal FlatDeduction, decimal Tax, decimal ProvidentFund, DateOnly? PayDate = null);

    // (audit M14) หนึ่ง query ต่อรอบ: แถวเงินเดือนที่อนุมัติแล้วของทุกคนในบริษัทตั้งแต่ต้นปีถึงงวดนี้ แล้วค่อยพับต่อคนในลูป
    // ยอดสะสมนับเฉพาะรอบที่ "อนุมัติแล้ว" (audit H3) — รอบที่ยังแก้ได้ไม่ใช่ข้อเท็จจริง
    private static async Task<Dictionary<long, List<YtdRow>>> LoadYtdRowsAsync(HRMContext context, Pay_PayrollRun run, CancellationToken ct)
    {
        // ปีภาษีตามวันจ่าย (audit M-01) — FoldYtd คัดว่าแถวไหนจ่ายก่อนงวดนี้
        var yearStart = new DateOnly(run.PayDate.Year, 1, 1);
        // YTD = only money actually paid: final runs, excluded employees left out (audit C-05)
        var rows = await context.Pay_PayrollEmployees
            .Where(PayrollRunFilters.RowWasPaid)
            .Where(e => e.CompanyId == run.CompanyId
                        && e.PayrollRunId != run.Id
                        && e.Pay_PayrollRun.PayDate >= yearStart
                        && e.Pay_PayrollRun.PayDate <= run.PayDate)
            .Select(e => new { e.HremployeeId, e.Pay_PayrollRun.PeriodStart, e.Pay_PayrollRun.PayDate, e.TaxableIncome, e.SocialSecurityAmount, e.ProvidentFundEmployeeAmount, e.TaxAmount })
            .ToListAsync(ct);
        // ยอดสะสมใช้ "เงินได้พึงประเมิน" ของแต่ละงวด (TaxableIncome) ไม่ใช่รายรับรวม (GrossEarnings) — พบจากเทสทั้งปี 2568:
        // รายรับรวมยังไม่หักขาดงาน/มาสาย และรวมรายการที่ไม่ต้องเสียภาษี (เช่น เบิกคืนค่าใช้จ่าย) ทำให้ประมาณการทั้งปีสูงเกินจริง
        var result = rows
            .GroupBy(r => r.HremployeeId)
            .ToDictionary(g => g.Key, g => g
                .Select(r => new YtdRow(r.PeriodStart, r.TaxableIncome, r.SocialSecurityAmount + r.ProvidentFundEmployeeAmount, r.TaxAmount, r.ProvidentFundEmployeeAmount, r.PayDate))
                .ToList());

        // ยอดยกมา: months this company paid from its previous system before going live mid-year are
        // this employer's own pay, so they count exactly like a paid run of that month
        // (ported from Advance.Payroll, CEO order 22 ก.ย. 2569: mirror the payroll domain)
        var openings = await context.Pay_EmployeeOpeningBalances
            .Where(o => o.CompanyId == run.CompanyId && o.IsActive
                        && o.TaxYear == run.PayDate.Year && o.Month <= run.PayDate.Month)
            .Select(o => new { o.HremployeeId, o.TaxYear, o.Month, o.TaxableIncome, o.SsoEmployee, o.PvdEmployee, o.TaxWithheld })
            .ToListAsync(ct);
        foreach (var o in openings)
        {
            if (!result.TryGetValue(o.HremployeeId, out var list)) result[o.HremployeeId] = list = [];
            var openingMonth = new DateOnly(o.TaxYear, o.Month, 1);
            list.Add(new YtdRow(openingMonth, o.TaxableIncome, o.SsoEmployee + o.PvdEmployee, o.TaxWithheld, o.PvdEmployee, openingMonth.AddMonths(1).AddDays(-1)));
        }
        return result;
    }

    // includeSamePeriod = รอบเสริม (โบนัส) ต้องนับรอบปกติของงวดเดียวกันด้วย — pure, ทดสอบได้
    public static (decimal YtdIncome, decimal YtdDeduction, decimal YtdTax, decimal YtdProvidentFund) FoldYtd(
        IReadOnlyList<YtdRow>? rows, DateOnly periodStart, bool includeSamePeriod, Pay_EmployeePriorEmployerIncome? priorEmployerIncome,
        DateOnly? payDate = null)
    {
        // จ่ายก่อนงวดนี้ = วันจ่ายก่อนหน้า · วันจ่ายเดียวกัน = ตัดสินด้วยงวด (รอบเสริมนับรอบปกติของงวดเดียวกันด้วย)
        var anchorPay = payDate ?? periodStart;
        bool PaidBefore(YtdRow r)
        {
            var rp = r.PayDate ?? r.PeriodStart;
            if (rp != anchorPay) return rp < anchorPay;
            return includeSamePeriod ? r.PeriodStart <= periodStart : r.PeriodStart < periodStart;
        }
        var prior = (rows ?? Array.Empty<YtdRow>()).Where(PaidBefore).ToList();
        var folded = FoldPriorEmployerIncome(prior.Sum(r => r.Income), prior.Sum(r => r.FlatDeduction), prior.Sum(r => r.Tax), priorEmployerIncome);
        return (folded.YtdIncome, folded.YtdDeduction, folded.YtdTax, prior.Sum(r => r.ProvidentFund));
    }

    // Pure and unit-testable on purpose (mirrors TaxBracketCalculator's own
    // separation of math from EF orchestration) — folds a mid-year hire's
    // prior-employer income/deduction/tax-withheld (from
    // Pay_EmployeePriorEmployerIncome, entered once from the certificate the
    // employee brings in) into this company's own YTD accumulators, so the
    // withholding projection reflects the employee's TRUE annual income.
    public static Pay_EmployeePriorEmployerIncome? CombinePriorIncome(IReadOnlyList<Pay_EmployeePriorEmployerIncome> rows)
    {
        if (rows.Count == 0) return null;
        if (rows.Count == 1) return rows[0];
        return new Pay_EmployeePriorEmployerIncome
        {
            HremployeeId = rows[0].HremployeeId,
            TaxYear = rows[0].TaxYear,
            PriorEmployerName = string.Join(" + ", rows.Select(r => r.PriorEmployerName ?? "-")),
            IncomeAmount = rows.Sum(r => r.IncomeAmount),
            DeductionAmount = rows.Sum(r => r.DeductionAmount),
            TaxWithheldAmount = rows.Sum(r => r.TaxWithheldAmount),
        };
    }

    public static (decimal YtdIncome, decimal YtdDeduction, decimal YtdTax) FoldPriorEmployerIncome(
        decimal ytdIncome, decimal ytdDeduction, decimal ytdTax, Pay_EmployeePriorEmployerIncome? prior)
        => prior is null
            ? (ytdIncome, ytdDeduction, ytdTax)
            : (ytdIncome + prior.IncomeAmount, ytdDeduction + prior.DeductionAmount, ytdTax + prior.TaxWithheldAmount);
}
