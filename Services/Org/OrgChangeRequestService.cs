using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Org;

// Staged effective-dated com_organization changes for the 3 gated kinds
// (create/move/change-boss). No scheduler exists anywhere in this codebase —
// "wait for the effective date" is implemented as lazy apply-on-read:
// ApplyDueChangesAsync is called from the top of every page that touches
// com_organization, and is cheap/idempotent enough to run on every load.
public class OrgChangeRequestService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    private static string WorkflowCodeFor(OrgOrganizationChangeType t) => t switch
    {
        OrgOrganizationChangeType.NewOrganization => "ORG_CHANGE_NEWORG",
        OrgOrganizationChangeType.ChangeParent => "ORG_CHANGE_MOVE",
        OrgOrganizationChangeType.ChangeApprover => "ORG_CHANGE_BOSS",
        _ => throw new ArgumentOutOfRangeException(nameof(t)),
    };

    public async Task<long> SubmitAsync(Org_OrganizationChangeRequest draft, long requesterUserId, string? requesterEmpId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        if (draft.EffectiveFrom < DateOnly.FromDateTime(DateTime.Today))
            throw new InvalidOperationException("วันที่มีผลต้องไม่ใช่วันที่ผ่านมาแล้ว");

        switch (draft.ChangeType)
        {
            case OrgOrganizationChangeType.NewOrganization:
                if (string.IsNullOrWhiteSpace(draft.NewCode) || string.IsNullOrWhiteSpace(draft.NewName))
                    throw new InvalidOperationException("กรุณาระบุรหัสและชื่อสังกัด");

                var codeTaken = await context.com_organizations.AnyAsync(o => o.code == draft.NewCode, ct);
                if (codeTaken)
                    throw new InvalidOperationException("มีรหัสสังกัดนี้อยู่แล้ว");

                var pendingSameCode = await context.Org_OrganizationChangeRequests.AnyAsync(r =>
                    !r.IsApplied && r.ChangeType == OrgOrganizationChangeType.NewOrganization && r.NewCode == draft.NewCode, ct);
                if (pendingSameCode)
                    throw new InvalidOperationException("มีคำขอสร้างหน่วยงานรหัสนี้ค้างอยู่แล้ว รอผลก่อน");
                break;

            case OrgOrganizationChangeType.ChangeParent:
            case OrgOrganizationChangeType.ChangeApprover:
                var target = await context.com_organizations.FirstOrDefaultAsync(o => o.id == draft.TargetOrganizationId, ct);
                if (target is null)
                    throw new InvalidOperationException("ไม่พบหน่วยงานที่ต้องการแก้ไข");
                break;
        }

        draft.RequestedByUserId = requesterUserId;
        draft.RequestedByEmpId = requesterEmpId;
        draft.RequestedDate = DateTime.Now;
        draft.JobMasterId = null;
        draft.IsApplied = false;

        context.Org_OrganizationChangeRequests.Add(draft);
        await context.SaveChangesAsync(ct); // need draft.Id before starting the workflow job

        var workflowCode = WorkflowCodeFor(draft.ChangeType);
        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == workflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{workflowCode}' — ติดต่อแอดมินให้ตั้งค่าก่อน");
        if (workflow.isactive != true)
            throw new InvalidOperationException($"workflow '{workflow.wname}' ปิดใช้งานอยู่ ไม่สามารถส่งคำขอใหม่ได้");

        var subject = BuildSubject(draft);
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Org_OrganizationChangeRequest", draft.Id.ToString(),
            requesterUserId, requesterEmpId, subject, amount: null, ct);

        var toPatch = await context.Org_OrganizationChangeRequests.FirstAsync(r => r.Id == draft.Id, ct);
        toPatch.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);

        return draft.Id;
    }

    private static string BuildSubject(Org_OrganizationChangeRequest draft) => draft.ChangeType switch
    {
        OrgOrganizationChangeType.NewOrganization => $"ขอสร้างหน่วยงานใหม่: {draft.NewCode} — {draft.NewName}",
        OrgOrganizationChangeType.ChangeParent => $"ขอย้ายสังกัด: {draft.TargetOrganizationCode} จาก {draft.OldParentCode ?? "(สูงสุด)"} ไป {draft.NewParentCode ?? "(สูงสุด)"}",
        OrgOrganizationChangeType.ChangeApprover => $"ขอเปลี่ยนหัวหน้า: {draft.TargetOrganizationCode} เป็น {draft.NewApproverName}",
        _ => "คำขอเปลี่ยนแปลงผังองค์กร",
    };

    public async Task<int> ApplyDueChangesAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var due = await context.Org_OrganizationChangeRequests
            .Where(r => !r.IsApplied && r.JobMasterId != null && r.EffectiveFrom <= today)
            .ToListAsync(ct);
        if (due.Count == 0) return 0;

        var jobIds = due.Select(r => r.JobMasterId!.Value).ToList();
        var jobs = await context.job_masters.Where(j => jobIds.Contains(j.jobmasterid)).ToDictionaryAsync(j => j.jobmasterid, ct);

        var applied = 0;
        var newlyCreated = new List<(Org_OrganizationChangeRequest Req, com_organization Org)>();
        foreach (var req in due)
        {
            if (!jobs.TryGetValue(req.JobMasterId!.Value, out var job)) continue;
            if (job.isJobClosed != true || job.status != WorkflowEngineService.StatusCompleted) continue; // still pending / rejected — check again next load

            switch (req.ChangeType)
            {
                case OrgOrganizationChangeType.NewOrganization:
                {
                    var parent = string.IsNullOrEmpty(req.NewParentCode)
                        ? null
                        : await context.com_organizations.FirstOrDefaultAsync(o => o.code == req.NewParentCode, ct);
                    var orgcodefull = await OrgCodeFullHelper.ComputeNextOrgCodeFullAsync(context, parent?.orgcodefull);
                    var newOrg = new com_organization
                    {
                        code = req.NewCode,
                        name = req.NewName,
                        name_en = req.NewNameEn,
                        abbr = req.NewAbbr,
                        abbr_en = req.NewAbbrEn,
                        istop = req.NewIsTop ?? false,
                        isActive = req.NewIsActive ?? true,
                        isManPowerCount = req.NewIsManPowerCount ?? true,
                        SectionTypeCode = req.NewSectionTypeCode,
                        SubSectionTypeId = req.NewSubSectionTypeId,
                        parent_code = req.NewParentCode,
                        CostCenterCode = req.NewCostCenterCode,
                        orgcodefull = orgcodefull,
                        startdate = req.NewStartDate,
                        enddate = req.NewEndDate,
                        remark = req.NewRemark,
                        createdate = DateTime.Now,
                    };
                    context.com_organizations.Add(newOrg);
                    newlyCreated.Add((req, newOrg));
                    break;
                }
                case OrgOrganizationChangeType.ChangeParent:
                {
                    var existing = await context.com_organizations.FirstOrDefaultAsync(o => o.id == req.TargetOrganizationId, ct);
                    if (existing is null) break; // target deleted since request was made — skip, stays unapplied for visibility
                    var parent = string.IsNullOrEmpty(req.NewParentCode)
                        ? null
                        : await context.com_organizations.FirstOrDefaultAsync(o => o.code == req.NewParentCode, ct);
                    existing.parent_code = req.NewParentCode;
                    existing.istop = req.NewIsTop ?? existing.istop;
                    existing.orgcodefull = await OrgCodeFullHelper.ComputeNextOrgCodeFullAsync(context, parent?.orgcodefull);
                    break;
                }
                case OrgOrganizationChangeType.ChangeApprover:
                {
                    var existing = await context.com_organizations.FirstOrDefaultAsync(o => o.id == req.TargetOrganizationId, ct);
                    if (existing is null) break;
                    existing.approver_empid = req.NewApproverEmpId;
                    existing.approver_name = req.NewApproverName;
                    break;
                }
            }

            req.IsApplied = true;
            req.AppliedDate = DateTime.Now;
            applied++;
        }

        if (applied > 0)
        {
            try
            {
                await context.SaveChangesAsync(ct);

                // com_organization.id is only assigned by the identity column after
                // the insert above actually runs — backfill it onto the request now
                // so the history page's actor cross-reference (which matches on
                // TargetOrganizationId) works for NewOrganization requests too, not
                // just ChangeParent/ChangeApprover (which already had it at submit time).
                if (newlyCreated.Count > 0)
                {
                    foreach (var (req, org) in newlyCreated)
                    {
                        req.TargetOrganizationId = org.id;
                        req.TargetOrganizationCode = org.code;
                    }
                    await context.SaveChangesAsync(ct);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another concurrent page load already applied one or more of these
                // rows (RowVersion mismatch) — safe to ignore, the next call
                // re-derives the due list fresh from the DB.
            }
        }
        return applied;
    }
}
