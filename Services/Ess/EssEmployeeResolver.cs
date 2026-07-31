namespace HRM.Services.Ess;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Resolves the current ESS user's own Hremployee record from the "empno"
// claim (attached automatically by ScUserClaimsPrincipalFactory for any
// sc_user with empid set — see Services/Login/PayrollCompanyResolver.cs for
// the same lookup pattern used for payroll_company). Every ESS page and
// endpoint must go through this instead of querying Hremployee directly, so
// there is exactly one place that decides "which employee is this person" —
// duplicating that logic per page is how role/menu claims drifted apart
// once already in this app (see ScUserClaimsPrincipalFactory.cs).
public static class EssEmployeeResolver
{
    public static async Task<Hremployee?> ResolveAsync(HRMContext context, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct = default)
    {
        var empno = user.FindFirst("empno")?.Value;
        if (string.IsNullOrWhiteSpace(empno))
            return null;

        return await context.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == empno, ct);
    }
}
