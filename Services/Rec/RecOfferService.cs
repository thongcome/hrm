namespace HRM.Services.Rec;

using System.Text.RegularExpressions;
using HRM.Models;
using HRM.Services.Hrd;
using HRM.Services.Shared;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Offer lifecycle: draft -> submit for approval (OFFER_APPROVAL workflow) ->
// lazy status sync -> send -> record candidate response -> HireAsync (the
// most consequential method in the whole Rec_* module — creates the real
// Hremployee row and hands off to onboarding).
public class RecOfferService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine, LifecycleTaskService lifecycleTaskService, RecRequisitionService requisitionService)
{
    public async Task<long> CreateDraftAsync(Rec_Offer draft, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        draft.Id = 0;
        draft.Status = OfferStatus.Draft;
        draft.JobMasterId = null;
        draft.HiredHremployeeId = null;
        context.Rec_Offers.Add(draft);
        await context.SaveChangesAsync(ct);
        return draft.Id;
    }

    public async Task<long> SubmitForApprovalAsync(long offerId, long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var offer = await context.Rec_Offers.FirstOrDefaultAsync(o => o.Id == offerId, ct)
            ?? throw new InvalidOperationException("ไม่พบข้อเสนอนี้");
        if (offer.Status != OfferStatus.Draft)
            throw new InvalidOperationException("ส่งอนุมัติได้เฉพาะข้อเสนอที่ยังเป็นฉบับร่าง (Draft) เท่านั้น");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "OFFER_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow 'OFFER_APPROVAL' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var app = await context.Rec_Applications.FirstOrDefaultAsync(a => a.Id == offer.ApplicationId, ct);
        var candidate = app is null ? null : await context.Rec_Candidates.FirstOrDefaultAsync(c => c.Id == app.CandidateId, ct);
        var subject = $"ข้อเสนอจ้างงาน: {candidate?.FirstName} {candidate?.LastName} — {offer.OfferedSalary:N2} บาท";

        var jobId = await engine.StartJobAsync(workflow.workflowid, "Rec_Offer", offer.Id.ToString(),
            requesterUserId, requesterEmpId, subject, offer.OfferedSalary, ct);

        offer.JobMasterId = jobId;
        offer.Status = OfferStatus.PendingApproval;
        await context.SaveChangesAsync(ct);
        return jobId;
    }

    // Lazy apply-on-read — mirrors every other approval flow in this codebase.
    public async Task SyncStatusFromJobAsync(long offerId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var offer = await context.Rec_Offers.FirstOrDefaultAsync(o => o.Id == offerId, ct);
        if (offer is null || offer.Status != OfferStatus.PendingApproval || offer.JobMasterId is null)
            return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == offer.JobMasterId, ct);
        if (job is null || job.isJobClosed != true)
            return;

        // Approved offers move to Draft-equivalent "ready to send" state —
        // reuse Draft is wrong (already went through approval), so land on
        // Sent only once HR actually emails it; until then treat approval as
        // unlocking the "ส่งอีเมล" action. We model that as still PendingApproval
        // -> Sent transition being manual (SendAsync), so here just flip to
        // a state that unblocks sending: reuse Status field creatively is
        // avoided — instead, approval directly enables SendAsync by checking
        // job status there. So this method only needs to catch rejection.
        if (job.status != WorkflowEngineService.StatusCompleted)
        {
            offer.Status = OfferStatus.Withdrawn;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<Rec_Offer?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.Rec_Offers.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<List<Rec_Offer>> GetAllForCompanyAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var slotIds = await context.Pos_PositionSlots.Where(s => s.CompanyId == companyId).Select(s => s.Id).ToListAsync(ct);
        return await context.Rec_Offers.Where(o => slotIds.Contains(o.TargetPositionSlotId)).OrderByDescending(o => o.Id).ToListAsync(ct);
    }

    // Checks approval via the job directly (see comment in
    // SyncStatusFromJobAsync) rather than relying on offer.Status alone.
    public async Task MarkSentAsync(long offerId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var offer = await context.Rec_Offers.FirstOrDefaultAsync(o => o.Id == offerId, ct)
            ?? throw new InvalidOperationException("ไม่พบข้อเสนอนี้");

        var approved = offer.JobMasterId is not null &&
            await context.job_masters.AnyAsync(j => j.jobmasterid == offer.JobMasterId && j.isJobClosed == true && j.status == WorkflowEngineService.StatusCompleted, ct);
        if (!approved)
            throw new InvalidOperationException("ข้อเสนอนี้ยังไม่ผ่านการอนุมัติ");

        offer.Status = OfferStatus.Sent;
        offer.SentDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }

    public async Task RecordResponseAsync(long offerId, bool accepted, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var offer = await context.Rec_Offers.FirstOrDefaultAsync(o => o.Id == offerId, ct)
            ?? throw new InvalidOperationException("ไม่พบข้อเสนอนี้");
        if (offer.Status != OfferStatus.Sent)
            throw new InvalidOperationException("บันทึกผลตอบรับได้เฉพาะข้อเสนอที่ส่งแล้วเท่านั้น");

        offer.Status = accepted ? OfferStatus.Accepted : OfferStatus.Declined;
        offer.RespondedDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }

