using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Hr;

// Routes "end this employee's employment" through the generic workflow
// engine instead of writing Hremployee.ResignDate directly — mirrors
// Services/Org/OrgChangeRequestService.cs / Services/Perf/PerfApprovalService.cs's
// submit -> job -> lazy-apply-on-read pattern. No scheduler exists anywhere
// in this app, so SyncStatusFromJobAsync must be called from page load
// (see PayrollEmployeeAdminDetail.razor's OnParametersSetAsync) rather than
// relying on anything pushing the result back.
public class SeparationRequestService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    private const string WorkflowCode = "EMPLOYEE_SEPARATION_APPROVAL";

    public async Task<long> SubmitAsync(long hremployeeId, SeparationType separationType, DateTime effectiveDate, string? reason,
        long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        if (emp.ResignDate is not null)
            throw new InvalidOperationException("พนักงานคนนี้มีวันที่ลาออก/สิ้นสุดการจ้างงานบันทึกไว้แล้ว");

        var alreadyPending = await context.Hr_SeparationRequests.AnyAsync(r =>
            r.HremployeeId == hremployeeId && r.Status == SeparationRequestStatus.PendingApproval, ct);
        if (alreadyPending)
            throw new InvalidOperationException("มีคำขอสิ้นสุดการจ้างงานของพนักงานคนนี้รออนุมัติอยู่แล้ว");

        var request = new Hr_SeparationRequest
        {
            HremployeeId = hremployeeId,
            EmpNo = emp.EmpNo,
            CompanyId = emp.companyid,
            SeparationType = separationType,
            EffectiveDate = effectiveDate,
            Reason = reason,
            RequestedByUserId = requesterUserId,
            RequestedDate = DateTime.Now,
        };
        context.Hr_SeparationRequests.Add(request);
        await context.SaveChangesAsync(ct); // need request.Id before starting the workflow job

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == WorkflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{WorkflowCode}' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งคำขอใหม่ได้");

        var subject = $"ขอสิ้นสุดการจ้างงาน: {emp.EmpName} {emp.EmpSurname} ({SeparationTypeLabel(separationType)}) มีผล {effectiveDate:dd/MM/yyyy}";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Hr_SeparationRequest", request.Id.ToString(),
            requesterUserId, requesterEmpId, subject, amount: null, ct);

        var toPatch = await context.Hr_SeparationRequests.FirstAsync(r => r.Id == request.Id, ct);
        toPatch.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);

        return request.Id;
    }

    // Lazy apply-on-read: called from PayrollEmployeeAdminDetail.razor's
    // OnParametersSetAsync on every load. A no-op unless there's a still-
    // pending request whose job has since closed.
    public async Task SyncStatusFromJobAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var request = await context.Hr_SeparationRequests
            .Where(r => r.HremployeeId == hremployeeId && r.Status == SeparationRequestStatus.PendingApproval && r.JobMasterId != null)
            .FirstOrDefaultAsync(ct);
        if (request is null) return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == request.JobMasterId, ct);
        if (job is null || job.isJobClosed != true) return;

        if (job.status == WorkflowEngineService.StatusCompleted)
        {
            request.Status = SeparationRequestStatus.Approved;

            var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == request.HremployeeId, ct);
            if (emp is not null && emp.ResignDate is null) // guard against a double-apply race
            {
                emp.ResignDate = request.EffectiveDate;
                emp.SeparationType = request.SeparationType;

                // Close any still-open PF membership period — nothing else in
                // the codebase writes Pay_ProvidentFundMembershipPeriod.LeaveDate,
                // so without this a departed employee's membership clock never
                // stops even though they're no longer contributing.
                var openPfPeriod = await context.Pay_ProvidentFundMembershipPeriods
                    .Where(p => p.HremployeeId == emp.id && p.LeaveDate == null)
                    .FirstOrDefaultAsync(ct);
                if (openPfPeriod is not null)
                    openPfPeriod.LeaveDate = DateOnly.FromDateTime(request.EffectiveDate);

                // Releasing the establishment slot is EFFECTIVE-DATE-driven, not
                // approval-driven (owner, 2026-09-03: "พ้นสภาพ ต้องดู effectivedate
                // เป็นหลัก") — an employee approved to leave in 30 days is still
                // working and still occupies their อัตรา until that date. So we
                // only release inline here for a same-day/backdated separation;
                // future-dated ones are released by ApplyDueSeparationsAsync when
                // their effective date arrives (lazy apply-on-read, no scheduler
                // exists — same pattern as OrgChangeRequestService.ApplyDueChangesAsync).
                if (emp.ResignDate <= DateTime.Today)
                    await ReleaseSlotsAsync(context, emp.id, ct);
            }
        }
        else
        {
            request.Status = SeparationRequestStatus.Rejected;
        }

        await context.SaveChangesAsync(ct);
    }

    // Clears every establishment slot this employee occupies so the อัตรา reads
    // vacant again (vacancy is derived from occupancy — PositionSlotAdmin shows
    // HremployeeId ?? "ว่าง", OrgChartNodeBuilder treats a null-occupant slot as
    // vacant). Nothing else in the codebase clears Pos_PositionSlot.HremployeeId,
    // so without this a departed employee keeps occupying their อัตรา and the
    // headcount/vacant count stays wrong. Caller decides WHEN (effective date).
    private static async Task ReleaseSlotsAsync(HRMContext context, long hremployeeId, CancellationToken ct)
    {
        var occupied = await context.Pos_PositionSlots.Where(s => s.HremployeeId == hremployeeId).ToListAsync(ct);
        foreach (var slot in occupied)
        {
            slot.HremployeeId = null;
            slot.EmpNo = null;
        }
    }

    // Lazy apply-on-read (no scheduler exists — mirrors
    // OrgChangeRequestService.ApplyDueChangesAsync): releases the อัตรา of every
    // employee whose separation effective date (Hremployee.ResignDate, set to the
    // approved EffectiveDate) has arrived but who still occupies a slot. Called
    // from the top of pages where vacancy matters (slot admin, org chart,
    // headcount). Idempotent — a released slot has a null occupant and won't match
    // again. Returns how many slots were freed.
    public async Task<int> ApplyDueSeparationsAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var today = DateTime.Today;
        var dueSlots = await (from s in context.Pos_PositionSlots
                              join e in context.Hremployee on s.HremployeeId equals e.id
                              where s.HremployeeId != null && e.ResignDate != null && e.ResignDate <= today
                              select s).ToListAsync(ct);
        foreach (var slot in dueSlots)
        {
            slot.HremployeeId = null;
            slot.EmpNo = null;
        }
        if (dueSlots.Count > 0)
            await context.SaveChangesAsync(ct);
        return dueSlots.Count;
    }

    // Postpone (or bring forward) an already-approved separation's effective date.
    // Owner rule (2026-09-03): "ถ้าเลื่อนลาออก ก็ต้องมีประวัติ (ผ่าน workflow)" — a
    // date change is NOT a silent edit; it is recorded as an Hr_SeparationDateChange
    // history row and routed through the same approval workflow. The live
    // Hr_SeparationRequest.EffectiveDate / Hremployee.ResignDate only move once the
    // change is approved (SyncDateChangeStatusFromJobAsync).
    public async Task<long> RescheduleAsync(long hremployeeId, DateTime newEffectiveDate, string? reason,
        long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var request = await context.Hr_SeparationRequests
            .Where(r => r.HremployeeId == hremployeeId && r.Status == SeparationRequestStatus.Approved)
            .OrderByDescending(r => r.RequestedDate)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอสิ้นสุดการจ้างงานที่อนุมัติแล้วสำหรับพนักงานคนนี้ — เลื่อนได้เฉพาะรายการที่อนุมัติแล้ว");

        if (request.EffectiveDate.Date <= DateTime.Today)
            throw new InvalidOperationException("วันที่มีผลเดิมผ่านมาแล้ว (พนักงานพ้นสภาพไปแล้ว) — เลื่อนไม่ได้");
        if (newEffectiveDate.Date == request.EffectiveDate.Date)
            throw new InvalidOperationException("วันที่ใหม่ตรงกับวันเดิม");

        var pendingChange = await context.Hr_SeparationDateChanges.AnyAsync(c =>
            c.SeparationRequestId == request.Id && c.Status == SeparationRequestStatus.PendingApproval, ct);
        if (pendingChange)
            throw new InvalidOperationException("มีคำขอเลื่อนวันของรายการนี้รออนุมัติอยู่แล้ว");

        var change = new Hr_SeparationDateChange
        {
            SeparationRequestId = request.Id,
            HremployeeId = hremployeeId,
            EmpNo = request.EmpNo,
            CompanyId = request.CompanyId,
            OldEffectiveDate = request.EffectiveDate,
            NewEffectiveDate = newEffectiveDate,
            Reason = reason,
            Status = SeparationRequestStatus.PendingApproval,
            RequestedByUserId = requesterUserId,
            RequestedDate = DateTime.Now,
        };
        context.Hr_SeparationDateChanges.Add(change);
        await context.SaveChangesAsync(ct); // need change.Id before starting the job

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == WorkflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{WorkflowCode}' — ติดต่อแอดมินให้ตั้งค่าก่อน");

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);
        var subject = $"ขอเลื่อนวันสิ้นสุดการจ้างงาน: {emp?.EmpName} {emp?.EmpSurname} จาก {request.EffectiveDate:dd/MM/yyyy} → {newEffectiveDate:dd/MM/yyyy}";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Hr_SeparationDateChange", change.Id.ToString(),
            requesterUserId, requesterEmpId, subject, amount: null, ct);

        var toPatch = await context.Hr_SeparationDateChanges.FirstAsync(c => c.Id == change.Id, ct);
        toPatch.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);

        return change.Id;
    }

    // Lazy apply-on-read for reschedule requests — same idiom as
    // SyncStatusFromJobAsync. When an approved reschedule's job closes, move the
    // live separation's EffectiveDate + the employee's ResignDate (and its PF
    // leave date) to the new date; a rejected reschedule keeps the original date.
    public async Task SyncDateChangeStatusFromJobAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var change = await context.Hr_SeparationDateChanges
            .Where(c => c.HremployeeId == hremployeeId && c.Status == SeparationRequestStatus.PendingApproval && c.JobMasterId != null)
            .OrderByDescending(c => c.RequestedDate)
            .FirstOrDefaultAsync(ct);
        if (change is null) return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == change.JobMasterId, ct);
        if (job is null || job.isJobClosed != true) return;

        if (job.status == WorkflowEngineService.StatusCompleted)
        {
            change.Status = SeparationRequestStatus.Approved;

            var request = await context.Hr_SeparationRequests.FirstOrDefaultAsync(r => r.Id == change.SeparationRequestId, ct);
            if (request is not null)
                request.EffectiveDate = change.NewEffectiveDate;

            var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);
            if (emp is not null)
            {
                var oldResign = emp.ResignDate;
                emp.ResignDate = change.NewEffectiveDate;

                // Keep the PF membership leave date in step with the moved date.
                if (oldResign is not null)
                {
                    var oldLeave = DateOnly.FromDateTime(oldResign.Value);
                    var pfPeriod = await context.Pay_ProvidentFundMembershipPeriods
                        .Where(p => p.HremployeeId == emp.id && p.LeaveDate == oldLeave)
                        .FirstOrDefaultAsync(ct);
                    if (pfPeriod is not null)
                        pfPeriod.LeaveDate = DateOnly.FromDateTime(change.NewEffectiveDate);
                }

                // If the new date is already due, free the อัตรา now.
                if (emp.ResignDate <= DateTime.Today)
                    await ReleaseSlotsAsync(context, emp.id, ct);
            }
        }
        else
        {
            change.Status = SeparationRequestStatus.Rejected;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Hr_SeparationDateChange>> GetDateChangeHistoryAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Hr_SeparationDateChanges
            .Where(c => c.HremployeeId == hremployeeId)
            .OrderByDescending(c => c.RequestedDate)
            .ToListAsync(ct);
    }

    public async Task<Hr_SeparationRequest?> GetLatestAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Hr_SeparationRequests
            .Where(r => r.HremployeeId == hremployeeId)
            .OrderByDescending(r => r.RequestedDate)
            .FirstOrDefaultAsync(ct);
    }

    public static string SeparationTypeLabel(SeparationType type) => type switch
    {
        SeparationType.VoluntaryResignation => "ลาออกเอง",
        SeparationType.TerminationOrdinary => "เลิกจ้าง (ไม่เข้าข่ายมาตรา 119 — มีค่าชดเชย)",
        SeparationType.TerminationSection119 => "เลิกจ้าง (เข้าข่ายมาตรา 119 — ไม่มีค่าชดเชย)",
        _ => type.ToString(),
    };
}
