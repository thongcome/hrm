namespace HRM.Services.Login;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Role-model expansion (CEO, 2026-09-07). PositionRoleSeeder creates the role
// ROWS once; this is what actually keeps people's MEMBERSHIP in those roles
// correct, every startup — unlike employeetype (EmployeeTypeRoleSeeder /
// UserProvisioningService, a one-time "set user ครั้งแรก" grant, because an
// employee's type essentially never changes after hire), a person's
// position, whether they're currently an org unit's boss, and whether
// they're a vendor can all change without anyone touching their sc_user
// account by hand — a promotion, transfer, or org reshuffle should just work
// on the next login, no admin follow-up step to remember.
//
// Ownership discipline (same idiom as ScMenuNavSeeder's SeederOwns): every
// sc_user_role row this service creates is stamped modby=SyncOwner. On each
// run it only ever ADDS a row for someone newly qualifying, or DEACTIVATES a
// row IT PREVIOUSLY created for someone who no longer qualifies — a role a
// human granted by hand (any other modby) is never touched, so this can
// never silently take away an admin's manual decision.
public static class DerivedRoleSyncService
{
    private const string SyncOwner = "DerivedRoleSyncService";

    public static async Task SyncAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        // บัญชี → พนักงาน ผูกด้วย sc_user.hremployee_id (FK, 22 ก.ย. 2569) — เดิมจับคู่ empid กับ EMP_NO
        // ซึ่งข้ามบริษัทได้ถ้ารหัสซ้ำกัน
        var users = await context.sc_users
            .Select(u => new { u.userid, u.hremployee_id, u.isVendor })
            .ToListAsync();
        var userIdsByEmployee = users
            .Where(u => u.hremployee_id != null)
            .GroupBy(u => u.hremployee_id!.Value)
            .ToDictionary(g => g.Key, g => g.Select(u => u.userid).ToList());

        // ---- 1) Position-ladder roles (sc_role.pos_exec_code) --------------
        var posRoles = await context.sc_roles
            .Where(r => r.isactive && r.pos_exec_code != null)
            .ToListAsync();
        if (posRoles.Count > 0)
        {
            var empByPosCode = (await context.Hremployee
                    .Where(e => e.IsActive && e.PosCode != null)
                    .Select(e => new { e.id, e.PosCode })
                    .ToListAsync())
                .GroupBy(e => e.PosCode!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(e => e.id).ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var role in posRoles)
            {
                var employeeIds = empByPosCode.TryGetValue(role.pos_exec_code!, out var list) ? list : new List<long>();
                var desiredUserIds = employeeIds
                    .SelectMany(id => userIdsByEmployee.TryGetValue(id, out var uids) ? uids : Enumerable.Empty<long>())
                    .ToHashSet();
                await ReconcileRoleMembershipAsync(context, role.roleid, desiredUserIds);
            }
        }

        // ---- 2) หัวหน้าแผนก (com_organization.boss_hremployee_id, any department) --
        var deptHeadRole = await context.sc_roles
            .FirstOrDefaultAsync(r => r.isactive && r.rolecode == PositionRoleSeeder.DeptHeadRoleCode);
        if (deptHeadRole is not null)
        {
            var bossIds = await context.com_organizations
                .Where(o => o.isActive && o.boss_hremployee_id != null)
                .Select(o => o.boss_hremployee_id!.Value)
                .Distinct()
                .ToListAsync();
            var desiredUserIds = bossIds
                .SelectMany(id => userIdsByEmployee.TryGetValue(id, out var uids) ? uids : Enumerable.Empty<long>())
                .ToHashSet();
            await ReconcileRoleMembershipAsync(context, deptHeadRole.roleid, desiredUserIds);
        }

        // ---- 3) Vendor / บุคคลภายนอก (sc_user.isVendor) ---------------------
        var vendorRole = await context.sc_roles
            .FirstOrDefaultAsync(r => r.isactive && r.rolecode == PositionRoleSeeder.VendorRoleCode);
        if (vendorRole is not null)
        {
            var desiredUserIds = users.Where(u => u.isVendor).Select(u => u.userid).ToHashSet();
            await ReconcileRoleMembershipAsync(context, vendorRole.roleid, desiredUserIds);
        }
    }

    // Adds a role row (modby=SyncOwner) for every desired user missing it,
    // and deactivates every SyncOwner-authored row for that role held by
    // someone no longer in the desired set. Never touches a row with a
    // different modby (a human's own grant/revoke decision stands forever).
    private static async Task ReconcileRoleMembershipAsync(HRMContext context, long roleId, HashSet<long> desiredUserIds)
    {
        var existingRows = await context.sc_user_roles
            .Where(ur => ur.roleid == roleId)
            .ToListAsync();
        var ownedByUs = existingRows.Where(ur => string.Equals(ur.modby, SyncOwner, StringComparison.Ordinal)).ToList();

        var alreadyActiveUserIds = ownedByUs.Where(ur => ur.isactive).Select(ur => ur.userid).ToHashSet();
        var anyRowUserIds = existingRows.Select(ur => ur.userid).ToHashSet(); // includes human-granted rows

        var toAdd = desiredUserIds.Where(uid => !anyRowUserIds.Contains(uid)).ToList();
        var toReactivate = ownedByUs.Where(ur => !ur.isactive && desiredUserIds.Contains(ur.userid)).ToList();
        var toDeactivate = ownedByUs.Where(ur => ur.isactive && !desiredUserIds.Contains(ur.userid)).ToList();

        if (toAdd.Count == 0 && toReactivate.Count == 0 && toDeactivate.Count == 0)
            return;

        // empid ของแถวใหม่ไม่ต้องตั้งเอง — HRMContext.UserEmployee.cs ทำให้ตรงกับ empid ของ user เจ้าของแถวเสมอตอน SaveChanges
        foreach (var userId in toAdd)
        {
            context.sc_user_roles.Add(new sc_user_role
            {
                roleid = roleId,
                userid = userId,
                isactive = true,
                modate = DateTime.Now,
                modby = SyncOwner,
            });
        }
        foreach (var row in toReactivate)
        {
            row.isactive = true;
            row.modate = DateTime.Now;
        }
        foreach (var row in toDeactivate)
        {
            row.isactive = false;
            row.modate = DateTime.Now;
        }

        await context.SaveChangesAsync();
    }
}
