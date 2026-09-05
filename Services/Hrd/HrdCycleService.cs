using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Hrd;

// The annual HRD (talent-management) cycle as ONE ordered, status-aware view.
// Each HRD module already exists as its own page, but nothing showed HR the
// sequence — what to do first, what's done, what's next. This service reads
// the real completion signal of each stage (scoped to the active performance
// period + company) so /hrd/cycle can render the cycle as a guided timeline
// with a "go to module" link per stage. Everything is a cheap set-based COUNT;
// per-employee stages use a correlated EXISTS against Hremployee rather than a
// 7,000-id IN list, so it opens fast even on the large ADVD company.
public class HrdCycleService(IDbContextFactory<HRMContext> dbFactory)
{
    public enum StageStatus { NotStarted, InProgress, Complete }

    public record Stage(int Order, string Name, string Description, string Icon, string Route,
        int Done, int Total, StageStatus Status);

    public record Overview(int Year, string? PeriodName, bool HasActivePeriod,
        int TotalEmployees, int CompletedStages, int TotalStages, List<Stage> Stages);

    private static StageStatus StatusOf(int done, int total) =>
        done <= 0 ? StageStatus.NotStarted : done >= total ? StageStatus.Complete : StageStatus.InProgress;

    public async Task<Overview> GetOverviewAsync(string companyId, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);

        // The cycle is anchored on the active performance period.
        var period = await ctx.Perf_EvaluationPeriods
            .Where(p => p.CompanyId == companyId && p.IsActive)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync(ct);
        long? periodId = period?.Id;
        int year = period?.StartDate.Year ?? DateTime.Today.Year;

        int totalEmp = await ctx.Hremployee.CountAsync(e => e.companyid == companyId && e.IsActive, ct);
        int denom = Math.Max(totalEmp, 1);

        // 1) Competency requirements defined per position that employees actually hold.
        var heldPosIds = await ctx.Pos_PositionSlots
            .Where(s => s.CompanyId == companyId && s.IsActive && s.PosExecTypeId != null)
            .Select(s => s.PosExecTypeId!.Value).Distinct().ToListAsync(ct);
        if (heldPosIds.Count == 0)
            heldPosIds = await ctx.Pos_ExecTypes.Where(t => t.CompanyId == companyId).Select(t => t.Id).ToListAsync(ct);
        var reqPosIds = await ctx.Job_CompetencyRequirements
            .Where(r => r.IsActive).Select(r => r.PosExecTypeId).Distinct().ToListAsync(ct);
        int compTotal = Math.Max(heldPosIds.Count, 1);
        int compDone = heldPosIds.Count(p => reqPosIds.Contains(p));

        // 2) Competency assessment — employees with at least one assessment.
        int assessDone = await ctx.Idp_CompetencyAssessments
            .Where(a => ctx.Hremployee.Any(e => e.id == a.HremployeeId && e.companyid == companyId && e.IsActive))
            .Select(a => a.HremployeeId).Distinct().CountAsync(ct);

        // 3) Performance — instances graded in the active period.
        int perfDone = periodId is null ? 0 : await ctx.Perf_EvaluationInstances
            .Where(i => i.EvaluationPeriodId == periodId && i.FinalGrade != null)
            .Select(i => i.HremployeeId).Distinct().CountAsync(ct);

        // 4) Potential ratings (9-box) in the active period.
        int potDone = periodId is null ? 0 : await ctx.Talent_PotentialRatings
            .Where(r => r.EvaluationPeriodId == periodId)
            .Select(r => r.HremployeeId).Distinct().CountAsync(ct);

        // 5) Succession — key positions that have at least one active successor.
        var keyPosIds = await ctx.Succ_KeyPositions
            .Where(k => k.CompanyId == companyId && k.IsActive).Select(k => k.Id).ToListAsync(ct);
        var nomKeyPosIds = await ctx.Succ_SuccessorNominations
            .Where(n => n.IsActive).Select(n => n.KeyPositionId).Distinct().ToListAsync(ct);
        int succTotal = Math.Max(keyPosIds.Count, 1);
        int succDone = keyPosIds.Count(k => nomKeyPosIds.Contains(k));

