namespace HRM.Services.Login;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Role-model expansion (CEO, 2026-09-07): sc_role had only 3 rows (Admin,
// Employee, กรรมการ) — nowhere near enough to reflect a real org. The CEO's
// own list: ประเภทพนักงาน (already covered by EmployeeTypeRoleSeeder),
// ตำแหน่ง (Pos_ExecType), หัวหน้าแผนกต่างๆ, guest, vendor/คนนอก. This seeder
// creates the STATIC role rows for the last four categories (idempotent
// add-missing-by-rolecode, never touches an existing row — same discipline
// as EmployeeTypeRoleSeeder). DerivedRoleSyncService (a separate, startup-run
// service) is what actually GRANTS these roles to the right people, and it
// re-runs every startup because — unlike employeetype — position, org-boss
// status, and vendor status all change over an employee's lifetime without
// anyone touching their sc_user account by hand.
//
// Position roles are seeded from whatever Pos_ExecType rows already exist
// (any company, any code) rather than a hardcoded A01..A07 list — a new
// company can define its own ladder with its own codes/names and this picks
// it up next startup with zero code changes, matching the config-first
// pattern used everywhere else in this codebase. Only IsBoss levels get a
// role (a plain "Officer" doesn't need its own permission role — Employee
// already covers it); a level's role stays IN PLACE if that Pos_ExecType row
// is later deactivated, only new seeding stops.
public static class PositionRoleSeeder
{
    public const string GuestRoleCode = "GUEST";
    public const string VendorRoleCode = "VENDOR";
    public const string DeptHeadRoleCode = "ORG_APPROVER";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        var changed = false;
        var existingByRoleCode = await context.sc_roles
            .Where(r => r.rolecode != null)
            .ToDictionaryAsync(r => r.rolecode!, StringComparer.OrdinalIgnoreCase);
        var defaultCompanyId = await context.sc_roles.Select(r => r.company_id).FirstOrDefaultAsync();
        if (defaultCompanyId == 0) defaultCompanyId = 1;

        // --- a) one role per IsBoss Pos_ExecType level, add-missing-by-code ---
        var bossLevels = await context.Pos_ExecTypes
            .Where(p => p.IsBoss && p.IsActive)
            .Select(p => new { p.Code, p.Name, p.NameEn })
            .Distinct()
            .ToListAsync();

        foreach (var level in bossLevels)
        {
            if (string.IsNullOrWhiteSpace(level.Code)) continue;
            var roleCode = $"POS_{level.Code}";
            if (existingByRoleCode.ContainsKey(roleCode)) continue;

            var role = new sc_role
            {
                company_id = defaultCompanyId,
                name = level.Name ?? level.Code,
                abbr = level.Code,
                rolelevel = "1",
                rolecode = roleCode,
                isactive = true,
                isHeader = true, // this whole category is "หัวหน้า/บอส" by definition (IsBoss levels only)
                pos_exec_code = level.Code,
                moddate = DateTime.Now,
                modby = "PositionRoleSeeder",
            };
            context.sc_roles.Add(role);
            existingByRoleCode[roleCode] = role;
            changed = true;
        }

        // --- b) หัวหน้าแผนก (org-chart boss, any department) ------------------
        // Distinct from the position-ladder roles above: those are about an
        // employee's own rank; this is "currently set as an org unit's
        // boss_emp_id", which DerivedRoleSyncService drives from
        // com_organization directly (not from pos_exec_code), so this role
        // itself carries no pos_exec_code mapping.
        if (!existingByRoleCode.ContainsKey(DeptHeadRoleCode))
        {
            var role = new sc_role
            {
                company_id = defaultCompanyId,
                name = "หัวหน้าแผนก (ตามผังองค์กร)",
                abbr = "DeptHead",
                rolelevel = "1",
                rolecode = DeptHeadRoleCode,
                isactive = true,
                isHeader = true,
                moddate = DateTime.Now,
                modby = "PositionRoleSeeder",
            };
            context.sc_roles.Add(role);
            existingByRoleCode[DeptHeadRoleCode] = role;
            changed = true;
        }

        // --- c) Guest — manual-assign only, no auto-grant signal exists ------
        if (!existingByRoleCode.ContainsKey(GuestRoleCode))
        {
            context.sc_roles.Add(new sc_role
            {
                company_id = defaultCompanyId,
                name = "ผู้เยี่ยมชม",
                abbr = "Guest",
                rolelevel = "1",
                rolecode = GuestRoleCode,
                isactive = true,
                moddate = DateTime.Now,
                modby = "PositionRoleSeeder",
            });
            changed = true;
        }

        // --- d) Vendor / บุคคลภายนอกที่มาทำงาน --------------------------------
        if (!existingByRoleCode.ContainsKey(VendorRoleCode))
        {
            context.sc_roles.Add(new sc_role
            {
                company_id = defaultCompanyId,
                name = "ผู้รับเหมา / บุคคลภายนอก",
                abbr = "Vendor",
                rolelevel = "1",
                rolecode = VendorRoleCode,
                isactive = true,
                moddate = DateTime.Now,
                modby = "PositionRoleSeeder",
            });
            changed = true;
        }

        if (changed)
            await context.SaveChangesAsync();
    }
}
