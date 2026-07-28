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
}
