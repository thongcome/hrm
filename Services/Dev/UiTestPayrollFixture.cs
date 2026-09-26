using HRM.Models;
using HRM.Services.Login;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Dev;

// บริษัททดสอบสำหรับกดทดสอบเงินเดือนผ่านหน้าจอ (CEO, 26 ก.ย. 2569: "ทำบัญชีทดสอบแล้วเทสผ่านหน้าจอด้วย สร้างหลาย ๆ test case")
//
// Development + Demo:SeedDemoData เท่านั้น · สร้างเฉพาะที่ยังไม่มี ไม่แก้ของที่มีแล้ว (HR/ผู้ทดสอบแก้ข้อมูลต่อได้โดยไม่ถูกทับ)
// บริษัท UITEST แยกจากข้อมูลสาธิต ADVD และจากบริษัท PTEST* ที่เทสต์ทั้งปีล้างทิ้งทุกครั้ง
//
// บัญชี (รหัสผ่าน = DevAuthSeeder.DevAdminPassword) — ไม่แตะบัญชี admin/advadmin ของ CEO:
//   uitest.maker    เจ้าหน้าที่เงินเดือน (สร้างรอบ คำนวณ ส่งอนุมัติ)       → พนักงาน UI001
//   uitest.checker  ผู้อนุมัติเงินเดือน (อนุมัติในกล่องงาน ลงบัญชี ยืนยันจ่าย) → พนักงาน UI002
//   uitest.ess      พนักงานทั่วไป (ESS: สลิป แจ้ง ล.ย.01)                   → พนักงาน UI003
//
// พนักงาน 10 คน แต่ละคนคือกรณีทดสอบหนึ่งกรณี (ดู Cases ด้านล่าง)
public static class UiTestPayrollFixture
{
    public const string CompanyCode = "UITEST";
    public const string MakerLogin = "uitest.maker", CheckerLogin = "uitest.checker", EssLogin = "uitest.ess";

    private sealed record Case(string EmpNo, string Name, string Surname, string Sex, decimal? Salary, decimal? DailyWage,
        DateTime BirthDate, DateTime WorkDate, DateTime? ResignDate, string Purpose);

    private static readonly Case[] Cases =
    {
        new("UI001", "มานะ", "ทำเงินเดือน", "M", 45000m, null, new(1990, 3, 1), new(2022, 1, 1), null, "ผู้ทำเงินเดือน (uitest.maker)"),
        new("UI002", "วิไล", "ผู้อนุมัติ", "F", 70000m, null, new(1985, 7, 1), new(2020, 1, 1), null, "ผู้อนุมัติ (uitest.checker)"),
        new("UI003", "ฟ้าใส", "ครอบครัว", "F", 100000m, null, new(1988, 5, 10), new(2019, 1, 1), null, "ESS แจ้ง ล.ย.01 คู่สมรส/บุตร/ประกัน (uitest.ess) + PF 5%"),
        new("UI004", "สมบูรณ์", "อายุเกินเข้าใหม่", "M", 30000m, null, new(1965, 2, 1), new(2026, 6, 1), null, "เข้าใหม่อายุ 61 → ไม่หักประกันสังคม"),
        new("UI005", "สมศรี", "เคยประกันตน", "F", 30000m, null, new(1965, 2, 1), new(2026, 6, 1), null, "เข้าใหม่อายุ 61 แต่เคยเป็นผู้ประกันตน → HR ต้องกำหนดที่หน้าสถานะผู้ประกันตน"),
        new("UI006", "เริ่ม", "กลางเดือน", "M", 31000m, null, new(1995, 1, 1), new(2026, 10, 16), null, "เข้างาน 16 ต.ค. → เงินเดือนตามสัดส่วน"),
        new("UI007", "ลา", "ออกกลางเดือน", "M", 40000m, null, new(1992, 1, 1), new(2021, 1, 1), new(2026, 10, 20), "ลาออก 20 ต.ค. → เงินเดือนตามสัดส่วน"),
        new("UI008", "น้อย", "ไม่ถึงเกณฑ์ภาษี", "F", 11000m, null, new(2000, 1, 1), new(2024, 1, 1), null, "เงินได้ต่ำกว่าเกณฑ์ → ภาษี 0 ประกันสังคม 550"),
        new("UI009", "แรง", "รายวัน", "M", null, 500m, new(1993, 1, 1), new(2024, 1, 1), null, "ลูกจ้างรายวัน 500 บาท × วันทำงาน"),
        new("UI010", "เก่ง", "ผู้บริหาร", "M", 150000m, null, new(1978, 1, 1), new(2015, 1, 1), null, "เงินเดือนสูง → ประกันสังคมติดเพดาน 750 ภาษีขั้นสูง"),
    };

