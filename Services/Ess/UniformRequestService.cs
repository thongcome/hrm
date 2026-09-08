namespace HRM.Services.Ess;

using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// End-to-end proof-of-concept for the generic Workflow Approval Engine
// (CEO, 2026-09-08) — mirrors Services/Leave/LeaveRequestService.cs's
// CreateDraftAsync/SubmitAsync shape exactly, minus the leave-specific
// policy/balance/attachment rules a brand-new, deliberately simple module
// doesn't need. Workflow definition ("UNIFORM_REQUEST") is created live
// through the existing /wf/workflows + /wf/sub-workflow-master admin pages,
// not seeded by a migration — this module only needs the workflow to
// EXIST by that code when SubmitAsync runs, same as every other module
// that calls into the engine.
public class UniformRequestService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    public const string WorkflowCode = "UNIFORM_REQUEST";

    public async Task<long> CreateDraftAsync(long hremployeeId, string uniformType, string size, int quantity, string? reason, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        if (string.IsNullOrWhiteSpace(uniformType))
            throw new InvalidOperationException("กรุณาเลือกประเภทชุด");
        if (string.IsNullOrWhiteSpace(size))
            throw new InvalidOperationException("กรุณาเลือกไซส์");
        if (quantity < 1)
            throw new InvalidOperationException("จำนวนต้องมากกว่า 0");

        var request = new Emp_UniformRequest
        {
            HremployeeId = emp.id,
            EmpNo = emp.EmpNo,
            CompanyId = emp.companyid,
            UniformType = uniformType.Trim(),
            Size = size.Trim(),
            Quantity = quantity,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            RequestedDate = DateTime.Now,
        };
        context.Emp_UniformRequests.Add(request);
        await context.SaveChangesAsync(ct);
        return request.Id;
    }

    public async Task<long> SubmitAsync(long requestId, long actorUserId, string? actorEmpNo, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var request = await context.Emp_UniformRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้");
        if (request.JobMasterId is not null)
            throw new InvalidOperationException("คำขอนี้ถูกส่งขออนุมัติไปแล้ว");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == WorkflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{WorkflowCode}' — ต้องสร้าง workflow นี้ที่ /wf/workflows ก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var subject = $"ขอเบิกชุดยูนิฟอร์ม: {request.EmpNo} {request.UniformType} ไซส์ {request.Size} x{request.Quantity}";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Emp_UniformRequest", requestId.ToString(),
            actorUserId, actorEmpNo, subject, null, ct);

        request.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);
        return jobId;
    }

    public async Task<List<Emp_UniformRequest>> GetMyRequestsAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Emp_UniformRequests
            .Where(r => r.HremployeeId == hremployeeId)
            .OrderByDescending(r => r.RequestedDate)
            .ToListAsync(ct);
    }
}
