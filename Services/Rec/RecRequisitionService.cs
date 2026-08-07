namespace HRM.Services.Rec;

using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Manpower requisition lifecycle. Submission goes through the generic
// Workflow Engine (workflow code REQUISITION_APPROVAL); status is pulled
// lazily on read via SyncStatusFromJobAsync — mirrors
// Services/Perf/PerfApprovalService.cs / Services/Org/OrgChangeRequestService.cs
// exactly (no scheduler exists anywhere in this codebase). For a
// NewHeadcount requisition, approval also auto-creates a vacant
// Pos_PositionSlot — that write happens inside SyncStatusFromJobAsync too,
// so it fires the moment any page loads the requisition after approval.
public class RecRequisitionService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    public async Task<long> CreateDraftAsync(Rec_Requisition draft, CancellationToken ct = default)
    {
        if (draft.RequisitionType == RequisitionType.Replacement && draft.TargetPositionSlotId is null)
            throw new InvalidOperationException("การขอแบบทดแทน (Replacement) ต้องระบุเลขที่อัตราเดิมที่จะทดแทน");

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        draft.Id = 0;
        draft.Status = RequisitionStatus.Draft;
        draft.JobMasterId = null;
        draft.NewPositionSlotId = null;
        context.Rec_Requisitions.Add(draft);
        await context.SaveChangesAsync(ct);
        return draft.Id;
    }

    public async Task<long> SubmitForApprovalAsync(long requisitionId, long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var req = await context.Rec_Requisitions.FirstOrDefaultAsync(r => r.Id == requisitionId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขออัตรากำลังนี้");
        if (req.Status != RequisitionStatus.Draft)
            throw new InvalidOperationException("ส่งอนุมัติได้เฉพาะคำขอที่ยังเป็นฉบับร่าง (Draft) เท่านั้น");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "REQUISITION_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow 'REQUISITION_APPROVAL' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var posExecType = await context.Pos_ExecTypes.FirstOrDefaultAsync(p => p.Id == req.PosExecTypeId, ct);
        var subject = $"คำขออัตรากำลัง ({(req.RequisitionType == RequisitionType.Replacement ? "ทดแทน" : "อัตราใหม่")}): {posExecType?.Name ?? req.PosExecTypeId.ToString()}";

        var jobId = await engine.StartJobAsync(workflow.workflowid, "Rec_Requisition", req.Id.ToString(),
            requesterUserId, requesterEmpId, subject, amount: null, ct);

        req.JobMasterId = jobId;
        req.Status = RequisitionStatus.PendingApproval;
        await context.SaveChangesAsync(ct);
        return jobId;
    }

    // Lazy apply-on-read. No-op unless still PendingApproval with a since-closed
    // job. On approval: Replacement leaves the existing slot untouched (HR
    // assigns someone to it separately via /pos/position-slots); NewHeadcount
    // creates a fresh vacant slot and records it back onto the requisition.
    public async Task SyncStatusFromJobAsync(long requisitionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var req = await context.Rec_Requisitions.FirstOrDefaultAsync(r => r.Id == requisitionId, ct);
        if (req is null || req.Status != RequisitionStatus.PendingApproval || req.JobMasterId is null)
            return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == req.JobMasterId, ct);
        if (job is null || job.isJobClosed != true)
            return;

        if (job.status != WorkflowEngineService.StatusCompleted)
        {
            req.Status = RequisitionStatus.Rejected;
            await context.SaveChangesAsync(ct);
            return;
        }

        req.Status = RequisitionStatus.Approved;

        if (req.RequisitionType == RequisitionType.NewHeadcount && req.NewPositionSlotId is null)
        {
            var posExecType = await context.Pos_ExecTypes.FirstOrDefaultAsync(p => p.Id == req.PosExecTypeId, ct);
            var slot = new Pos_PositionSlot
            {
                CompanyId = req.CompanyId,
                PosExecTypeId = req.PosExecTypeId,
                OrganizationId = req.OrganizationId,
                Name = posExecType?.Name,
                AbbName = posExecType?.AbbName,
                IsActive = true,
                HremployeeId = null,
                Remark = $"สร้างอัตโนมัติจากคำขออัตรากำลัง #{req.Id} (Recruitment)",
                CreateDate = DateTime.Now,
            };
            context.Pos_PositionSlots.Add(slot);
            await context.SaveChangesAsync(ct);
            req.NewPositionSlotId = slot.Id;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Rec_Requisition>> GetAllAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_Requisitions
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.Id)
            .ToListAsync(ct);
    }

    public async Task<Rec_Requisition?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_Requisitions.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    // Approved requisitions still open to post — excludes ones already
    // linked to an open/filled posting so HR doesn't double-post the same seat.
    public async Task<List<Rec_Requisition>> GetPostableAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var postedRequisitionIds = await context.Rec_JobPostings
            .Where(p => p.Status != PostingStatus.Cancelled)
            .Select(p => p.RequisitionId)
            .ToListAsync(ct);

        return await context.Rec_Requisitions
            .Where(r => r.CompanyId == companyId && r.Status == RequisitionStatus.Approved && !postedRequisitionIds.Contains(r.Id))
            .OrderByDescending(r => r.Id)
            .ToListAsync(ct);
    }

    public async Task MarkFilledIfCompleteAsync(long requisitionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var req = await context.Rec_Requisitions.FirstOrDefaultAsync(r => r.Id == requisitionId, ct);
        if (req is null || req.Status != RequisitionStatus.Approved) return;

        var hiredCount = await context.Rec_JobPostings.Where(p => p.RequisitionId == requisitionId)
            .Join(context.Rec_Applications, p => p.Id, a => a.JobPostingId, (p, a) => a)
            .CountAsync(a => a.Stage == ApplicationStage.Hired, ct);

        if (hiredCount >= req.OpeningsCount)
        {
            req.Status = RequisitionStatus.Filled;
            await context.SaveChangesAsync(ct);
        }
    }
}
