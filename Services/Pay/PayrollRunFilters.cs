using System.Linq.Expressions;
using HRM.Models;

namespace HRM.Services.Pay;

// The single definition of "these payroll figures are final" — used by YTD tax,
// same-month SSO capping, ภ.ง.ด.1/1ก, 50 ทวิ, e-Filing and the dashboards/reports.
//
// Never write `Status >= PayrollRunStatus.Approved` for this: Cancelled (9) is numerically
// greater than Approved (3), so that comparison silently counted cancelled runs (audit C-04).
public static class PayrollRunFilters
{
    // Approved, Posted or Paid — and a run type this product actually has (Regular/Bonus).
    public static readonly Expression<Func<Pay_PayrollRun, bool>> RunIsFinal = r =>
        (r.Status == PayrollRunStatus.Approved || r.Status == PayrollRunStatus.Posted || r.Status == PayrollRunStatus.Paid)
        && (r.RunType == PayrollRunType.Regular || r.RunType == PayrollRunType.Bonus || r.RunType == PayrollRunType.FinalPay);

    // An employee row whose money was actually paid: its run is final AND the employee was
    // not excluded from that run. An excluded row is left out of the bank file, GL and SSO
    // file, so it must not count as income, tax withheld or contributions anywhere either
    // (audit C-05).
    public static readonly Expression<Func<Pay_PayrollEmployee, bool>> RowWasPaid = pe =>
        !pe.IsExcluded
        && (pe.Pay_PayrollRun.Status == PayrollRunStatus.Approved || pe.Pay_PayrollRun.Status == PayrollRunStatus.Posted || pe.Pay_PayrollRun.Status == PayrollRunStatus.Paid)
        && (pe.Pay_PayrollRun.RunType == PayrollRunType.Regular || pe.Pay_PayrollRun.RunType == PayrollRunType.Bonus
            || pe.Pay_PayrollRun.RunType == PayrollRunType.FinalPay);

    private static readonly Func<Pay_PayrollRun, bool> RunIsFinalCompiled = RunIsFinal.Compile();

    public static bool IsFinal(this Pay_PayrollRun run) => RunIsFinalCompiled(run);
}
