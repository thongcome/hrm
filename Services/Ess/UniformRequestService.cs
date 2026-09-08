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

    // The domain table carries NO workflow column (CEO, 8 ก.ย. 2569:
    // "jobmasterid ไม่ควรมาอยู่ใน domain, jobmaster ต้อง link ด้วย id มาที่
    // domain"). job_master already stores which entity and which row it is
    // approving — reftable + refid — so that pair IS the link, owned by the
    // workflow side alone. This constant is the one place the entity name is
    // spelled, shared by StartJobAsync and every lookup below.
    public const string RefTable = nameof(Emp_UniformRequest);

    // The job approving a given request, or null while it's still a draft.
    // Replaces what a JobMasterId column used to answer.
    public async Task<job_master?> GetJobAsync(long requestId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await FindJobAsync(context, requestId, ct);
    }

    // Batch form of GetJobAsync for list screens — one query for the whole
    // page instead of one per row.
    public async Task<Dictionary<long, job_master>> GetJobsAsync(IEnumerable<long> requestIds, CancellationToken ct = default)
    {
        var ids = requestIds.Select(id => id.ToString()).ToList();
        if (ids.Count == 0) return new();

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var jobs = await context.job_masters
            .Where(j => j.reftable == RefTable && j.refid != null && ids.Contains(j.refid))
            .ToListAsync(ct);

        // A resubmitted request can own more than one job row over its life;
        // the newest one is the live state, so it wins.
        return jobs
            .GroupBy(j => long.Parse(j.refid!))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(j => j.jobmasterid).First());
    }

    private static Task<job_master?> FindJobAsync(HRMContext context, long requestId, CancellationToken ct)
    {
        var refid = requestId.ToString();
        return context.job_masters
            .Where(j => j.reftable == RefTable && j.refid == refid)
            .OrderByDescending(j => j.jobmasterid)
            .FirstOrDefaultAsync(ct);
    }

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
        // "Already submitted?" is now a question for job_master, not for a
        // column on this row — the workflow side owns that fact.
        var openJob = await FindJobAsync(context, requestId, ct);
        if (openJob is not null && openJob.isJobClosed != true)
            throw new InvalidOperationException("คำขอนี้ถูกส่งขออนุมัติไปแล้ว");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == WorkflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{WorkflowCode}' — ต้องสร้าง workflow นี้ที่ /wf/workflows ก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var subject = $"ขอเบิกชุดยูนิฟอร์ม: {request.EmpNo} {request.UniformType} ไซส์ {request.Size} x{request.Quantity}";
        // StartJobAsync writes reftable + refid onto job_master — which is
        // the whole link. Nothing to write back onto the request row.
        return await engine.StartJobAsync(workflow.workflowid, RefTable, requestId.ToString(),
            actorUserId, actorEmpNo, subject, null, ct);
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
