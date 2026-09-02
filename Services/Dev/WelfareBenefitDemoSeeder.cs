namespace HRM.Services.Dev;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Seeds a sensible Thai-SME welfare-benefit catalog (advance-data-discipline:
// "ship sensible defaults so it works out of the box, but let everything be
// overridden") for the demo companies, so the new Welfare module has content to
// show and HR has a realistic starting point to edit rather than a blank
// catalog.
//
// Idempotent by (CompanyId, Code) — add-missing only, never overwrites a row HR
// may have edited, and safe to run every startup. Development-only call site.
public static class WelfareBenefitDemoSeeder
{
    private static readonly string[] Companies = { "001", "ADVD" };

    private sealed record Def(
        string Code, string NameTh, string NameEn, WelfareBenefitCategory Category,
        WelfareEntitlementMode Mode, decimal? Annual, decimal? PerEvent, int? MaxPerYear,
        bool RequiresReceipt, int? MinServiceMonths, int Sort, string? Desc = null);

    private static readonly Def[] Catalog =
    {
        new("MED_OPD", "ค่ารักษาพยาบาลผู้ป่วยนอก (OPD)", "Outpatient Medical", WelfareBenefitCategory.Medical,
            WelfareEntitlementMode.AnnualAmount, 20000, null, null, true, null, 10, "เบิกตามใบเสร็จค่ารักษาผู้ป่วยนอก ภายในวงเงินต่อปี"),
        new("DENTAL", "ค่าทันตกรรม", "Dental", WelfareBenefitCategory.Medical,
            WelfareEntitlementMode.AnnualAmount, 5000, null, null, true, null, 20),
        new("GLASSES", "ค่าตัดแว่นสายตา", "Prescription Glasses", WelfareBenefitCategory.Allowance,
            WelfareEntitlementMode.PerEventAmount, null, 2000, 1, true, 12, 30, "ปีละ 1 ครั้ง สำหรับพนักงานที่ทำงานครบ 1 ปี"),
        new("HEALTH_CHECK", "ตรวจสุขภาพประจำปี", "Annual Health Check", WelfareBenefitCategory.HealthCheck,
            WelfareEntitlementMode.CountPerYear, null, null, 1, false, 12, 40, "บริษัทจัดให้ปีละ 1 ครั้ง"),
        new("GRANT_FUNERAL", "เงินช่วยเหลืองานศพ (ครอบครัว)", "Bereavement Grant (Family)", WelfareBenefitCategory.Grant,
            WelfareEntitlementMode.PerEventAmount, null, 5000, null, false, null, 50, "กรณีบิดา มารดา คู่สมรส หรือบุตรเสียชีวิต"),
        new("GRANT_MARRIAGE", "เงินช่วยเหลือสมรส", "Marriage Grant", WelfareBenefitCategory.Grant,
            WelfareEntitlementMode.PerEventAmount, null, 3000, 1, false, 12, 60),
        new("GRANT_CHILDBIRTH", "เงินช่วยเหลือคลอดบุตร", "Childbirth Grant", WelfareBenefitCategory.Grant,
            WelfareEntitlementMode.PerEventAmount, null, 3000, null, false, null, 70),
        new("UNIFORM", "ค่าเครื่องแบบ/ยูนิฟอร์ม", "Uniform Allowance", WelfareBenefitCategory.Allowance,
            WelfareEntitlementMode.AnnualAmount, 2000, null, null, true, null, 80),
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("WelfareBenefitDemoSeeder");
        await using var ctx = await dbFactory.CreateDbContextAsync();

        var added = 0;
        foreach (var companyId in Companies)
        {
            var existing = await ctx.Wel_BenefitTypes
                .Where(t => t.CompanyId == companyId)
                .Select(t => t.Code)
                .ToListAsync();
            var have = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var d in Catalog)
            {
                if (have.Contains(d.Code)) continue; // add-missing only
                ctx.Wel_BenefitTypes.Add(new Wel_BenefitType
                {
                    CompanyId = companyId, Code = d.Code, NameTh = d.NameTh, NameEn = d.NameEn,
                    Category = d.Category, EntitlementMode = d.Mode,
                    AnnualLimitAmount = d.Annual, PerEventLimitAmount = d.PerEvent, MaxClaimsPerYear = d.MaxPerYear,
                    RequiresReceipt = d.RequiresReceipt, MinServiceMonths = d.MinServiceMonths,
                    Description = d.Desc, SortOrder = d.Sort, IsActive = true,
                });
                added++;
            }
        }

        if (added > 0)
        {
            await ctx.SaveChangesAsync();
            logger.LogInformation("Welfare benefit catalog seeded: {Added} benefit types across demo companies.", added);
        }
    }
}
