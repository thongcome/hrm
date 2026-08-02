using HRM.Models;
using HRM.Services.Pay.Calculators;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// HR-proxy company loan flow. Unlike SeveranceService (which rides the
// existing Pay_AdhocPayItem pipeline), a loan is a multi-period commitment
// with a running balance, so it gets its own header+detail tables — see
// Pay_EmployeeLoan.cs for why KPTEMPRECEIVE (the legacy cooperative
// loan/shares system) wasn't reused. PayrollCalculationService picks up
// installments via LoanDeductionCalculator, not this service directly.
public class EmployeeLoanService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public EmployeeLoanService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Pay_EmployeeLoan>> GetLoansForEmployeeAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Pay_EmployeeLoans
            .Include(l => l.Pay_EmployeeLoanInstallments)
            .Where(l => l.HremployeeId == hremployeeId)
            .OrderByDescending(l => l.RequestedDate)
            .ToListAsync(ct);
    }

    public async Task<long> SubmitAsync(long hremployeeId, decimal principal, int totalInstallments, string startPeriod, string? reason, long actorUserId, CancellationToken ct = default)
    {
        // Re-validated server-side (not just trusting a cached UI preview) —
        // the schedule math and the active-loan check both need a fresh read.
        var schedule = EmployeeLoanScheduleCalculator.Calculate(principal, totalInstallments, startPeriod);

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        var hasActiveLoan = await context.Pay_EmployeeLoans.AnyAsync(l =>
            l.HremployeeId == hremployeeId && l.Status == Pay_EmployeeLoanStatus.Active, ct);
        if (hasActiveLoan)
            throw new InvalidOperationException("พนักงานคนนี้มีเงินกู้ที่ยังไม่ปิดยอดอยู่แล้ว — ต้องรอให้ชำระหมดหรือยกเลิกก่อนยื่นใหม่");

        var loan = new Pay_EmployeeLoan
        {
            HremployeeId = hremployeeId,
            EmpNo = emp.EmpNo,
            CompanyId = emp.companyid,
            PrincipalAmount = principal,
            InstallmentAmount = schedule[0].Amount,
            TotalInstallments = totalInstallments,
            RemainingBalance = principal,
            StartPeriod = startPeriod,
            Reason = reason,
            Status = Pay_EmployeeLoanStatus.Active,
            RequestedByUserId = actorUserId,
        };
        context.Pay_EmployeeLoans.Add(loan);
        await context.SaveChangesAsync(ct);

        foreach (var line in schedule)
        {
            context.Pay_EmployeeLoanInstallments.Add(new Pay_EmployeeLoanInstallment
            {
                LoanId = loan.Id,
                InstallmentNo = line.InstallmentNo,
                Period = line.Period,
                Amount = line.Amount,
                BalanceAfter = line.BalanceAfter,
                Status = Pay_LoanInstallmentStatus.Pending,
            });
        }
        await context.SaveChangesAsync(ct);

        return loan.Id;
    }

    public async Task CancelAsync(long loanId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var loan = await context.Pay_EmployeeLoans
            .Include(l => l.Pay_EmployeeLoanInstallments)
            .FirstOrDefaultAsync(l => l.Id == loanId, ct)
            ?? throw new InvalidOperationException("ไม่พบเงินกู้");

        if (loan.Pay_EmployeeLoanInstallments.Any(i => i.Status == Pay_LoanInstallmentStatus.Consumed))
            throw new InvalidOperationException("ยกเลิกไม่ได้ — มีการหักเงินกู้งวดแรกไปแล้ว");

        loan.Status = Pay_EmployeeLoanStatus.Cancelled;
        foreach (var inst in loan.Pay_EmployeeLoanInstallments.Where(i => i.Status == Pay_LoanInstallmentStatus.Pending))
            inst.Status = Pay_LoanInstallmentStatus.Cancelled;

        await context.SaveChangesAsync(ct);
    }
}