    public static async Task EnsureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UiTestPayrollFixture");
        await using var ctx = await dbFactory.CreateDbContextAsync();

        var company = await ctx.com_companies.FirstOrDefaultAsync(c => c.code == CompanyCode);
        if (company is null)
        {
            company = new com_company { code = CompanyCode, name = "บริษัท ทดสอบหน้าจอเงินเดือน จำกัด", abbr = CompanyCode, isActive = true, tax_id = "0105569000019", moddate = DateTime.Now, modby = "UiTestPayrollFixture" };
            ctx.com_companies.Add(company);
            await ctx.SaveChangesAsync();
            await SeedCompanyConfigAsync(ctx);
            logger.LogInformation("UI test fixture: created company {Code} with payroll config.", CompanyCode);
        }

        var employees = await ctx.Hremployee.Where(e => e.companyid == CompanyCode).ToDictionaryAsync(e => e.EmpNo);
        foreach (var c in Cases.Where(c => !employees.ContainsKey(c.EmpNo)))
        {
            var e = new Hremployee
            {
                companyid = CompanyCode, EmpNo = c.EmpNo, EmpName = c.Name, EmpSurname = c.Surname, Sex = c.Sex,
                IdCard = ThaiIdFor(c.EmpNo), BirthDate = c.BirthDate, WorkDate = c.WorkDate, ResignDate = c.ResignDate,
                SalaryAmt = c.Salary, DailyWage = c.DailyWage,
                SalexpBank = "014", SalexpBranch = "0001", SalexpAccid = "0140" + c.EmpNo[2..].PadLeft(6, '0'),
                OverleaveFlag = 0,
            };
            ctx.Hremployee.Add(e);
            employees[c.EmpNo] = e;
        }
        await ctx.SaveChangesAsync();

        if (!await ctx.Pay_ProvidentFundElections.AnyAsync(p => p.HremployeeId == employees["UI003"].id))
        {
            ctx.Pay_ProvidentFundElections.Add(new Pay_ProvidentFundElection { HremployeeId = employees["UI003"].id, EmployeeContributionRate = 5, CompanyContributionRate = 5, EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true, ElectedByUserId = 0, ElectedDate = DateTime.Now });
            await ctx.SaveChangesAsync();
        }

