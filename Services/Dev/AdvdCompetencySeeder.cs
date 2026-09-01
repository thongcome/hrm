namespace HRM.Services.Dev;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Competency catalog for the AdvanceDigital (ADVD) demo company — CEO order
// 1 ก.ย. 2569, follow-up to the 7,000-employee demo dataset: without a
// catalog the JD page's "AI แนะนำ competency" (Comp_Category is
// company-scoped) correctly reports nothing to match for ADVD positions.
//
// Shape mirrors company 001's real catalog exactly: Comp_Category
// (CompanyId + CategoryType) → Comp_Competency → 5 Comp_ProficiencyLevel
// rows per competency. 13 competencies across the 3 standard categories,
// with formulaic level descriptions (adequate for demo; a real deployment
// would author them per competency).
//
// One-shot guard: skips when ADVD already has any Comp_Category — an HR
// edit to the demo catalog is never overwritten. Development-only call
// site (Program.cs dev block, right after DemoCompanySeeder).
public static class AdvdCompetencySeeder
{
    private const string CompanyId = "ADVD";

    private sealed record Comp(string Code, string Name, string NameEn, string Description);

    private static readonly (string Code, string Name, string NameEn, CompetencyCategoryType Type, Comp[] Items)[] Catalog =
    {
        ("CORE", "สมรรถนะหลัก", "Core Competencies", CompetencyCategoryType.Core, new[]
        {
            new Comp("INTEGRITY", "ความซื่อสัตย์และจริยธรรม", "Integrity & Ethics", "ยึดมั่นความถูกต้อง โปร่งใส ตรวจสอบได้ ในทุกการทำงาน"),
            new Comp("COMMUNICATE", "การสื่อสาร", "Communication", "สื่อสารชัดเจน ตรงประเด็น ทั้งการพูด การเขียน และการรับฟัง"),
            new Comp("TEAMWORK", "การทำงานเป็นทีม", "Teamwork", "ร่วมมือกับเพื่อนร่วมงานข้ามหน่วยงานเพื่อเป้าหมายร่วม"),
            new Comp("CUSTOMER", "การมุ่งเน้นลูกค้า", "Customer Focus", "เข้าใจความต้องการของลูกค้าภายใน/ภายนอก และตอบสนองได้ตรงจุด"),
            new Comp("QUALITY", "ความใส่ใจคุณภาพ", "Quality Orientation", "ทำงานถูกต้องตั้งแต่ครั้งแรก ใส่ใจรายละเอียดและมาตรฐานงาน"),
        }),
        ("LEAD", "สมรรถนะผู้นำ", "Leadership Competencies", CompetencyCategoryType.Leadership, new[]
        {
            new Comp("LEADERSHIP", "ภาวะผู้นำทีม", "Team Leadership", "นำทีมให้บรรลุเป้าหมาย มอบหมายงานและติดตามผลอย่างเป็นระบบ"),
            new Comp("DECISION", "การตัดสินใจ", "Decision Making", "ตัดสินใจบนข้อมูล ชั่งน้ำหนักความเสี่ยงและผลกระทบรอบด้าน"),
            new Comp("PEOPLE_DEV", "การพัฒนาทีมงาน", "People Development", "สอนงาน ให้ข้อมูลป้อนกลับ และวางแผนพัฒนาลูกทีมรายบุคคล"),
            new Comp("PLANNING", "การวางแผนและติดตามงาน", "Planning & Monitoring", "วางแผนงาน จัดลำดับความสำคัญ และติดตามความคืบหน้าจนสำเร็จ"),
        }),
        ("FUNC", "สมรรถนะเฉพาะงาน", "Functional Competencies", CompetencyCategoryType.Functional, new[]
        {
            new Comp("PROBLEM", "การแก้ปัญหาหน้างาน", "Problem Solving", "วิเคราะห์สาเหตุของปัญหาคุณภาพ/การผลิต และแก้ไขอย่างเป็นระบบ"),
            new Comp("DIGITAL", "ทักษะดิจิทัล", "Digital Literacy", "ใช้เครื่องมือดิจิทัลและระบบงานขององค์กรได้อย่างมีประสิทธิภาพ"),
            new Comp("SAFETY", "ความปลอดภัยในการทำงาน", "Workplace Safety", "ปฏิบัติตามมาตรฐานความปลอดภัยและชี้จุดเสี่ยงในพื้นที่งาน"),
            new Comp("DATA", "การวิเคราะห์ข้อมูล", "Data Analysis", "อ่านและวิเคราะห์รายงาน/ตัวชี้วัด เพื่อสนับสนุนการตัดสินใจ"),
        }),
    };

    // Generic 5-step ladder — {0} is the competency name.
    private static readonly string[] LevelTemplates =
    {
        "รู้และเข้าใจพื้นฐานของ{0} ปฏิบัติได้เมื่อมีผู้แนะนำ",
        "ปฏิบัติ{0}ได้ด้วยตนเองในงานประจำตามมาตรฐานที่กำหนด",
        "ปฏิบัติ{0}ได้ดีในสถานการณ์ที่หลากหลาย และช่วยแนะนำเพื่อนร่วมงานได้",
        "เป็นแบบอย่างด้าน{0} ปรับปรุงวิธีการทำงานของทีมให้ดีขึ้นได้",
        "ถ่ายทอดและวางระบบ/มาตรฐานด้าน{0} ในระดับหน่วยงานหรือองค์กร",
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdvdCompetencySeeder");
        await using var ctx = await dbFactory.CreateDbContextAsync();

        if (await ctx.Comp_Categories.AnyAsync(c => c.CompanyId == CompanyId))
            return; // one-shot: demo catalog exists (possibly HR-edited) — never touch again

        var catSort = 0;
        var totalComps = 0;
        foreach (var (code, name, nameEn, type, items) in Catalog)
        {
            var category = new Comp_Category
            {
                CompanyId = CompanyId,
                Code = code,
                Name = name,
                NameEn = nameEn,
                CategoryType = type,
                SortOrder = ++catSort,
                IsActive = true,
            };
            ctx.Comp_Categories.Add(category);
            await ctx.SaveChangesAsync(); // need category.Id for the children

            var compSort = 0;
            foreach (var item in items)
            {
                var comp = new Comp_Competency
                {
                    CategoryId = category.Id,
                    Code = item.Code,
                    Name = item.Name,
                    NameEn = item.NameEn,
                    Description = item.Description,
                    SortOrder = ++compSort,
                    IsActive = true,
                };
                ctx.Comp_Competencies.Add(comp);
                await ctx.SaveChangesAsync(); // need comp.Id for the levels

                for (var level = 1; level <= 5; level++)
                {
                    ctx.Comp_ProficiencyLevels.Add(new Comp_ProficiencyLevel
                    {
                        CompetencyId = comp.Id,
                        Level = level,
                        Description = string.Format(LevelTemplates[level - 1], item.Name),
                    });
                }
                totalComps++;
            }
            await ctx.SaveChangesAsync();
        }

        logger.LogInformation("ADVD competency catalog seeded: {Categories} categories, {Comps} competencies, {Levels} proficiency levels.",
            Catalog.Length, totalComps, totalComps * 5);
    }
}
