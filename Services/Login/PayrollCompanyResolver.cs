namespace HRM.Services.Login;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// One sc_user account maps to exactly one company for payroll purposes.
//
// 11 ก.ย. 2569 (audit C3): EmpNo is only unique PER COMPANY (Hremployee's
// alternate key is companyid + EmpNo), so "find the employee by EmpNo" picks an
// arbitrary company the day a second tenant has the same code. The account's own
// company is authoritative: sc_user.company_id -> com_company.id, and
// com_company.code is the string every Pay_*/Hremployee row carries as companyid
// (verified 8 ก.ย. 2569, see CLAUDE.md). The EmpNo bridge is only a fallback for
// legacy accounts whose company_id is 0.
public static class PayrollCompanyResolver
{
    public static async Task<string?> ResolveAsync(HRMContext context, sc_user scUser, CancellationToken ct = default)
    {
        if (scUser.company_id > 0)
        {
            var code = await context.com_companies
                .Where(c => c.id == scUser.company_id)
                .Select(c => c.code)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(code))
                return code;
        }

        if (string.IsNullOrWhiteSpace(scUser.empid))
            return null;

        var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == scUser.empid, ct);
        return employee?.companyid;
    }
}
