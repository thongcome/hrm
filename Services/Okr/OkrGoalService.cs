using HRM.Models;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Okr;

// OKR v2: Objective (cascade เชิงองค์กร) แยกจาก Key Result (วัดผลเชิงตัวเลข)
// ต่างจาก v1 (Perf_Goal) ที่รวมสองอย่างไว้ในโหนดเดียว — containment ระหว่าง
// Objective พ่อ-ลูกตรวจจริงผ่าน OrgEmployeeResolverHelper/orgcodefull แทนการ
// เทียบแค่ลำดับ OwnerType เหมือน v1 progress ของ Objective คำนวณจาก
// KeyResult ของตัวเองเท่านั้น ไม่ auto roll-up จากลูก (กัน double-count)
public class OkrGoalService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<List<Okr_Objective>> GetGoalTreeAsync(long cycleId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Okr_Objectives
            .Include(o => o.KeyResults)
            .Where(o => o.CycleId == cycleId)
            .OrderBy(o => o.OwnerType).ThenBy(o => o.Title)
            .ToListAsync(ct);
    }

    public async Task<Okr_Objective> CreateObjectiveAsync(Okr_Objective objective, string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var cycle = await context.Okr_Cycles.FirstOrDefaultAsync(c => c.Id == objective.CycleId, ct)
            ?? throw new InvalidOperationException("ไม่พบวงจร OKR ที่เลือก");
        if (cycle.IsLocked)
            throw new InvalidOperationException("วงจรนี้ถูกล็อกแล้ว ไม่สามารถสร้าง Objective ใหม่ได้");

        switch (objective.OwnerType)
        {
            case OkrOwnerType.Organization when objective.OwnerOrganizationId is null:
                throw new InvalidOperationException("กรุณาเลือกหน่วยงานเจ้าของ Objective");
            case OkrOwnerType.Employee when objective.OwnerHremployeeId is null:
                throw new InvalidOperationException("กรุณาเลือกพนักงานเจ้าของ Objective");
        }

        if (objective.ParentObjectiveId is long parentId)
        {
            var parent = await context.Okr_Objectives.FirstOrDefaultAsync(o => o.Id == parentId, ct)
                ?? throw new InvalidOperationException("ไม่พบ Objective แม่ที่เลือก");
            if (parent.CycleId != objective.CycleId)
                throw new InvalidOperationException("Objective แม่ต้องอยู่ในวงจรเดียวกัน");
            if ((int)objective.OwnerType <= (int)parent.OwnerType)
                throw new InvalidOperationException("Objective ลูกต้องอยู่ระดับที่ลึกกว่า Objective แม่เสมอ (บริษัท → หน่วยงาน → บุคคล)");

            await ValidateContainmentAsync(context, companyId, objective, parent, ct);
        }

        objective.CreatedDate = DateTime.Now;
        context.Okr_Objectives.Add(objective);
        await context.SaveChangesAsync(ct);
        return objective;
    }

    private static async Task ValidateContainmentAsync(HRMContext context, string companyId, Okr_Objective child, Okr_Objective parent, CancellationToken ct)
    {
        // Company owner = ทั้งบริษัทคือ container เสมอ ผ่านทุกกรณี
        if (parent.OwnerType == OkrOwnerType.Company) return;

        if (parent.OwnerType == OkrOwnerType.Organization)
        {
            var parentOrgId = parent.OwnerOrganizationId!.Value;

            if (child.OwnerType == OkrOwnerType.Employee)
            {
                var subtree = await OrgEmployeeResolverHelper.ResolveOrganizationSubtreeAsync(context, companyId, parentOrgId, ct);
                if (!subtree.Any(e => e.id == child.OwnerHremployeeId))
                    throw new InvalidOperationException("พนักงานที่เลือกไม่ได้อยู่ในหน่วยงาน (หรือหน่วยงานลูก) ของ Objective แม่ — กรุณาตรวจสอบผังองค์กร");
            }
            else if (child.OwnerType == OkrOwnerType.Organization)
            {
                var parentOrg = await context.com_organizations.FirstOrDefaultAsync(o => o.id == parentOrgId, ct);
                var childOrg = await context.com_organizations.FirstOrDefaultAsync(o => o.id == child.OwnerOrganizationId, ct);
                if (parentOrg is null || childOrg is null || string.IsNullOrWhiteSpace(parentOrg.orgcodefull) || string.IsNullOrWhiteSpace(childOrg.orgcodefull)
                    || !childOrg.orgcodefull.StartsWith(parentOrg.orgcodefull))
                    throw new InvalidOperationException("หน่วยงานที่เลือกไม่ได้อยู่ใต้หน่วยงานของ Objective แม่ — กรุณาตรวจสอบผังองค์กร");
            }
        }
    }

    public async Task UpdateObjectiveAsync(Okr_Objective objective, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.Okr_Objectives.FirstOrDefaultAsync(o => o.Id == objective.Id, ct)
            ?? throw new InvalidOperationException("ไม่พบ Objective นี้แล้ว");

        existing.Title = objective.Title;
        existing.Description = objective.Description;
        existing.Status = objective.Status;
        existing.CategoryId = objective.CategoryId;

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteObjectiveAsync(long objectiveId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var hasChildren = await context.Okr_Objectives.AnyAsync(o => o.ParentObjectiveId == objectiveId, ct);
        if (hasChildren)
            throw new InvalidOperationException("ลบไม่ได้ — ยังมี Objective ลูกผูกอยู่กับ Objective นี้");
        var hasKrs = await context.Okr_KeyResults.AnyAsync(k => k.ObjectiveId == objectiveId, ct);
        if (hasKrs)
            throw new InvalidOperationException("ลบไม่ได้ — ยังมี Key Result ผูกอยู่กับ Objective นี้ กรุณาลบ Key Result ก่อน");

        var objective = await context.Okr_Objectives.FirstOrDefaultAsync(o => o.Id == objectiveId, ct);
        if (objective is null) return;
        context.Okr_Objectives.Remove(objective);
        await context.SaveChangesAsync(ct);
    }

    // enforceOwnerHremployeeId: ตั้งค่าจากหน้า ESS เท่านั้น — บังคับให้ Objective
    // เป้าหมายเป็นของพนักงานคนที่กำลังกระทำการอยู่จริง กันแก้ไข Objective ของ
    // คนอื่นผ่านการสวมรอย URL ตรงๆ (ตรวจที่ service layer ไม่ใช่แค่ซ่อนปุ่มใน UI)
    public async Task<Okr_KeyResult> AddKeyResultAsync(Okr_KeyResult keyResult, long? enforceOwnerHremployeeId = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var objective = await context.Okr_Objectives.Include(o => o.Cycle).FirstOrDefaultAsync(o => o.Id == keyResult.ObjectiveId, ct)
            ?? throw new InvalidOperationException("ไม่พบ Objective ที่เลือก");
        if (enforceOwnerHremployeeId is long ownerId && objective.OwnerHremployeeId != ownerId)
            throw new InvalidOperationException("คุณไม่มีสิทธิ์แก้ไข Objective นี้");
        if (objective.Cycle.IsLocked)
            throw new InvalidOperationException("วงจรนี้ถูกล็อกแล้ว ไม่สามารถเพิ่ม Key Result ใหม่ได้");
        if (keyResult.TargetValue == (keyResult.StartValue ?? 0m) && keyResult.MetricType != OkrKeyResultMetricType.Milestone)
            throw new InvalidOperationException("ค่าเป้าหมายต้องไม่เท่ากับค่าเริ่มต้น (กันหารด้วยศูนย์ตอนคำนวณ % ความคืบหน้า)");

        keyResult.CreatedDate = DateTime.Now;
        keyResult.CurrentValue = keyResult.StartValue ?? 0m;
        context.Okr_KeyResults.Add(keyResult);
        await context.SaveChangesAsync(ct);
        return keyResult;
    }

    public async Task RecordKeyResultCheckInAsync(long keyResultId, decimal value, OkrConfidenceLevel? confidence, string? note, long actorUserId, long? enforceOwnerHremployeeId = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var kr = await context.Okr_KeyResults.Include(k => k.Objective).ThenInclude(o => o.Cycle)
            .FirstOrDefaultAsync(k => k.Id == keyResultId, ct)
            ?? throw new InvalidOperationException("ไม่พบ Key Result นี้แล้ว");
        if (enforceOwnerHremployeeId is long ownerId && kr.Objective.OwnerHremployeeId != ownerId)
            throw new InvalidOperationException("คุณไม่มีสิทธิ์ check-in ให้ Key Result นี้");
        if (kr.Objective.Cycle.IsLocked)
            throw new InvalidOperationException("วงจรนี้ถูกล็อกแล้ว ไม่สามารถ check-in ได้อีก");

        context.Okr_KeyResultCheckIns.Add(new Okr_KeyResultCheckIn
        {
            KeyResultId = keyResultId,
            CheckInDate = DateTime.Now,
            ValueAtCheckIn = value,
            Confidence = confidence,
            Note = note,
            CreatedByUserId = actorUserId,
        });
        kr.CurrentValue = value;

        await context.SaveChangesAsync(ct);
    }

    public async Task AddObjectiveUpdateAsync(long objectiveId, string note, OkrObjectiveStatus statusAtUpdate, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var objective = await context.Okr_Objectives.FirstOrDefaultAsync(o => o.Id == objectiveId, ct)
            ?? throw new InvalidOperationException("ไม่พบ Objective นี้แล้ว");

        context.Okr_ObjectiveUpdates.Add(new Okr_ObjectiveUpdate
        {
            ObjectiveId = objectiveId,
            UpdateDate = DateTime.Now,
            StatusAtUpdate = statusAtUpdate,
            Note = note,
            CreatedByUserId = actorUserId,
        });
        objective.Status = statusAtUpdate;

        await context.SaveChangesAsync(ct);
    }

    public async Task<decimal?> GetKeyResultWeightWarningAsync(long objectiveId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var krs = await context.Okr_KeyResults.Where(k => k.ObjectiveId == objectiveId).ToListAsync(ct);
        if (krs.Count == 0) return null;
        return krs.Sum(k => k.Weight);
    }

    public static decimal CalculateKeyResultProgress(Okr_KeyResult kr)
    {
        if (kr.MetricType == OkrKeyResultMetricType.Milestone)
            return kr.CurrentValue >= kr.TargetValue ? 100m : 0m;

        var start = kr.StartValue ?? 0m;
        if (kr.TargetValue == start) return 0m;
        var pct = (kr.CurrentValue - start) / (kr.TargetValue - start) * 100m;
        return Math.Clamp(pct, 0m, 100m);
    }

    public static decimal? CalculateObjectiveProgress(Okr_Objective objective)
    {
        if (objective.KeyResults is null || objective.KeyResults.Count == 0) return null;

        var totalWeight = objective.KeyResults.Sum(k => k.Weight);
        if (totalWeight <= 0m) return null;

        var weightedSum = objective.KeyResults.Sum(k => CalculateKeyResultProgress(k) * k.Weight);
        return Math.Round(weightedSum / totalWeight, 2);
    }
}
