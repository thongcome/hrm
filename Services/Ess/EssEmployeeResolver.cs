namespace HRM.Services.Ess;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Resolves the current ESS user's own Hremployee record from the
// "hremployee_id" claim (attached automatically by ScUserClaimsPrincipalFactory
// for any sc_user with hremployee_id set — see Services/Login/PayrollCompanyResolver.cs
// for the same lookup pattern used for payroll_company). Every ESS page and
// endpoint must go through this instead of querying Hremployee directly, so
// there is exactly one place that decides "which employee is this person" —
// duplicating that logic per page is how role/menu claims drifted apart
// once already in this app (see ScUserClaimsPrincipalFactory.cs).
//
// 22 ก.ย. 2569: หาจาก claim "hremployee_id" (sc_user.hremployee_id, FK) — ความสัมพันธ์ผูกด้วย id ไม่ใช่รหัส
// (skill advance-data-discipline Part A0) · session ที่ login ก่อนเปลี่ยนไม่มี claim นี้ → login ใหม่หนึ่งครั้ง
public static class EssEmployeeResolver
{
    public const string EmployeeIdClaim = "hremployee_id";

    /// <summary>id ของพนักงานเจ้าของ session (null = บัญชีที่ไม่ใช่พนักงาน เช่น advadmin)</summary>
    public static long? EmployeeId(System.Security.Claims.ClaimsPrincipal user)
        => long.TryParse(user.FindFirst(EmployeeIdClaim)?.Value, out var id) ? id : null;

    public static async Task<Hremployee?> ResolveAsync(HRMContext context, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (EmployeeId(user) is not long id)
            return null;
        return await context.Hremployee.FirstOrDefaultAsync(e => e.id == id, ct);
    }
}
