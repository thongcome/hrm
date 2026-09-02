namespace HRM.Services.Welfare;

using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Orchestrates a welfare claim's life: create an editable draft, then submit it
// into the generic approval engine (WELFARE_CLAIM workflow) — mirrors
// LeaveRequestService's draft → StartJobAsync flow. Status is never stored on
// Wel_Claim; it is read back from job_master. Approval itself happens in the
// existing /wf/my-inbox (the engine routes the job to the Admin/HR role).
public class WelfareClaimService(
    IDbContextFactory<HRMContext> dbFactory,
    WorkflowEngineService engine,
    WelfareBalanceService balanceService)
{
    public async Task<long> CreateDraftAsync(long hremployeeId, string empNo, string companyId,
        long benefitTypeId, DateOnly eventDate, decimal amount, string? description, CancellationToken ct = default)
    {
        var err = await balanceService.ValidateClaimAsync(companyId, benefitTypeId, hremployeeId, amount, eventDate.Year, ct);
        if (err is not null) throw new InvalidOperationException(err);

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var claim = new Wel_Claim
        {
            CompanyId = companyId, HremployeeId = hremployeeId, EmpNo = empNo,
            BenefitTypeId = benefitTypeId, EventDate = eventDate, Amount = amount,
            Description = description, RequestedDate = DateTime.Now,
        };
        context.Wel_Claims.Add(claim);
        await context.SaveChangesAsync(ct);

        claim.ClaimNo = $"WEL-{DateTime.Now:yyyyMM}-{claim.Id:D5}";
        await context.SaveChangesAsync(ct);
        return claim.Id;
    }

    public async Task<long> SubmitAsync(long claimId, long actorUserId, string actorEmpNo, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var claim = await context.Wel_Claims.Include(c => c.BenefitType).FirstOrDefaultAsync(c => c.Id == claimId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้");
        if (claim.JobMasterId is not null)
            throw new InvalidOperationException("คำขอนี้ส่งขออนุมัติแล้ว");

        var err = await balanceService.ValidateClaimAsync(claim.CompanyId, claim.BenefitTypeId, claim.HremployeeId, claim.Amount, claim.EventDate.Year, ct);
        if (err is not null) throw new InvalidOperationException(err);

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == WelfareWorkflowSeeder.WorkflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{WelfareWorkflowSeeder.WorkflowCode}'");

        var subject = $"เบิกสวัสดิการ: {claim.EmpNo} {claim.BenefitType.NameTh} {claim.Amount:N0} บาท";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Wel_Claim", claimId.ToString(),
            actorUserId, actorEmpNo, subject, claim.Amount, ct);

        claim.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);
        return jobId;
    }

    // Delete a draft that was never submitted (no history to keep, same as
    // LeaveRequestService.CancelAsync's draft path).
    public async Task DeleteDraftAsync(long claimId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var claim = await context.Wel_Claims.FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null) return;
        if (claim.JobMasterId is not null)
            throw new InvalidOperationException("คำขอที่ส่งอนุมัติแล้วยกเลิกที่นี่ไม่ได้");
        context.Wel_Claims.Remove(claim);
        await context.SaveChangesAsync(ct);
    }
}
