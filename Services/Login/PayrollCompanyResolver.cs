namespace HRM.Services.Login;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// One sc_user account maps to exactly one company for payroll purposes,
// derived from the SAME empid -> Hremployee.EmpNo bridge already used for
// ESS ("empno" claim) — a payroll clerk's own employee record determines
// which company's payroll they can touch. Someone who legitimately needs
// to work multiple companies gets a separate sc_user account per company
// (their EmpNo differs per company anyway) rather than one account
// spanning several.
public static class PayrollCompanyResolver
{
    public static async Task<string?> ResolveAsync(HRMContext context, string? empid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(empid))
            return null;

        var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == empid, ct);
        return employee?.companyid;
    }
}