        var provisioning = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        await EnsureLoginAsync(ctx, provisioning, logger, company, employees["UI001"], MakerLogin, "PAYROLL_OFFICER");
        await EnsureLoginAsync(ctx, provisioning, logger, company, employees["UI002"], CheckerLogin, "PAYROLL_APPROVER");
        await EnsureLoginAsync(ctx, provisioning, logger, company, employees["UI003"], EssLogin, null);
    }

    private static async Task EnsureLoginAsync(HRMContext ctx, UserProvisioningService provisioning, ILogger logger,
        com_company company, Hremployee emp, string login, string? roleCode)
    {
        var scUser = await ctx.sc_users.FirstOrDefaultAsync(u => u.loginname == login);
        if (scUser is null)
        {
            scUser = new sc_user
            {
                loginname = login, empid = emp.EmpNo, hremployee_id = emp.id,
                firstname = emp.EmpName ?? login, lastname = emp.EmpSurname ?? "",
                company_id = company.id, isdisable = false, iscancel = false, isActivate = true, isforcechanged = false,
                moddate = DateTime.Now, modby = "UiTestPayrollFixture",
            };
            ctx.sc_users.Add(scUser);
            await ctx.SaveChangesAsync();
        }

        foreach (var code in new[] { roleCode, "emp" }.OfType<string>())
        {
            var roleId = await ctx.sc_roles.Where(r => r.rolecode == code && r.isactive).Select(r => (long?)r.roleid).FirstOrDefaultAsync();
            if (roleId is long rid && !await ctx.sc_user_roles.AnyAsync(ur => ur.userid == scUser.userid && ur.roleid == rid))
            {
                ctx.sc_user_roles.Add(new sc_user_role { userid = scUser.userid, roleid = rid, empid = emp.EmpNo, isactive = true, modate = DateTime.Now, modby = "UiTestPayrollFixture" });
                await ctx.SaveChangesAsync();
            }
        }

        var result = await provisioning.EnsureIdentityLinkedAsync(scUser, DevAuthSeeder.DevAdminPassword, $"{login}@hrm.local");
        logger.LogInformation("UI test fixture: login '{Login}' ({Role}) provisioned={Ok}.", login, roleCode ?? "ESS", result.Succeeded);
    }

    // ตั้งค่าบริษัทแบบเดียวกับบริษัททดสอบของเทสต์ทั้งปี: ประกันสังคม 5% เพดาน 15,000, จ่ายวันที่ 25 เดือนละงวด,
    // วันหยุดคัดลอกจาก ADVD, นโยบายกองทุน/ขาดงาน/บัญชี GL ขั้นต่ำที่ปิดรอบได้ครบ
    private static async Task SeedCompanyConfigAsync(HRMContext ctx)
    {
        const string co = CompanyCode;
        ctx.Hrucfsecuritys.Add(new Hrucfsecurity { companyid = co, SecurityCode = HrucfsecurityRateProvider.CurrentEmployeeSecurityCode, SecurityDesc = "ประกันสังคม (พนักงาน)", PercenSecurity = 5m, SecurityMoney = 15000m, OverSecurityMoney = 0m, PercenmgSecurity = 5m, EmployerPercenSecurity = 5m });
        ctx.Pay_PayslipSettings.Add(new Pay_PayslipSettings { CompanyId = co, PasswordTemplate = "{IdCardLast4}", CompanyName = "บริษัท ทดสอบหน้าจอเงินเดือน จำกัด", CompanyTaxId = "0105569000019", CompanyAddress = "99 ถนนทดสอบ กรุงเทพฯ 10110", PayDayOfMonth = 25, ModifiedDate = DateTime.Now });
        ctx.Lve_CompanySettings.Add(new Lve_CompanySetting { CompanyId = co, CountryCode = "TH", WorkDaysMask = null });
        foreach (var h in await ctx.Lve_CompanyHolidays.AsNoTracking().Where(h => h.CompanyId == "ADVD" && h.IsActive && h.HolidayDate.Year >= 2026).ToListAsync())
            ctx.Lve_CompanyHolidays.Add(new Lve_CompanyHoliday { CompanyId = co, HolidayDate = h.HolidayDate, Name = h.Name, IsActive = true });
        ctx.Pay_ProvidentFundPolicies.Add(new Pay_ProvidentFundPolicy { CompanyId = co, PolicyCode = $"PF-{co}", EffectiveFrom = new DateOnly(2020, 1, 1), MinEmployeeRate = 2, MaxEmployeeRate = 15, MinCompanyRate = 2, MaxCompanyRate = 15, IsEnabled = true, UseFundMembershipYearsForVesting = false });
        ctx.Pay_AttendanceDeductionPolicies.Add(new Pay_AttendanceDeductionPolicy { CompanyId = co, LateMode = PayLateDeductionMode.None, AbsentMode = PayAbsentDeductionMode.DailyRate, DaysPerMonthDivisor = 30, HoursPerDay = 8, DailyWageMode = PayDailyWageDaysMode.WorkingDays, ProrationMode = PayProrationMode.ActualDaysInPeriod, IsActive = true, ModifiedDate = DateTime.Now });
        ctx.Pay_GLAccountMappings.AddRange(
            new Pay_GLAccountMapping { CompanyId = co, MappingKey = GLMappingKeys.NetPayable, DisplayName = "เงินเดือนค้างจ่าย", CreditAccountCode = "2110", IsActive = true, ModifiedDate = DateTime.Now },
            new Pay_GLAccountMapping { CompanyId = co, MappingKey = GLMappingKeys.EmployerSso, DisplayName = "ประกันสังคมนายจ้าง", DebitAccountCode = "5210", CreditAccountCode = "2120", IsActive = true, ModifiedDate = DateTime.Now },
            new Pay_GLAccountMapping { CompanyId = co, MappingKey = GLMappingKeys.EmployerProvidentFund, DisplayName = "สมทบกองทุน", DebitAccountCode = "5220", CreditAccountCode = "2130", IsActive = true, ModifiedDate = DateTime.Now });
        ctx.Pay_PaySchedules.Add(new Pay_PaySchedule { CompanyId = co, Code = "MONTHLY", Name = "เดือนละงวด", PeriodsPerMonth = 1, SecondTermStartDay = 16, AppliesTo = PayScheduleGroup.All, EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true });
        await ctx.SaveChangesAsync();
    }

    // เลขประจำตัวประชาชน 13 หลักที่ผ่านหลักตรวจสอบ (ไฟล์ ภ.ง.ด.1/สปส.1-10 ไม่เตือน) — ขึ้นต้น 1-9999 ไม่ชนกับคนจริง
    private static string ThaiIdFor(string empNo)
    {
        var body = ("199990" + empNo[2..].PadLeft(6, '0'))[..12];
        var sum = 0;
        for (var i = 0; i < 12; i++) sum += (body[i] - '0') * (13 - i);
        return body + ((11 - sum % 11) % 10);
    }
}