    // The consequential step: turns an accepted offer into a real Hremployee,
    // assigns them to the target seat, and kicks off onboarding — mirrors
    // PayrollEmployeeAdmin.razor's create-employee flow (EmpNo auto-gen,
    // mandatory email via EmployeeEmailResolver) plus
    // EmployeePositionSync.SyncAsync (the same helper PositionSlotAdmin.razor
    // uses) plus LifecycleTaskService.StartOnboardingAsync (idempotent, safe
    // to call unconditionally right after creating the new hire).
    public async Task<long> HireAsync(long offerId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var offer = await context.Rec_Offers.FirstOrDefaultAsync(o => o.Id == offerId, ct)
            ?? throw new InvalidOperationException("ไม่พบข้อเสนอนี้");
        if (offer.Status != OfferStatus.Accepted)
            throw new InvalidOperationException("จ้างได้เฉพาะข้อเสนอที่ผู้สมัครตอบรับแล้วเท่านั้น");
        if (offer.HiredHremployeeId is not null)
            throw new InvalidOperationException("จ้างพนักงานคนนี้ไปแล้ว");

        var slot = await context.Pos_PositionSlots.FirstOrDefaultAsync(s => s.Id == offer.TargetPositionSlotId, ct)
            ?? throw new InvalidOperationException("ไม่พบเลขที่อัตราเป้าหมาย");
        var app = await context.Rec_Applications.FirstOrDefaultAsync(a => a.Id == offer.ApplicationId, ct)
            ?? throw new InvalidOperationException("ไม่พบใบสมัครนี้");
        var candidate = await context.Rec_Candidates.FirstOrDefaultAsync(c => c.Id == app.CandidateId, ct)
            ?? throw new InvalidOperationException("ไม่พบผู้สมัครนี้");

        var empNo = await GenerateNextEmpNoAsync(context, slot.CompanyId, ct);

        var emp = new Hremployee
        {
            companyid = slot.CompanyId,
            EmpNo = empNo,
            EmpName = candidate.FirstName,
            EmpSurname = candidate.LastName,
            WorkDate = offer.StartDate.ToDateTime(TimeOnly.MinValue),
            SalaryAmt = offer.OfferedSalary,
        };
        context.Hremployee.Add(emp);
        await context.SaveChangesAsync(ct);

        await EmployeeEmailResolver.SetAsync(context, emp.id, candidate.Email, ct);

        var oldHremployeeId = slot.HremployeeId;
        slot.HremployeeId = emp.id;
        await EmployeePositionSync.SyncAsync(context, slot, oldHremployeeId, ct);
        await context.SaveChangesAsync(ct);

        offer.HiredHremployeeId = emp.id;
        app.Stage = ApplicationStage.Hired;
        app.LastUpdatedByUserId = actorUserId;
        await context.SaveChangesAsync(ct);

        await lifecycleTaskService.StartOnboardingAsync(emp.id, actorUserId, ct);

        var posting = await context.Rec_JobPostings.FirstOrDefaultAsync(p => p.Id == app.JobPostingId, ct);
        if (posting is not null)
            await requisitionService.MarkFilledIfCompleteAsync(posting.RequisitionId, ct);

        return emp.id;
    }

    private static async Task<string> GenerateNextEmpNoAsync(HRMContext context, string? companyId, CancellationToken ct)
    {
        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        var prefix = settings?.EmpCodePrefix ?? string.Empty;
        var digits = settings is null || settings.EmpCodeDigits < 1 ? 3 : settings.EmpCodeDigits;

        int next;
        if (settings?.EmpCodeNextNumber is int statefulNext)
        {
            next = statefulNext;
        }
        else
        {
            var existingNumbers = await context.Hremployee.Where(e => e.EmpNo.StartsWith(prefix)).Select(e => e.EmpNo).ToListAsync(ct);
            var pattern = $"^{Regex.Escape(prefix)}(\\d+)$";
            next = existingNumbers
                .Select(no => Regex.Match(no, pattern))
                .Where(m => m.Success)
                .Select(m => int.Parse(m.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max() + 1;
        }

        var candidate = prefix + next.ToString(new string('0', digits));
        if (await context.Hremployee.AnyAsync(e => e.EmpNo == candidate, ct))
        {
            // Extremely unlikely race with the stateful counter path — fall
            // back to scanning for real rather than crashing HireAsync.
            var existingNumbers = await context.Hremployee.Where(e => e.EmpNo.StartsWith(prefix)).Select(e => e.EmpNo).ToListAsync(ct);
            var pattern = $"^{Regex.Escape(prefix)}(\\d+)$";
            next = existingNumbers
                .Select(no => Regex.Match(no, pattern))
                .Where(m => m.Success)
                .Select(m => int.Parse(m.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max() + 1;
            candidate = prefix + next.ToString(new string('0', digits));
        }

        if (settings?.EmpCodeNextNumber is not null)
        {
            settings.EmpCodeNextNumber++;
            await context.SaveChangesAsync(ct);
        }

        return candidate;
    }
}
