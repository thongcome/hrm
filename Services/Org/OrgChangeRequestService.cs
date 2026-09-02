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
                    // comp_code: inherit the parent's company tree; a new ROOT
                    // node belongs to the currently active company (CEO rule:
                    // one active company; comp_code == com_company.code).
                    var compCode = parent?.comp_code
                        ?? (await ActiveCompanyHelper.GetActiveAsync(context))?.code;
                    var newOrg = new com_organization
                    {
                        comp_code = compCode,
                        code = req.NewCode,
                        name = req.NewName,
                        name_en = req.NewNameEn,
                        abbr = req.NewAbbr,
                        abbr_en = req.NewAbbrEn,
                        istop = req.NewIsTop ?? false,
                        isActive = req.NewIsActive ?? true,
                        isManPowerCount = req.NewIsManPowerCount ?? true,
                        isCompany = req.NewIsCompany ?? false,
                        isBranch = req.NewIsBranch ?? false,
                        SectionTypeCode = req.NewSectionTypeCode,
                        SubSectionTypeId = req.NewSubSectionTypeId,
                        layer_code = req.NewLayerCode,
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

                    var oldFull = existing.orgcodefull;
                    existing.parent_code = req.NewParentCode;
                    existing.istop = req.NewIsTop ?? existing.istop;
                    existing.orgcodefull = await OrgCodeFullHelper.ComputeNextOrgCodeFullAsync(context, parent?.orgcodefull);

                    // Advance Security slice 1 requirement: a move must
                    // cascade-rebuild every descendant's orgcodefull too, not
                    // just the moved node's own — otherwise the sc_role_scope
                    // ORG/BRANCH prefix check (and anything else reading
                    // orgcodefull) goes stale for the whole moved subtree the
                    // instant this apply runs. Previously a known, documented
                    // gap (OrganizationAdmin.razor never touched descendants);
                    // closing it here is in scope for this slice specifically
                    // because the scope feature depends on orgcodefull being
                    // correct.
                    if (!string.IsNullOrEmpty(oldFull) && oldFull != existing.orgcodefull)
                        await RebuildDescendantOrgCodesAsync(context, existing.id, oldFull, existing.orgcodefull!, ct);

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

    // Rewrites orgcodefull for every descendant of a node that just moved,
    // by replacing the moved node's own old prefix with its new one — a
    // single string-replace pass works because orgcodefull is a fixed
    // 2-digit-per-level concatenation, so only the ancestor portion up to
    // and including the moved node itself changes; each descendant's own
    // suffix (the part identifying its position under the moved node) is
    // unaffected. Also refreshes Hremployee.orgcodefull for every employee
    // snapshotted against the moved node or any of its descendants — that
    // snapshot (EmployeePositionSync) is what sc_role_scope's ORG/BRANCH
    // check actually reads, so it must stay in sync with the move.
    private static async Task RebuildDescendantOrgCodesAsync(
        HRMContext context, long movedOrgId, string oldFull, string newFull, CancellationToken ct)
    {
        var descendants = await context.com_organizations
            .Where(o => o.id != movedOrgId && o.orgcodefull != null && o.orgcodefull.StartsWith(oldFull))
            .ToListAsync(ct);

        var affectedOrgIds = new List<long> { movedOrgId };
        foreach (var descendant in descendants)
        {
            descendant.orgcodefull = newFull + descendant.orgcodefull![oldFull.Length..];
            affectedOrgIds.Add(descendant.id);
        }

        var affectedEmployees = await context.Hremployee
            .Where(e => e.OrganizationId != null && affectedOrgIds.Contains(e.OrganizationId!.Value))
            .ToListAsync(ct);
        var orgFullById = descendants.ToDictionary(o => o.id, o => o.orgcodefull!);
        orgFullById[movedOrgId] = newFull;
        foreach (var emp in affectedEmployees)
        {
            if (emp.OrganizationId is long orgId && orgFullById.TryGetValue(orgId, out var full))
                emp.orgcodefull = full;
        }
    }
}
