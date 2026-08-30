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

    public async Task<PayrollRunCalculationSummary> CalculateAsync(long payrollRunId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == payrollRunId, ct)
            ?? throw new InvalidOperationException($"Pay_PayrollRun {payrollRunId} not found.");

        if (run.Status != PayrollRunStatus.Draft && run.Status != PayrollRunStatus.Calculated)
            throw new InvalidOperationException($"Cannot calculate a run in status {run.Status}. Only Draft or Calculated runs can be (re)calculated.");

        // idempotent while unlocked: wipe any existing employee/line-item rows for this run first
        var existingEmployeeIds = await context.Pay_PayrollEmployees
            .Where(e => e.PayrollRunId == payrollRunId)
            .Select(e => e.Id)
            .ToListAsync(ct);

        if (existingEmployeeIds.Count > 0)
        {
            context.Pay_PayrollLineItems.RemoveRange(
                context.Pay_PayrollLineItems.Where(li => existingEmployeeIds.Contains(li.PayrollEmployeeId)));
            // Pay_PayrollAuditLog.PayrollEmployeeId is a Restrict FK (deliberately, to
            // avoid SQL Server's "multiple cascade paths" error against the direct
            // Run->AuditLog cascade) — it must be cleared explicitly before the
            // employee rows can be deleted, or this throws a DbUpdateException.
            context.Pay_PayrollAuditLogs.RemoveRange(
                context.Pay_PayrollAuditLogs.Where(a => a.PayrollEmployeeId != null && existingEmployeeIds.Contains(a.PayrollEmployeeId.Value)));
            // Pay_PayrollAnomaly.PayrollEmployeeId is also a Restrict FK for the same
            // reason as the audit log above — clear it before the employee rows can be
            // deleted. DetectAnomaliesAsync() re-detects and re-inserts fresh anomaly
            // rows against the new Pay_PayrollEmployee rows later in this method.
            context.Pay_PayrollAnomalies.RemoveRange(
                context.Pay_PayrollAnomalies.Where(a => a.PayrollEmployeeId != null && existingEmployeeIds.Contains(a.PayrollEmployeeId.Value)));
            context.Pay_PayrollEmployees.RemoveRange(
                context.Pay_PayrollEmployees.Where(e => e.PayrollRunId == payrollRunId));
            await context.SaveChangesAsync(ct);
        }

        var payItemTypes = await context.Pay_PayItemTypes.ToDictionaryAsync(t => t.Code, ct);
        var taxBrackets = await context.Pay_TaxBrackets
            .Where(b => b.EffectiveYear == run.PeriodStart.Year && b.IsActive)
            .ToListAsync(ct);

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

        var (ssoRate, ssoCap) = await _socialSecurityRateProvider.GetCurrentRateAsync(run.CompanyId, ct);

        var periodEndDt = run.PeriodEnd.ToDateTime(TimeOnly.MaxValue);
        var periodStartDt = run.PeriodStart.ToDateTime(TimeOnly.MinValue);

        var eligibleEmployees = await context.Hremployee
            .Where(e => e.companyid == run.CompanyId
                        && e.WorkDate != null && e.WorkDate <= periodEndDt
                        && (e.ResignDate == null || e.ResignDate >= periodStartDt))
            .ToListAsync(ct);

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

        var negativeCount = 0;
        var totalNet = 0m;

        foreach (var emp in eligibleEmployees)
        {
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

            var baseSalary = Math.Round((emp.SalaryAmt ?? 0m) * proration.ProrationFactor, 2, MidpointRounding.AwayFromZero);
            lineItems.Add(NewLine(payItemTypes["BASE"], PayLineSourceType.Base, baseSalary, 1, ++seq, "HREMPLOYEE", emp.id,
                $"ฐานเงินเดือน {(emp.SalaryAmt ?? 0m):N2} × สัดส่วนวันทำงาน {proration.ActualWorkingDays}/{proration.WorkingDaysInPeriod} วัน ({proration.ProrationFactor:P2}) = {baseSalary:N2}"));

            var otRecords = await _overtimeCalculator.GetOvertimeForPeriodAsync(emp.companyid, emp.EmpNo, run.PeriodStart, run.PeriodEnd, ct);
            var otAmount = OvertimeEarningsCalculator.SumAmount(otRecords);
            if (otAmount != 0)
                lineItems.Add(NewLine(payItemTypes["OT"], PayLineSourceType.Overtime, otAmount, 1, ++seq, "HRW_OT", null,
                    $"รวมค่าล่วงเวลาจากรายการที่บันทึกไว้ {otRecords.Count} รายการในงวดนี้ = {otAmount:N2}"));

            var grossEarnings = baseSalary + otAmount;

            var ssoAmount = SocialSecurityCalculator.Calculate(grossEarnings, ssoRate, ssoCap);
            if (ssoAmount != 0)
                lineItems.Add(NewLine(payItemTypes["SSO"], PayLineSourceType.SocialSecurity, ssoAmount, -1, ++seq, null, null,
                    $"{ssoRate:0.##}% ของค่าจ้างที่คำนวณได้ {grossEarnings:N2} (เพดานฐานคำนวณ {ssoCap:N2}) = {ssoAmount:N2}"));

            var election = pfElections.FirstOrDefault(pe => pe.HremployeeId == emp.id);
            var pfEmployeeRate = election?.EmployeeContributionRate ?? emp.ProvfEmprate ?? 0m;
            var pfCompanyRate = election?.CompanyContributionRate ?? emp.ProvfCorprate ?? 0m;
            var pf = ProvidentFundCalculator.Calculate(grossEarnings, pfEmployeeRate, pfCompanyRate);
            if (pf.EmployeeAmount != 0)
                lineItems.Add(NewLine(payItemTypes["PF"], PayLineSourceType.ProvidentFund, pf.EmployeeAmount, -1, ++seq, "Pay_ProvidentFundElection", election?.Id,
                    $"อัตราสะสมพนักงาน {pfEmployeeRate:0.##}% × เงินได้ {grossEarnings:N2} = {pf.EmployeeAmount:N2} (บริษัทสมทบ {pfCompanyRate:0.##}% = {pf.CompanyAmount:N2})"));

            var empInsuranceEnrollments = insuranceEnrollments.Where(e => e.HremployeeId == emp.id).ToList();
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
            if (!string.IsNullOrWhiteSpace(emp.RefMembno))
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
            var empLoanInstallments = await LoanDeductionCalculator.GetEmployeeLoanInstallmentsForPeriodAsync(context, emp.id, run.PayrollPeriod, run.Id, ct);
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
            var taxableGrossThisPeriod = baseSalary + otAmount + adhocTaxableEarnings;

            var empMonthlyElections = monthlyTaxElections.Where(e => e.HremployeeId == emp.id).ToList();
            var electedMonthlyDeduction = empMonthlyElections.Sum(e => e.AnnualAmount) / 12m;
            var thisPeriodFlatDeduction = personalAllowancePerMonth + ssoAmount + pf.EmployeeAmount + electedMonthlyDeduction;

            var priorEmployerIncome = priorEmployerIncomes.FirstOrDefault(p => p.HremployeeId == emp.id);
            var (ytdIncome, ytdDeduction, ytdTax) = await GetYtdAccumulatorsAsync(context, emp.id, run, priorEmployerIncome, ct);
            var (monthlyTax, annualCalc) = TaxBracketCalculator.CalculateMonthlyWithholding(
                ytdIncome, taxableGrossThisPeriod, ytdDeduction, thisPeriodFlatDeduction,
                expenseDeductionRate, expenseDeductionCap, remainingPeriods, ytdTax, taxBrackets);
            if (monthlyTax != 0)
                lineItems.Add(NewLine(payItemTypes["TAX"], PayLineSourceType.Tax, monthlyTax, -1, ++seq, null, null,
                    "ภาษีหัก ณ ที่จ่ายประจำเดือน คำนวณจากเงินได้สะสมทั้งปีเทียบตารางอัตราภาษี — ดูรายละเอียดฉบับเต็มในหัวข้อ \"บันทึกการคำนวณภาษี\" ด้านล่าง"));

            var totalDeductions = ssoAmount + pf.EmployeeAmount + insuranceEmployeeAmount + welfareFundEmployeeAmount + loanAmount + adhocDeductions + monthlyTax;
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

        // Best-effort — anomaly detection is purely advisory and must never
        // stop a payroll run from being calculated. If ML.NET or a query in
        // here throws, log it and let the calculation stand.
        try
        {
            await _anomalyDetectionService.DetectAnomaliesAsync(payrollRunId, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anomaly detection failed for payroll run {PayrollRunId}", payrollRunId);
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
    private static async Task<(decimal YtdIncome, decimal YtdDeduction, decimal YtdTax)> GetYtdAccumulatorsAsync(
        HRMContext context, long hremployeeId, Pay_PayrollRun run, Pay_EmployeePriorEmployerIncome? priorEmployerIncome, CancellationToken ct)
    {
        var yearStart = new DateOnly(run.PeriodStart.Year, 1, 1);

        var priorRows = await context.Pay_PayrollEmployees
            .Include(e => e.Pay_PayrollRun)
            .Where(e => e.HremployeeId == hremployeeId
                        && e.Pay_PayrollRun.PeriodStart >= yearStart
                        && e.Pay_PayrollRun.PeriodStart < run.PeriodStart
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
