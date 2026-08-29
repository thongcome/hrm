namespace HRM.Services.Rec;

using HRM.Models;
using HRM.Services.Hrd;
using HRM.Services.Shared;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Offer lifecycle: draft -> submit for approval (OFFER_APPROVAL workflow) ->
// lazy status sync -> send -> record candidate response -> SubmitForHireApprovalAsync
// (starts a SECOND, separate HIRE_APPROVAL workflow — gates ConfirmHireAsync,
// the most consequential method in the whole Rec_* module, which creates the
// real Hremployee row and hands off to onboarding).
public class RecOfferService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine, LifecycleTaskService lifecycleTaskService, RecRequisitionService requisitionService)
{
    private const string HireWorkflowCode = "HIRE_APPROVAL";

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

    // Second, separate approval gate — starts only once the candidate has
    // accepted, and mirrors SubmitForApprovalAsync's shape but against
    // Rec_Offer.HireJobMasterId instead of JobMasterId (that field only ever
    // gated the offer's own terms, not the decision to actually create an
    // employee record). Requires candidate.IdCard so
    // EmployeeIdentityHelper.CheckAsync can catch a former employee before
    // the workflow even starts — cheaper to block here than to discover the
    // duplicate at ConfirmHireAsync after approval has already been spent.
    public async Task<long> SubmitForHireApprovalAsync(long offerId, long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var offer = await context.Rec_Offers.FirstOrDefaultAsync(o => o.Id == offerId, ct)
            ?? throw new InvalidOperationException("ไม่พบข้อเสนอนี้");
        if (offer.Status != OfferStatus.Accepted)
            throw new InvalidOperationException("ส่งอนุมัติการจ้างได้เฉพาะข้อเสนอที่ผู้สมัครตอบรับแล้วเท่านั้น");
        if (offer.HiredHremployeeId is not null)
            throw new InvalidOperationException("จ้างพนักงานคนนี้ไปแล้ว");

        var app = await context.Rec_Applications.FirstOrDefaultAsync(a => a.Id == offer.ApplicationId, ct)
            ?? throw new InvalidOperationException("ไม่พบใบสมัครนี้");
        var candidate = await context.Rec_Candidates.FirstOrDefaultAsync(c => c.Id == app.CandidateId, ct)
            ?? throw new InvalidOperationException("ไม่พบผู้สมัครนี้");
        if (string.IsNullOrWhiteSpace(candidate.IdCard))
            throw new InvalidOperationException("ต้องกรอกเลขบัตรประชาชนของผู้สมัครก่อน (ที่หน้าข้อมูลผู้สมัคร) จึงจะส่งอนุมัติการจ้างได้");

        var identityCheck = await EmployeeIdentityHelper.CheckAsync(context, candidate.IdCard, ct);
        if (identityCheck.Result == IdCardMatchResult.MatchesActiveEmployee)
            throw new InvalidOperationException($"เลขบัตรประชาชนนี้ตรงกับพนักงานที่ยังทำงานอยู่ ({identityCheck.Matched!.EmpNo} {identityCheck.Matched.EmpName} {identityCheck.Matched.EmpSurname}) ไม่สามารถจ้างซ้ำได้");
        if (identityCheck.Result == IdCardMatchResult.MatchesDepartedEmployee)
            throw new InvalidOperationException($"เลขบัตรประชาชนนี้ตรงกับอดีตพนักงาน ({identityCheck.Matched!.EmpNo} {identityCheck.Matched.EmpName} {identityCheck.Matched.EmpSurname}) กรุณาใช้หน้า \"รับพนักงานเก่ากลับเข้างาน\" แทน");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == HireWorkflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{HireWorkflowCode}' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งอนุมัติได้");

        var subject = $"อนุมัติการจ้าง: {candidate.FirstName} {candidate.LastName}";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Rec_Offer", offer.Id.ToString(),
            requesterUserId, requesterEmpId, subject, offer.OfferedSalary, ct);

