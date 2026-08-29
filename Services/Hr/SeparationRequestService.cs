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
            }
        }
        else
        {
            request.Status = SeparationRequestStatus.Rejected;
        }

        await context.SaveChangesAsync(ct);
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
