namespace HRM.Services.Dev;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Wave 2 · item 6 (production-gap plan, 2 ก.ย. 2569): the Competency→JD→Perf/IDP
// chain was real end-to-end — the competency library, the JD→competency
// requirement mapping (Job_CompetencyRequirements), and the competency-gap
// analysis that drives IDP all render live — but the *body* of each Job
// Description was empty: Job_ProfileDuties had a single stray row and
// Job_ProfileQualifications / Job_ProfileKpis were empty, so the JD page showed
// its competency block fully populated above three blank sections
// ("ยังไม่มีรายการ").
//
// This fills the JD body for the ADVD finance career ladder (Pos_ExecType 2→5:
// พนักงาน / พนักงานอาวุโส / หัวหน้างาน / ผู้จัดการแผนก — the same four rungs the
// Career module already renders) with coherent Thai finance-role content:
// duties weighted to sum to 100 % and linked back to competencies (so the
// JD→competency chain is visible from the duty side too), qualifications, and
// KPIs. Pos_ExecType is a global position-type catalog (not company-scoped), so
// these JD templates apply wherever those rungs are used.
//
// Development-only, one-shot (skips once any Job_ProfileKpi exists — KPIs are the
// section that starts empty), deterministic (fixed content, no Random/Now).
// Runs after DemoCompanySeeder / AdvdHrdDemoSeeder (needs the competency rows to
// link against). Mirrors the idioms of the sibling ADVD seeders.
public static class AdvdJobProfileBodySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdvdJobProfileBodySeeder");
        await using var ctx = await dbFactory.CreateDbContextAsync();

        if (await ctx.Job_ProfileKpis.AnyAsync())
            return; // one-shot marker (KPI section starts empty everywhere)

        // Resolve competencies by code so duty→competency links survive id drift.
        var compByCode = await ctx.Comp_Competencies
            .GroupBy(c => c.Code)
            .Select(g => new { Code = g.Key, Id = g.Min(c => c.Id) })
            .ToDictionaryAsync(x => x.Code, x => x.Id);
        long? Comp(string code) => compByCode.TryGetValue(code, out var id) ? id : null;

        // The four finance-ladder rungs, in ascending order.
        const long PWorker = 2, PSenior = 3, PLead = 4, PManager = 5;

        // A stray partial duty on the worker rung (weight 60 %, no siblings) was
        // left by earlier demo data — soft-deactivate it so the seeded set below
        // reads as a clean 100 %.
        var strays = await ctx.Job_ProfileDuties
            .Where(d => d.PosExecTypeId == PWorker && d.IsActive)
            .ToListAsync();
        foreach (var d in strays) d.IsActive = false;

        void Duty(long pos, int sort, string text, decimal weight, string? compCode)
            => ctx.Job_ProfileDuties.Add(new Job_ProfileDuty
            {
                PosExecTypeId = pos, SortOrder = sort, Text = text, WeightPercent = weight,
                IncludeInCompetency = compCode is not null, LinkedCompetencyId = Comp(compCode ?? ""),
                IsActive = true,
            });
        void Qual(long pos, int sort, JobQualificationType type, string text, bool required, string? compCode = null)
            => ctx.Job_ProfileQualifications.Add(new Job_ProfileQualification
            {
                PosExecTypeId = pos, SortOrder = sort, QualType = type, Text = text, IsRequired = required,
                IncludeInCompetency = compCode is not null, LinkedCompetencyId = Comp(compCode ?? ""),
                IsActive = true,
            });
        void Kpi(long pos, int sort, string name, string target, string? unit)
            => ctx.Job_ProfileKpis.Add(new Job_ProfileKpi
            {
                PosExecTypeId = pos, SortOrder = sort, Name = name, TargetDescription = target, Unit = unit,
                IsActive = true,
            });

        // ---- พนักงาน (บัญชี/การเงิน ระดับปฏิบัติการ) ----
        Duty(PWorker, 1, "บันทึกรายการบัญชีประจำวันและจัดทำเอกสารทางการเงินให้ถูกต้องครบถ้วน", 35, "FIN_ANALYSIS");
        Duty(PWorker, 2, "กระทบยอดบัญชีธนาคารและตรวจสอบความถูกต้องของข้อมูล", 25, "QUALITY");
        Duty(PWorker, 3, "จัดเก็บและดูแลเอกสารทางบัญชีในระบบให้เป็นระเบียบตามมาตรฐาน", 20, "DIGITAL");
        Duty(PWorker, 4, "ประสานงานกับหน่วยงานภายในเพื่อสนับสนุนงานด้านการเงิน", 20, "COMMUNICATE");
        Qual(PWorker, 1, JobQualificationType.Education, "ปริญญาตรี สาขาการบัญชี การเงิน หรือสาขาที่เกี่ยวข้อง", true);
        Qual(PWorker, 2, JobQualificationType.ExperienceYears, "ประสบการณ์งานบัญชี 0–2 ปี", false);
        Qual(PWorker, 3, JobQualificationType.Skill, "ใช้โปรแกรมบัญชีและ MS Excel ในระดับดี", true, "DIGITAL");
        Kpi(PWorker, 1, "ความถูกต้องของการบันทึกบัญชี", "ไม่น้อยกว่า 99%", "%");
        Kpi(PWorker, 2, "ความตรงเวลาในการปิดงานประจำวัน", "ภายในกำหนดทุกวันทำการ", null);
        Kpi(PWorker, 3, "จำนวนข้อผิดพลาดที่ตรวจพบภายหลัง", "ไม่เกิน 2 ครั้งต่อเดือน", "ครั้ง/เดือน");

        // ---- พนักงานอาวุโส ----
        Duty(PSenior, 1, "จัดทำและวิเคราะห์รายงานการเงินรายเดือนเพื่อสนับสนุนการตัดสินใจ", 30, "FIN_ANALYSIS");
        Duty(PSenior, 2, "ตรวจทานงานบันทึกบัญชีของทีมและให้คำแนะนำให้ถูกต้อง", 25, "QUALITY");
        Duty(PSenior, 3, "จัดทำงบการเงินและรายงานภาษีให้ถูกต้องครบถ้วนตามกฎหมาย", 25, "FIN_ANALYSIS");
        Duty(PSenior, 4, "เสนอแนวทางปรับปรุงกระบวนการทำงานด้านบัญชีให้มีประสิทธิภาพ", 20, "PROBLEM");
        Qual(PSenior, 1, JobQualificationType.Education, "ปริญญาตรีขึ้นไป สาขาการบัญชีหรือการเงิน", true);
        Qual(PSenior, 2, JobQualificationType.ExperienceYears, "ประสบการณ์งานบัญชี/การเงิน 3–5 ปี", true);
        Qual(PSenior, 3, JobQualificationType.Skill, "วิเคราะห์งบการเงินและใช้ระบบ ERP ได้", true, "FIN_ANALYSIS");
        Kpi(PSenior, 1, "ความตรงเวลาในการปิดงบรายเดือน", "ไม่เกิน 5 วันทำการ", "วัน");
        Kpi(PSenior, 2, "ความแม่นยำของรายงานการเงิน", "ไม่น้อยกว่า 99%", "%");
        Kpi(PSenior, 3, "จำนวนประเด็นที่ผู้สอบบัญชีตรวจพบ", "ไม่เกิน 1 ครั้งต่อปี", "ครั้ง/ปี");

        // ---- หัวหน้างาน ----
        Duty(PLead, 1, "บริหารและควบคุมงานบัญชีของทีมให้เป็นไปตามแผนงาน", 30, "PLANNING");
        Duty(PLead, 2, "ตรวจสอบและอนุมัติรายการทางการเงินในขอบเขตที่รับผิดชอบ", 25, "DECISION");
        Duty(PLead, 3, "พัฒนาและสอนงานทีมงานให้มีทักษะเพิ่มขึ้นตามแผน", 25, "PEOPLE_DEV");
        Duty(PLead, 4, "จัดทำรายงานสรุปเสนอผู้บริหารและติดตามการใช้งบประมาณ", 20, "FIN_ANALYSIS");
        Qual(PLead, 1, JobQualificationType.Education, "ปริญญาตรีขึ้นไป สาขาการบัญชีหรือการเงิน", true);
        Qual(PLead, 2, JobQualificationType.ExperienceYears, "ประสบการณ์ 5–8 ปี รวมประสบการณ์หัวหน้างาน", true);
        Qual(PLead, 3, JobQualificationType.Skill, "ภาวะผู้นำและการบริหารทีมงาน", true, "LEADERSHIP");
        Kpi(PLead, 1, "ความตรงเวลาในการปิดงบของทีม", "ไม่เกิน 5 วันทำการ", "วัน");
        Kpi(PLead, 2, "ระดับความผูกพันของทีม (Engagement)", "ไม่น้อยกว่า 80%", "%");
        Kpi(PLead, 3, "พนักงานที่ได้รับการพัฒนาตามแผน IDP", "ไม่น้อยกว่า 90%", "%");

        // ---- ผู้จัดการแผนก ----
        Duty(PManager, 1, "กำหนดกลยุทธ์และแผนงานของแผนกการเงินให้สอดคล้องกับเป้าหมายองค์กร", 30, "STRATEGIC");
        Duty(PManager, 2, "บริหารงบประมาณและควบคุมต้นทุนของหน่วยงาน", 25, "DECISION");
        Duty(PManager, 3, "บริหารและพัฒนาบุคลากรในแผนกให้บรรลุเป้าหมาย", 25, "LEADERSHIP");
        Duty(PManager, 4, "กำกับดูแลการปฏิบัติตามกฎระเบียบและมาตรฐานทางการเงิน", 20, "QUALITY");
        Qual(PManager, 1, JobQualificationType.Education, "ปริญญาตรีขึ้นไป สาขาการบัญชีหรือการเงิน (ปริญญาโทจะพิจารณาเป็นพิเศษ)", true);
        Qual(PManager, 2, JobQualificationType.ExperienceYears, "ประสบการณ์บริหารงานการเงิน 8 ปีขึ้นไป", true);
        Qual(PManager, 3, JobQualificationType.Skill, "การวางแผนกลยุทธ์และการตัดสินใจเชิงบริหาร", true, "STRATEGIC");
        Qual(PManager, 4, JobQualificationType.License, "ผู้ทำบัญชี (CPD) หรือ CPA จะพิจารณาเป็นพิเศษ", false);
        Kpi(PManager, 1, "ผลการดำเนินงานของแผนกตามเป้าหมาย (OKR/KPI)", "ไม่น้อยกว่า 90%", "%");
        Kpi(PManager, 2, "ความแม่นยำของการบริหารงบประมาณ", "คลาดเคลื่อนไม่เกิน ±5%", "%");
        Kpi(PManager, 3, "อัตราการรักษาบุคลากรในแผนก", "ไม่น้อยกว่า 90%", "%");

        await ctx.SaveChangesAsync();
        logger.LogInformation("ADVD JD body seeded for finance ladder (Pos_ExecType 2–5): duties, qualifications, KPIs.");
    }
}
