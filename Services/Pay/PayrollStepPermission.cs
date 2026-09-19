using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// Who may press which button on a payroll run (CEO, 18 ก.ย. 2569: "ต้อง config สิทธิ์
// พนักงานคนที่ทำ payroll ได้"). Two roles, assigned per person in the user admin screen:
//
//   PAYROLL_OFFICER  เจ้าหน้าที่เงินเดือน — calculate, send for review, cancel (before approval)
//   PAYROLL_APPROVER ผู้อนุมัติเงินเดือน — approve, post, confirm paid
//
// The Admin role may do every step. Separation of duties still applies on top of this
// (PayrollSeparationOfDuties): whoever calculated/sent a run cannot approve, post or pay it,
// even when one person holds both roles or is Admin.
// Checked by role CODE (stable) in the service; the page asks the same method to decide
// which buttons to show, so UI and enforcement cannot disagree.
public static class PayrollStepPermission
{
    public const string OfficerRoleCode = "PAYROLL_OFFICER";
    public const string ApproverRoleCode = "PAYROLL_APPROVER";
    private const string AdminRoleName = "Admin";

    public static string RoleCodeFor(PayrollAction action) => action switch
    {
        PayrollAction.Approve or PayrollAction.Post or PayrollAction.MarkPaid => ApproverRoleCode,
        _ => OfficerRoleCode,
    };

    public static string RoleLabelFor(PayrollAction action) =>
        RoleCodeFor(action) == ApproverRoleCode ? "ผู้อนุมัติเงินเดือน" : "เจ้าหน้าที่เงินเดือน";

    public static async Task<HashSet<PayrollAction>> GetPermittedAsync(HRMContext context, long userId, CancellationToken ct = default)
    {
        var roles = await context.sc_user_roles
            .Where(ur => ur.userid == userId && ur.isactive && ur.role != null && ur.role.isactive)
            .Select(ur => new { ur.role!.rolecode, ur.role.name })
            .ToListAsync(ct);

        var isAdmin = roles.Any(r => string.Equals(r.name, AdminRoleName, StringComparison.OrdinalIgnoreCase));
        var codes = roles.Where(r => r.rolecode != null).Select(r => r.rolecode!).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Enum.GetValues<PayrollAction>()
            .Where(a => isAdmin || codes.Contains(RoleCodeFor(a)))
            .ToHashSet();
    }

    public static async Task EnsureAsync(HRMContext context, long userId, PayrollAction action, CancellationToken ct = default)
    {
        var permitted = await GetPermittedAsync(context, userId, ct);
        if (!permitted.Contains(action))
            throw new InvalidOperationException($"ขั้นตอนนี้ต้องเป็นผู้มีบทบาท \"{RoleLabelFor(action)}\" — ให้ผู้ดูแลระบบกำหนดบทบาทที่หน้าผู้ใช้ระบบ");
    }
}