        // 6) IDP — employees with a development plan.
        int idpDone = await ctx.Idp_Plans
            .Where(p => ctx.Hremployee.Any(e => e.id == p.HremployeeId && e.companyid == companyId && e.IsActive))
            .Select(p => p.HremployeeId).Distinct().CountAsync(ct);

        // 7) Learning & development — employees with a training enrollment.
        int lmsDone = await ctx.Lms_Enrollments
            .Where(x => ctx.Hremployee.Any(e => e.id == x.HremployeeId && e.companyid == companyId && e.IsActive))
            .Select(x => x.HremployeeId).Distinct().CountAsync(ct);

        // 8) Engagement — a survey campaign that has collected responses.
        int engCampaigns = await ctx.Eng_SurveyCampaigns.CountAsync(c => c.CompanyId == companyId, ct);
        int engDone = await ctx.Eng_SurveyCampaigns.CountAsync(c => c.CompanyId == companyId && c.ResponseCount > 0, ct);
        int engTotal = Math.Max(engCampaigns, 1);

        var stages = new List<Stage>
        {
            new(1, "ตั้งค่าสมรรถนะตามตำแหน่ง", "กำหนดสมรรถนะที่แต่ละตำแหน่งต้องมี (ฐานของการวิเคราะห์ gap)", MudBlazor.Icons.Material.Filled.Tune, "/competency/analytics", compDone, compTotal, StatusOf(compDone, compTotal)),
            new(2, "ประเมินสมรรถนะรายบุคคล", "พนักงาน/หัวหน้าประเมินสมรรถนะ เพื่อคำนวณช่องว่าง (gap)", MudBlazor.Icons.Material.Filled.FactCheck, "/idp/hr-overview", assessDone, denom, StatusOf(assessDone, denom)),
            new(3, "ประเมินผลการปฏิบัติงาน", "ให้คะแนน KPI/สมรรถนะ ตามรอบประเมิน และอนุมัติผล", MudBlazor.Icons.Material.Filled.Assessment, "/perf/hr-dashboard", perfDone, denom, StatusOf(perfDone, denom)),
            new(4, "ประเมินศักยภาพ + Calibrate 9-Box", "ให้คะแนนศักยภาพ และปรับเทียบผลงาน×ศักยภาพในตาราง 9-box", MudBlazor.Icons.Material.Filled.GridOn, "/talent/nine-box", potDone, denom, StatusOf(potDone, denom)),
            new(5, "วางแผนสืบทอดตำแหน่ง", "ระบุตำแหน่งสำคัญ และเสนอชื่อผู้สืบทอดตามความพร้อม", MudBlazor.Icons.Material.Filled.AccountTree, "/succession/bench-strength", succDone, succTotal, StatusOf(succDone, succTotal)),
            new(6, "จัดทำแผนพัฒนารายบุคคล (IDP)", "สร้างแผนพัฒนา 70-20-10 ปิด gap สมรรถนะและเตรียมความก้าวหน้า", MudBlazor.Icons.Material.Filled.Insights, "/idp/hr-overview", idpDone, denom, StatusOf(idpDone, denom)),
            new(7, "การเรียนรู้และพัฒนา (LMS)", "มอบหมาย/ติดตามการอบรมตามแผนพัฒนาและความจำเป็น", MudBlazor.Icons.Material.Filled.School, "/lms/dashboard", lmsDone, denom, StatusOf(lmsDone, denom)),
            new(8, "สำรวจความผูกพัน (Engagement)", "วัด eNPS/วัฒนธรรมองค์กร และจัดทำแผนปฏิบัติการ", MudBlazor.Icons.Material.Filled.Favorite, "/eng/dashboard", engDone, engTotal, StatusOf(engDone, engTotal)),
        };

        int completed = stages.Count(s => s.Status == StageStatus.Complete);
        return new Overview(year, period?.Name, period is not null, totalEmp, completed, stages.Count, stages);
    }
}
