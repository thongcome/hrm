namespace HRM.Services.Login;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Runtime seeder for the employeetype master + the employeetype→role mapping
// (CEO access-control step 3, 2026-09-01: ตอน onboard/set user ครั้งแรก
// พนักงาน (employeetype 01) ได้ role Employee, กรรมการ (02) ได้ role กรรมการ).
//
// Reuses the legacy employeetype table (verified live: real IDENTITY id via
// sys.columns.is_identity = 1, so plain Add is safe — no NextIdAsync needed;
// table is currently empty). Idempotent add-missing-by-code: an existing row
// with the same code is NEVER touched, so admin edits survive restarts.
// Same discipline for sc_role: the 'กรรมการ' role is created only if missing
// (never touches Admin/Employee themselves), and sc_role.employeetype_code is
// filled ONLY when currently NULL — a human's later change (including
// re-pointing or clearing the mapping) wins over the seeder, forever.
//
// Wire-up (one line in Program.cs, next to the other startup seeders, AFTER
// the migration adding sc_role.employeetype_code has applied):
//   await HRM.Services.Login.EmployeeTypeRoleSeeder.SeedAsync(app.Services);
public static class EmployeeTypeRoleSeeder
{
    public const string EmployeeTypeCodeEmployee = "01";  // matches HREMPLOYEE.EMPTYPE_CODE
    public const string EmployeeTypeCodeCommittee = "02";

    // (code, thai name, english name). ismanpower = 1 for both: '01' is the
    // regular workforce, and committee members ('02') are people occupying
    // real positions too — nothing here is a non-headcount placeholder type.
    private static readonly (string Code, string NameTh, string NameEn)[] StandardTypes =
    {
        (EmployeeTypeCodeEmployee,  "พนักงาน", "Employee"),
        (EmployeeTypeCodeCommittee, "กรรมการ", "Committee"),
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        var changed = false;

        // --- a) employeetype master rows, add-missing-by-code -------------
        var existingTypeCodes = (await context.employeetypes
                .Select(t => t.code)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (code, nameTh, nameEn) in StandardTypes)
        {
            if (existingTypeCodes.Contains(code))
                continue;

            context.employeetypes.Add(new Employeetype
            {
                code = code,
                name = nameTh,
                engname = nameEn,
                ismanpower = 1,
                createdate = DateTime.Now,
            });
            changed = true;
        }

        // --- b) ensure the 'กรรมการ' role exists --------------------------
        // Conventions mirrored from the live Employee row (roleid 10:
        // company_id=1, rolelevel='1'); read it here rather than hardcoding
        // so a re-homed Employee role carries the committee role with it.
        var employeeRole = await context.sc_roles
            .FirstOrDefaultAsync(r => r.name == "Employee");

        var committeeRole = await context.sc_roles
            .FirstOrDefaultAsync(r => r.name == "กรรมการ" || r.rolecode == "COMMITTEE");
        if (committeeRole is null)
        {
            committeeRole = new sc_role
            {
                company_id = employeeRole?.company_id ?? 1,
                name = "กรรมการ",
                abbr = "committee",
                rolelevel = employeeRole?.rolelevel ?? "1",
                rolecode = "COMMITTEE",
                isactive = true,
                moddate = DateTime.Now,
                modby = "startup-seed",
            };
            context.sc_roles.Add(committeeRole);
            changed = true;
        }

        // --- c) default employeetype→role mapping, NULL-only ---------------
        if (employeeRole is not null && employeeRole.employeetype_code is null)
        {
            employeeRole.employeetype_code = EmployeeTypeCodeEmployee;
            changed = true;
        }
        if (committeeRole.employeetype_code is null)
        {
            committeeRole.employeetype_code = EmployeeTypeCodeCommittee;
            changed = true;
        }

        if (changed)
            await context.SaveChangesAsync();
    }
}
