namespace HRM.Services.Dev;

using HRM.Data;
using HRM.Models;
using HRM.Services.Login;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Development-only convenience so anyone (a new developer, an AI agent
// verifying a change) always has a known-working login without hand-editing
// the database or guessing a forgotten password. Every app startup in
// Development resets DevAdminEmail's password back to DevAdminPassword —
// idempotent by design, same "runs every startup, only actually does
// something the first/next time it's needed" idiom as the erp-web OIDC
// client seed in Program.cs. Never runs outside Development — see the
// IsDevelopment() gate around the call in Program.cs. Do not add an
// equivalent for Production; a hardcoded password is only acceptable
// because this code path is physically unreachable there.
public static class DevAuthSeeder
{
    public const string DevAdminEmail = "admin@local.humanok";
    public const string DevAdminPassword = "Dev@12345";

    // Non-admin counterpart to the admin account above, for verifying
    // ESS-only / non-admin behavior (e.g. that an admin-gated page actually
    // rejects a plain employee) without guessing or raw-SQL-resetting a
    // real person's password. sc_user 26 ("008", empid 008) already exists
    // with only the "Employee" role — no "Admin" — so this account is a
    // true non-admin fixture, not a stand-in that happens to also have
    // elevated access.
    public const string DevEssEmail = "ess.test008@hrm.local";
    public const string DevEssPassword = "Dev@12345";

    // Demo cast (added for live demos, 2026-08-31): every existing
    // test-account email here gets the same known password on startup, so
    // a demo can switch between several employee/supervisor personas
    // without anyone tracking per-account passwords. Only accounts whose
    // Identity email is a synthetic test address are listed — accounts
    // tied to a real person's own email (sawat@, thongcome@...) are
    // deliberately left alone so this never clobbers a password a human
    // actually set. test_payroll@hrm.local is also excluded on purpose:
    // the pre-login home page advertises its password as Test@12345, and
    // resetting it here would silently make that on-screen hint wrong.
    // See docs/HRM_Demo_Guide.md for who each account is in the demo.
    private static readonly string[] DemoEmployeeEmails =
    {
        "test005@hrm.local",     // 005 ปราณี สุขใจ — Employee+Admin (หัวหน้า/HR persona)
        "009@local.humanok",     // 009 นภัสสร วงศ์ษา — Employee only
        "010@local.humanok",     // 010 ประยุทธ ชัยมงคล — Employee only
        "012@local.humanok",     // 012 ชาญวิทย์ บุญมี — Employee+Admin
    };

    public static async Task EnsureKnownDevPasswordAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await ResetPasswordAsync(userManager, DevAdminEmail, DevAdminPassword);
        await ResetPasswordAsync(userManager, DevEssEmail, DevEssPassword);
        foreach (var email in DemoEmployeeEmails)
            await ResetPasswordAsync(userManager, email, DevEssPassword);
    }

    // End-to-end fixture for CEO access-control step 3 (auto-role by
    // employeeType at first user setup), Development only. Creates — once —
    // a committee-type (EMPTYPE_CODE '02') demo employee at AdvanceDigital
    // with an sc_user that has NO role rows, then provisions its Identity
    // account through the REAL UserProvisioningService path, which must
    // auto-assign the 'กรรมการ' role via sc_role.employeetype_code. Login:
    // KB0001 / Dev@12345. Must run AFTER EmployeeTypeRoleSeeder (needs the
    // '02' mapping) — see the call site in Program.cs. Same dev-only
    // rationale as the password resets above.
    public const string CommitteeFixtureLogin = "KB0001";

    public static async Task EnsureCommitteeAutoRoleFixtureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevAuthSeeder");
        await using var ctx = await dbFactory.CreateDbContextAsync();

        var scUser = await ctx.sc_users.FirstOrDefaultAsync(u => u.loginname == CommitteeFixtureLogin);
        if (scUser is null)
        {
            var company = await ctx.com_companies.FirstOrDefaultAsync(c => c.code == "ADVD");
            var emp = await ctx.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == CommitteeFixtureLogin);
            if (emp is null)
            {
                emp = new Hremployee
                {
                    companyid = "ADVD",
                    EmpNo = CommitteeFixtureLogin,
                    EmpName = "สมเกียรติ",
                    EmpSurname = "วัฒนกิจ",
                    EmptypeCode = "02", // กรรมการ — the whole point of the fixture
                    Sex = "M",
                    WorkDate = new DateTime(2020, 1, 6),
                    IsActive = true,
                };
                ctx.Hremployee.Add(emp);
            }
            scUser = new sc_user
            {
                loginname = CommitteeFixtureLogin,
                empid = CommitteeFixtureLogin,
                firstname = "สมเกียรติ",
                lastname = "วัฒนกิจ",
                company_id = company?.id ?? 1,
                isdisable = false,
                iscancel = false,
                isActivate = true,
                isforcechanged = false,
                moddate = DateTime.Now,
                modby = "DevAuthSeeder",
            };
            ctx.sc_users.Add(scUser);
            await ctx.SaveChangesAsync();
            logger.LogInformation("Committee auto-role fixture: created Hremployee/sc_user {Login}.", CommitteeFixtureLogin);
        }

        // Provision through the real first-setup path (idempotent) — this is
        // what triggers the employeeType→role auto-assign being proven here.
        var provisioning = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        var result = await provisioning.EnsureIdentityLinkedAsync(scUser, DevEssPassword, "kb0001@hrm.local");
        if (!result.Succeeded)
        {
            logger.LogWarning("Committee auto-role fixture: provisioning failed — {Error}", result.Error);
            return;
        }

        var roleNames = await (from ur in ctx.sc_user_roles
                               join r in ctx.sc_roles on ur.roleid equals r.roleid
                               where ur.userid == scUser.userid
                               select r.name).ToListAsync();
        logger.LogInformation("Committee auto-role fixture: {Login} now holds roles [{Roles}] (expected: กรรมการ).",
            CommitteeFixtureLogin, string.Join(", ", roleNames));
    }

    // Demo login scoped to AdvanceDigital (CEO 1 ก.ย. 2569: demo HRD on the
    // 7,000-employee ADVD company, not the 24-person '001'). The existing
    // 'admin' account resolves to company '001', so this adds a SEPARATE
    // login (advadmin / Dev@12345) linked to ADVD's CEO employee AD0001 —
    // payroll_company resolves to 'ADVD', it holds the Admin role (full menu
    // claims), and AD0001 is a real org approver so MSS/approval demos work
    // too. The '001' admin path is left untouched. Development only.
    public const string AdvdAdminLogin = "advadmin";
    private const string AdvdAdminEmpNo = "AD0001";

    public static async Task EnsureAdvdDemoAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevAuthSeeder");
        await using var ctx = await dbFactory.CreateDbContextAsync();

        // AD0001 must exist (seeded by DemoCompanySeeder). If not, ADVD isn't
        // seeded yet — nothing to do.
        var emp = await ctx.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == AdvdAdminEmpNo && e.companyid == "ADVD");
        if (emp is null) { logger.LogWarning("ADVD demo admin: employee {Emp} not found — ADVD not seeded yet.", AdvdAdminEmpNo); return; }

        var company = await ctx.com_companies.FirstOrDefaultAsync(c => c.code == "ADVD");
        var scUser = await ctx.sc_users.FirstOrDefaultAsync(u => u.loginname == AdvdAdminLogin);
        if (scUser is null)
        {
            // A dedicated login row (loginname 'advadmin') pointing at AD0001,
            // so we never disturb AD0001's own employee sc_user.
            scUser = new sc_user
            {
                loginname = AdvdAdminLogin,
                empid = AdvdAdminEmpNo, // bridges to payroll_company 'ADVD'
                firstname = emp.EmpName ?? "ADVD",
                lastname = emp.EmpSurname ?? "Admin",
                company_id = company?.id ?? 1,
                isdisable = false,
                iscancel = false,
                isActivate = true,
                isforcechanged = false,
                moddate = DateTime.Now,
                modby = "DevAuthSeeder",
            };
            ctx.sc_users.Add(scUser);
            await ctx.SaveChangesAsync();
            logger.LogInformation("ADVD demo admin: created sc_user {Login} -> {Emp}.", AdvdAdminLogin, AdvdAdminEmpNo);
        }

        // Grant the Admin role (full menu claims) if not already held.
        var adminRole = await ctx.sc_roles.FirstOrDefaultAsync(r => r.name == "Admin" && r.isactive);
        if (adminRole is not null)
        {
            var hasAdmin = await ctx.sc_user_roles.AnyAsync(ur => ur.userid == scUser.userid && ur.roleid == adminRole.roleid);
            if (!hasAdmin)
            {
                ctx.sc_user_roles.Add(new sc_user_role
                {
                    userid = scUser.userid,
                    roleid = adminRole.roleid,
                    empid = AdvdAdminEmpNo,
                    isactive = true,
                    modate = DateTime.Now,
                    modby = "DevAuthSeeder",
                });
                await ctx.SaveChangesAsync();
            }
        }

        // Provision the Identity login (idempotent) with the known dev password.
        var provisioning = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        var result = await provisioning.EnsureIdentityLinkedAsync(scUser, DevAdminPassword, "advadmin@hrm.local");
        logger.LogInformation("ADVD demo admin: login '{Login}' provisioned={Ok} (password = the dev admin password).",
            AdvdAdminLogin, result.Succeeded);
    }

    private static async Task ResetPasswordAsync(UserManager<ApplicationUser> userManager, string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return; // Account itself (and its sc_user/role/menu linkage) is expected to already exist — this only resets the password, never creates the account.

        if (await userManager.HasPasswordAsync(user))
            await userManager.RemovePasswordAsync(user);
        await userManager.AddPasswordAsync(user, password);
    }
}