        offer.HireJobMasterId = jobId;
        offer.Status = OfferStatus.PendingHireApproval;
        await context.SaveChangesAsync(ct);
        return jobId;
    }

    // Lazy apply-on-read for the hire-approval gate. Reverts to Accepted (not
    // Withdrawn) on rejection so HR can fix whatever blocked it and resubmit
    // — the candidate's acceptance is still on file, only the internal
    // approval failed.
    public async Task SyncHireApprovalStatusFromJobAsync(long offerId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var offer = await context.Rec_Offers.FirstOrDefaultAsync(o => o.Id == offerId, ct);
        if (offer is null || offer.Status != OfferStatus.PendingHireApproval || offer.HireJobMasterId is null)
            return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == offer.HireJobMasterId, ct);
        if (job is null || job.isJobClosed != true)
            return;

        if (job.status != WorkflowEngineService.StatusCompleted)
        {
            offer.Status = OfferStatus.Accepted;
            await context.SaveChangesAsync(ct);
        }
    }

    // The consequential step: turns an approved-for-hire offer into a real
    // Hremployee, assigns them to the target seat, and kicks off onboarding —
    // mirrors PayrollEmployeeAdmin.razor's create-employee flow (EmpNo
    // auto-gen, mandatory email via EmployeeEmailResolver) plus
    // EmployeePositionSync.SyncAsync (the same helper PositionSlotAdmin.razor
    // uses) plus LifecycleTaskService.StartOnboardingAsync (idempotent, safe
    // to call unconditionally right after creating the new hire). Gated on
    // HireJobMasterId being approved — checked directly against the job, same
    // pattern as MarkSentAsync — rather than trusting offer.Status alone.
    public async Task<long> ConfirmHireAsync(long offerId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var offer = await context.Rec_Offers.FirstOrDefaultAsync(o => o.Id == offerId, ct)
            ?? throw new InvalidOperationException("ไม่พบข้อเสนอนี้");
        if (offer.HiredHremployeeId is not null)
            throw new InvalidOperationException("จ้างพนักงานคนนี้ไปแล้ว");

        var hireApproved = offer.HireJobMasterId is not null &&
            await context.job_masters.AnyAsync(j => j.jobmasterid == offer.HireJobMasterId && j.isJobClosed == true && j.status == WorkflowEngineService.StatusCompleted, ct);
        if (!hireApproved)
            throw new InvalidOperationException("การจ้างพนักงานคนนี้ยังไม่ผ่านการอนุมัติ");

        var slot = await context.Pos_PositionSlots.FirstOrDefaultAsync(s => s.Id == offer.TargetPositionSlotId, ct)
            ?? throw new InvalidOperationException("ไม่พบเลขที่อัตราเป้าหมาย");
        var app = await context.Rec_Applications.FirstOrDefaultAsync(a => a.Id == offer.ApplicationId, ct)
            ?? throw new InvalidOperationException("ไม่พบใบสมัครนี้");
        var candidate = await context.Rec_Candidates.FirstOrDefaultAsync(c => c.Id == app.CandidateId, ct)
            ?? throw new InvalidOperationException("ไม่พบผู้สมัครนี้");

        var empNo = await EmployeeIdentityHelper.GenerateNextEmpNoAsync(context, slot.CompanyId, ct);

        var emp = new Hremployee
        {
            companyid = slot.CompanyId,
            EmpNo = empNo,
            EmpName = candidate.FirstName,
            EmpSurname = candidate.LastName,
            IdCard = candidate.IdCard,
            WorkDate = offer.StartDate.ToDateTime(TimeOnly.MinValue),
            SalaryAmt = offer.OfferedSalary,
        };
        context.Hremployee.Add(emp);
        await context.SaveChangesAsync(ct);

        await EmployeeEmailResolver.SetAsync(context, emp.id, candidate.Email, ct);

        // Carry over whatever education/experience the candidate typed on
        // the apply form (or HR added on CandidateDetail.razor) into the new
        // employee's real personnel record — field-for-field copy, since
        // Rec_CandidateEducation/Experience mirror Hrd_Education/Experience
        // shape exactly for this purpose. Saves HR from re-typing history
        // that's already on file.
        var candidateEducation = await context.Rec_CandidateEducations.Where(e => e.CandidateId == candidate.Id && e.IsActive).ToListAsync(ct);
        foreach (var edu in candidateEducation)
        {
            context.Hrd_Educations.Add(new Hrd_Education
            {
                HremployeeId = emp.id,
                Level = edu.Level,
                Degree = edu.Degree,
                Major = edu.Major,
                MajorSubject = edu.MajorSubject,
                Faculty = edu.Faculty,
                Institute = edu.Institute,
                Country = edu.Country,
                EntryDate = edu.EntryDate,
                FinishedDate = edu.FinishedDate,
                Gpa = edu.Gpa,
                IsHonors = edu.IsHonors,
                IsHighestDegree = edu.IsHighestDegree,
                Remark = edu.Remark,
            });
        }

        var candidateExperience = await context.Rec_CandidateExperiences.Where(e => e.CandidateId == candidate.Id && e.IsActive).ToListAsync(ct);
        foreach (var exp in candidateExperience)
        {
            context.Hrd_Experiences.Add(new Hrd_Experience
            {
                HremployeeId = emp.id,
                StartDate = exp.StartDate,
                EndDate = exp.EndDate,
                Position = exp.Position,
                Company = exp.Company,
                Remark = exp.Remark,
            });
        }
        await context.SaveChangesAsync(ct);

        var oldHremployeeId = slot.HremployeeId;
        slot.HremployeeId = emp.id;
        await EmployeePositionSync.SyncAsync(context, slot, oldHremployeeId, actorUserId: actorUserId, ct: ct);
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
}
