namespace HRM.Services.Dev;

using HRM.Data;
using HRM.Models;
using HRM.Services.Login;
using Microsoft.EntityFrameworkCore;

// Dev-only: provisions a small ADVD demo cast — one employee per job grade
// (A01→1 … A07→7) — with a known login so a demo can switch between an admin and
// employees at different levels. Login name = the employee's EMP_NO, password =
// the shared dev password. Provisions through the REAL UserProvisioningService
// path (so employeetype→role auto-assign runs), idempotent. Never runs outside
// Development (see the IsDevelopment() gate in Program.cs). Requested 2026-09-03.
public static class DemoCastSeeder
{
    public const string Password = "Dev@12345";

    // One representative per POS_CODE / grade rung, resolved from the AUTOX seed.
    private static readonly string[] CastEmpNos =
    {
        "AD0001", // A07 ประธานเจ้าหน้าที่บริหาร (CEO) — grade 7
        "AD0002", // A06 ผู้อำนวยการฝ่าย — grade 6
        "AD0005", // A05 ผู้จัดการฝ่าย — grade 5
        "AD0008", // A04 ผู้จัดการแผนก — grade 4
        "AD0038", // A03 หัวหน้างาน — grade 3
        "AD0158", // A02 พนักงานอาวุโส — grade 2
        "AD0159", // A01 พนักงาน — grade 1
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var provisioning = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoCastSeeder");
        await using var ctx = await dbFactory.CreateDbContextAsync();

        var company = await ctx.com_companies.FirstOrDefaultAsync(c => c.code == "ADVD");

        foreach (var empNo in CastEmpNos)
        {
            var emp = await ctx.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == empNo && e.companyid == "ADVD");
            if (emp is null) { logger.LogWarning("DemoCast: employee {Emp} not found — ADVD not seeded yet?", empNo); continue; }

            var scUser = await ctx.sc_users.FirstOrDefaultAsync(u => u.loginname == empNo);
            if (scUser is null)
            {
                scUser = new sc_user
                {
                    loginname = empNo,
                    empid = empNo,
                    firstname = emp.EmpName ?? empNo,
                    lastname = emp.EmpSurname ?? "",
                    company_id = company?.id ?? 1,
                    isdisable = false,
                    iscancel = false,
                    isActivate = true,
                    isforcechanged = false,
                    moddate = DateTime.Now,
                    modby = "DemoCastSeeder",
                };
                ctx.sc_users.Add(scUser);
                await ctx.SaveChangesAsync();
            }

            // Real first-setup path: creates the Identity login, links it, and
            // auto-assigns the role from the employee's type. Idempotent — resets
            // the known dev password each run so the cast always logs in.
            var result = await provisioning.EnsureIdentityLinkedAsync(scUser, Password, $"{empNo.ToLower()}@hrm.local");
            if (!result.Succeeded)
                logger.LogWarning("DemoCast: provisioning {Login} failed — {Error}", empNo, result.Error);
        }
    }
}
