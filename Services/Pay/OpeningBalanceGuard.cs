using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// A month is paid EITHER by the previous system (ยอดยกมา, Pay_EmployeeOpeningBalance) OR by a
// regular run here — never both, or the same salary counts twice in YTD tax, 50 ทวิ and ภ.ง.ด.1ก.
// The import refuses a month already calculated here; this is the other direction: the engine
// refuses a regular run for a month that is already an opening balance, and pre-flight shows it.
// Bonus runs are allowed — a bonus paid here in a month the old system paid salary is real.
// Ported from Advance.Payroll (CEO order, 22 ก.ย. 2569: mirror the payroll domain).
public static class OpeningBalanceGuard
{
    public static async Task<List<string>> OverlappingEmpNosAsync(HRMContext context, Pay_PayrollRun run, CancellationToken ct = default)
    {
        if (run.RunType is not (PayrollRunType.Regular or PayrollRunType.FinalPay)) return [];
        return await context.Pay_EmployeeOpeningBalances
            .Where(o => o.CompanyId == run.CompanyId && o.IsActive
                        && o.TaxYear == run.PeriodStart.Year && o.Month == run.PeriodStart.Month)
            .OrderBy(o => o.EmpNo)
            .Select(o => o.EmpNo)
            .ToListAsync(ct);
    }

    public static string Message(Pay_PayrollRun run, IReadOnlyList<string> empNos) =>
        $"เดือน {run.PeriodStart.Month}/{run.PeriodStart.Year + 543} มียอดยกมาจากระบบเดิมแล้ว {empNos.Count} คน "
        + $"({string.Join(", ", empNos.Take(10))}{(empNos.Count > 10 ? ", …" : "")}) — เดือนนี้จ่ายจากระบบเดิมไปแล้ว "
        + "คำนวณซ้ำจะนับเงินได้/ภาษีสองครั้ง: เริ่มรอบปกติที่เดือนถัดไป หรือปิดยอดยกมาของเดือนนี้ก่อน";
}
