using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Att;

// Attendance correction requests: submit -> workflow ATT_CORRECTION (supervisor
// per org chart) -> on COMPLETED, write ManualEntry punches for the day and
// re-aggregate it. Status is applied lazily on read (SyncAsync), the same
// apply-on-read convention the leave and org-change modules use — there is no
// background scheduler in this app.
public class AttendanceCorrectionService(
    IDbContextFactory<HRMContext> dbFactory,
    WorkflowEngineService engine,
    AttendanceAggregationService aggregation)
{
    public const string WorkflowCode = "ATT_CORRECTION";

    public async Task<long> SubmitAsync(long hremployeeId, string companyId, DateOnly workDate, TimeOnly? requestedIn, TimeOnly? requestedOut,
        string reason, long actorUserId, string actorEmpNo, CancellationToken ct = default)
    {
        if (requestedIn is null && requestedOut is null) throw new InvalidOperationException("ระบุเวลาเข้าหรือเวลาออกอย่างน้อยหนึ่งค่า");
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("ระบุเหตุผล");
        if (workDate > DateOnly.FromDateTime(DateTime.Today)) throw new InvalidOperationException("แก้ไขเวลาล่วงหน้าไม่ได้");

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == WorkflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{WorkflowCode}' — ตรวจสอบว่า migration ถูก apply แล้ว");

        var duplicate = await context.Att_CorrectionRequests.AnyAsync(r => r.HremployeeId == hremployeeId && r.WorkDate == workDate
                                                                          && r.Status == AttCorrectionStatus.Pending, ct);
        if (duplicate) throw new InvalidOperationException($"มีคำขอแก้ไขเวลาของวันที่ {workDate:dd/MM/yyyy} รออนุมัติอยู่แล้ว");

        var inDt = requestedIn is TimeOnly ti ? workDate.ToDateTime(ti) : (DateTime?)null;
        var outDt = requestedOut is TimeOnly to ? workDate.ToDateTime(to) : (DateTime?)null;
        if (inDt is not null && outDt is not null && outDt < inDt) outDt = outDt.Value.AddDays(1); // overnight shift

        var req = new Att_CorrectionRequest
        {
            CompanyId = companyId, HremployeeId = hremployeeId, EmpNo = actorEmpNo, WorkDate = workDate,
            RequestedIn = inDt, RequestedOut = outDt, Reason = reason.Trim(),
            Status = AttCorrectionStatus.Pending, RequestedByUserId = actorUserId,
        };
        context.Att_CorrectionRequests.Add(req);
        await context.SaveChangesAsync(ct);

        var subject = $"ขอแก้ไขเวลาเข้างาน {actorEmpNo} วันที่ {workDate:dd/MM/yyyy}"
                      + (inDt is not null ? $" เข้า {inDt:HH:mm}" : "") + (outDt is not null ? $" ออก {outDt:HH:mm}" : "");
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Att_CorrectionRequest", req.Id.ToString(), actorUserId, actorEmpNo, subject, null, ct);
        req.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);
        return req.Id;
    }

    // Reads the workflow outcome and applies it once. Safe to call on every
    // page load for the request.
    public async Task<Att_CorrectionRequest?> SyncAsync(long requestId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var req = await context.Att_CorrectionRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (req is null || req.JobMasterId is null || req.Status != AttCorrectionStatus.Pending) return req;

        var status = await context.job_masters.Where(j => j.jobmasterid == req.JobMasterId).Select(j => j.status).FirstOrDefaultAsync(ct);
        if (status == WorkflowEngineService.StatusCompleted)
        {
            var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == req.HremployeeId, ct);
            if (emp is not null)
            {
                if (req.RequestedIn is DateTime inDt)
                    context.Att_PunchLogs.Add(new Att_PunchLog { CompanyId = req.CompanyId, HremployeeId = emp.id, RawEmpCode = emp.EmpNo, PunchTime = inDt, Direction = AttPunchDirection.In, Source = AttPunchSource.ManualEntry });
                if (req.RequestedOut is DateTime outDt)
                    context.Att_PunchLogs.Add(new Att_PunchLog { CompanyId = req.CompanyId, HremployeeId = emp.id, RawEmpCode = emp.EmpNo, PunchTime = outDt, Direction = AttPunchDirection.Out, Source = AttPunchSource.ManualEntry });
            }
            req.Status = AttCorrectionStatus.Approved;
            req.IsApplied = true;
            req.AppliedDate = DateTime.Now;
            await context.SaveChangesAsync(ct);
            // Rebuild that day's summary so late/absent flags reflect the correction.
            await aggregation.RunAsync(req.CompanyId, req.WorkDate, req.WorkDate, ct);
        }
        else if (status == WorkflowEngineService.StatusRejected)
        {
            req.Status = AttCorrectionStatus.Rejected;
            await context.SaveChangesAsync(ct);
        }
        else if (status == WorkflowEngineService.StatusCancelled)
        {
            req.Status = AttCorrectionStatus.Cancelled;
            await context.SaveChangesAsync(ct);
        }
        return req;
    }

    public async Task<List<Att_CorrectionRequest>> ListMineAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var mine = await context.Att_CorrectionRequests.Where(r => r.HremployeeId == hremployeeId)
            .OrderByDescending(r => r.WorkDate).Take(50).ToListAsync(ct);
        foreach (var r in mine.Where(r => r.Status == AttCorrectionStatus.Pending && r.JobMasterId != null))
            await SyncAsync(r.Id, ct);
        return await context.Att_CorrectionRequests.Where(r => r.HremployeeId == hremployeeId)
            .OrderByDescending(r => r.WorkDate).Take(50).AsNoTracking().ToListAsync(ct);
    }
}
