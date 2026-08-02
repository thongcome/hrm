namespace HRM.Services.Pay.Calculators;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Fixes PayrollProcess.razor GetLoan (Components\Pages\Payroll\PayrollProcess.razor
// ~line 1512): the legacy query was `Kptempreceives.Where(x => x.MemberNo == refMemNo)`
// with NO RecvPeriod filter, despite Kptempreceive.RecvPeriod existing specifically to
// represent "which collection period this loan bill belongs to" — so the same loan
// installment was re-deducted on every subsequent payroll run. This scopes strictly
// to the requested collection period.
public class LoanDeductionCalculator
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public LoanDeductionCalculator(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Kptempreceivedet>> GetLoanDeductionsForPeriodAsync(string companyId, string memberNo, string recvPeriod, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var kpSlipNos = await context.Kptempreceives
            .Where(h => h.companyid == companyId && h.MemberNo == memberNo && h.RecvPeriod == recvPeriod)
            .Select(h => h.KpslipNo)
            .ToListAsync(ct);

        if (kpSlipNos.Count == 0) return new List<Kptempreceivedet>();

        return await context.Kptempreceivedets
            .Where(d => d.companyid == companyId && kpSlipNos.Contains(d.KpslipNo))
            .ToListAsync(ct);
    }

    public static decimal SumAmount(IEnumerable<Kptempreceivedet> details) => details.Sum(x => x.ItemPayment ?? 0m);

    // Pay_EmployeeLoan installments (the new HR-proxy company-loan pathway —
    // see Pay_EmployeeLoan.cs) — separate from the cooperative KPTEMPRECEIVE
    // pathway above; an employee could in principle have both. Includes rows
    // already Consumed by THIS run so recalculating a Draft/Calculated run
    // re-picks them up idempotently instead of losing them, same shape as
    // Pay_AdhocPayItem's status query in PayrollCalculationService.
    //
    // Takes the CALLER's HRMContext (unlike GetLoanDeductionsForPeriodAsync
    // above, which is read-only) — PayrollCalculationService mutates the
    // returned rows' Status/ConsumedByPayrollRunId and the owning loan's
    // RemainingBalance/Status, then calls SaveChangesAsync on its own
    // context; a separate context here would silently lose those writes.
    public static async Task<List<Pay_EmployeeLoanInstallment>> GetEmployeeLoanInstallmentsForPeriodAsync(HRMContext context, long hremployeeId, string period, long currentRunId, CancellationToken ct = default)
    {
        return await context.Pay_EmployeeLoanInstallments
            .Include(i => i.Pay_EmployeeLoan)
            .Where(i => i.Pay_EmployeeLoan.HremployeeId == hremployeeId
                && i.Period == period
                && (i.Status == Pay_LoanInstallmentStatus.Pending
                    || (i.Status == Pay_LoanInstallmentStatus.Consumed && i.ConsumedByPayrollRunId == currentRunId)))
            .ToListAsync(ct);
    }

    public static decimal SumEmployeeLoanAmount(IEnumerable<Pay_EmployeeLoanInstallment> installments) => installments.Sum(x => x.Amount);
}
