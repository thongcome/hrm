namespace HRM.Services.Pay;

using System.Text.Json;
using HRM.Models;
using HRM.Services.Pay.Calculators;
using Microsoft.EntityFrameworkCore;

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

    public PayrollCalculationService(
        IDbContextFactory<HRMContext> dbFactory,
        ISocialSecurityRateProvider socialSecurityRateProvider,
        OvertimeEarningsCalculator overtimeCalculator,
        LoanDeductionCalculator loanCalculator)
    {
        _dbFactory = dbFactory;
        _socialSecurityRateProvider = socialSecurityRateProvider;
        _overtimeCalculator = overtimeCalculator;
        _loanCalculator = loanCalculator;
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
            context.Pay_PayrollEmployees.RemoveRange(
                context.Pay_PayrollEmployees.Where(e => e.PayrollRunId == payrollRunId));
            await context.SaveChangesAsync(ct);
        }

        var payItemTypes = await context.Pay_PayItemTypes.ToDictionaryAsync(t => t.Code, ct);
        var taxBrackets = await context.Pay_TaxBrackets
            .Where(b => b.EffectiveYear == run.PeriodStart.Year && b.IsActive)
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
            lineItems.Add(NewLine(payItemTypes["BASE"], PayLineSourceType.Base, baseSalary, 1, ++seq, "HREMPLOYEE", emp.id));

            var otRecords = await _overtimeCalculator.GetOvertimeForPeriodAsync(emp.companyid, emp.EmpNo, run.PeriodStart, run.PeriodEnd, ct);
            var otAmount = OvertimeEarningsCalculator.SumAmount(otRecords);
            if (otAmount != 0)
                lineItems.Add(NewLine(payItemTypes["OT"], PayLineSourceType.Overtime, otAmount, 1, ++seq, "HRW_OT", null));

            var grossEarnings = baseSalary + otAmount;

            var ssoAmount = SocialSecurityCalculator.Calculate(grossEarnings, ssoRate, ssoCap);
            if (ssoAmount != 0)
                lineItems.Add(NewLine(payItemTypes["SSO"], PayLineSourceType.SocialSecurity, ssoAmount, -1, ++seq, null, null));

            var election = pfElections.FirstOrDefault(pe => pe.HremployeeId == emp.id);
            var pfEmployeeRate = election?.EmployeeContributionRate ?? emp.ProvfEmprate ?? 0m;
            var pfCompanyRate = election?.CompanyContributionRate ?? emp.ProvfCorprate ?? 0m;
            var pf = ProvidentFundCalculator.Calculate(grossEarnings, pfEmployeeRate, pfCompanyRate);
            if (pf.EmployeeAmount != 0)
                lineItems.Add(NewLine(payItemTypes["PF"], PayLineSourceType.ProvidentFund, pf.EmployeeAmount, -1, ++seq, null, null));

            var empInsuranceEnrollments = insuranceEnrollments.Where(e => e.HremployeeId == emp.id).ToList();
            var insuranceEmployeeAmount = empInsuranceEnrollments.Sum(e => e.EmployeeAmount);
            var insuranceCompanyAmount = empInsuranceEnrollments.Sum(e => e.CompanyAmount);
            if (insuranceEmployeeAmount != 0)
                lineItems.Add(NewLine(payItemTypes["INSURANCE"], PayLineSourceType.Insurance, insuranceEmployeeAmount, -1, ++seq, "Pay_EmployeeInsuranceEnrollment", null));

            var loanAmount = 0m;
            if (!string.IsNullOrWhiteSpace(emp.RefMembno))
            {
                var loanDetails = await _loanCalculator.GetLoanDeductionsForPeriodAsync(emp.companyid, emp.RefMembno, run.PayrollPeriod, ct);
                loanAmount = LoanDeductionCalculator.SumAmount(loanDetails);
                if (loanAmount != 0)
                    lineItems.Add(NewLine(payItemTypes["LOAN"], PayLineSourceType.Loan, loanAmount, -1, ++seq, "KPTEMPRECEIVEDET", null));
            }

            // HR-entered company loans (Pay_EmployeeLoan) — separate pathway
            // from the cooperative KPTEMPRECEIVE loan above; an employee
            // could have both types of deduction in the same period.
            var empLoanInstallments = await LoanDeductionCalculator.GetEmployeeLoanInstallmentsForPeriodAsync(context, emp.id, run.PayrollPeriod, run.Id, ct);
            foreach (var installment in empLoanInstallments)
            {
                lineItems.Add(NewLine(payItemTypes["LOAN"], PayLineSourceType.Loan, installment.Amount, -1, ++seq, "Pay_EmployeeLoanInstallment", installment.Id));
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
                lineItems.Add(NewLine(adhoc.Pay_PayItemType, PayLineSourceType.Adjustment, adhoc.Amount, signFlag, ++seq, "Pay_AdhocPayItem", adhoc.Id));

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

            var (ytdIncome, ytdDeduction, ytdTax) = await GetYtdAccumulatorsAsync(context, emp.id, run, ct);
            var (monthlyTax, annualCalc) = TaxBracketCalculator.CalculateMonthlyWithholding(
                ytdIncome, taxableGrossThisPeriod, ytdDeduction, 0m, remainingPeriods, ytdTax, taxBrackets);
            if (monthlyTax != 0)
                lineItems.Add(NewLine(payItemTypes["TAX"], PayLineSourceType.Tax, monthlyTax, -1, ++seq, null, null));

            var totalDeductions = ssoAmount + pf.EmployeeAmount + insuranceEmployeeAmount + loanAmount + adhocDeductions + monthlyTax;
            var netPayResult = NetPayGuardService.Ensure(grossEarnings - totalDeductions);

            payEmp.GrossEarnings = grossEarnings;
            payEmp.TotalDeductions = totalDeductions;
            payEmp.NetPay = netPayResult.AdjustedNetPay;
            payEmp.TaxAmount = monthlyTax;
            payEmp.SocialSecurityAmount = ssoAmount;
            payEmp.ProvidentFundEmployeeAmount = pf.EmployeeAmount;
            payEmp.ProvidentFundCompanyAmount = pf.CompanyAmount;
            payEmp.InsuranceEmployeeAmount = insuranceEmployeeAmount;
            payEmp.InsuranceCompanyAmount = insuranceCompanyAmount;
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
                    RemainingPeriods = remainingPeriods,
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

        return new PayrollRunCalculationSummary(eligibleEmployees.Count, negativeCount, totalNet);
    }

    private static Pay_PayrollLineItem NewLine(Pay_PayItemType itemType, PayLineSourceType sourceType, decimal amount, int signFlag, int seq, string? sourceRefTable, long? sourceRefId)
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
        };
    }

    // YTD figures are derived from previously-calculated Pay_PayrollEmployee rows
    // in the same calendar year rather than a separate accumulator table.
    private static async Task<(decimal YtdIncome, decimal YtdDeduction, decimal YtdTax)> GetYtdAccumulatorsAsync(
        HRMContext context, long hremployeeId, Pay_PayrollRun run, CancellationToken ct)
    {
        var yearStart = new DateOnly(run.PeriodStart.Year, 1, 1);

        var priorRows = await context.Pay_PayrollEmployees
            .Include(e => e.Pay_PayrollRun)
            .Where(e => e.HremployeeId == hremployeeId
                        && e.Pay_PayrollRun.PeriodStart >= yearStart
                        && e.Pay_PayrollRun.PeriodStart < run.PeriodStart
                        && e.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled)
            .ToListAsync(ct);

        return (priorRows.Sum(r => r.GrossEarnings), 0m, priorRows.Sum(r => r.TaxAmount));
    }
}
