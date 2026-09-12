using Advance.Workflow.Contracts;
using Advance.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Advance.Workflow.Engine;

// ============================================================================
//  WorkflowApproverResolver — ported from Model/wf_sub_workflow_master.Behavior.cs
//  (HRM's wf_sub_workflow_master.GetUserAsync/GetUserBySourceAsync and their
//  eight per-field strategy methods). That file put this logic directly on
//  the entity as a partial class, per the CEO's "class มี class เดียว" design
//  note; here it moves to a plain resolver class instead, because the seams
//  (IOrgDirectorySource / IWorkflowUserDirectory) are services that need DI,
//  and an EF entity copied into Advance.Workflow.Domain has no service
//  dependencies by design (see that project's own comment on why it stays
//  pure POCOs).
//
//  Ported strategies (same eight fields, same union-not-first-match behavior
//  as the original GetUserBySourceAsync):
//    iscustomUser, userid1/2/3, isAdhocUser, iscustomRole  — read only
//    Advance.Workflow.Domain tables (wf_custom_user, wf_adhoc_user, ...); no
//    seam needed except turning a role id into user ids
//    (IWorkflowUserDirectory.GetUserIdsInRolesAsync).
//    isLOA                                                 — wf_loa/wf_loa_user,
//    Domain-only, no seam needed.
//    SupervisorChain (isupperrole/isupperuser/isNeedsupervisorapprove) and
//    isApproverSameCostCenter/isApproverSameOrg              — these are the
//    ones that read com_organization/Hremployee/sc_user directly in the
//    original. See the TODO(seam) comments below.
//
//  NOT ported (flagged in ../../EXTRACTION-PLAN.md as "needs the real
//  author's judgment before cutover" — the amount of context to safely
//  re-derive these from scratch exceeds what Phase 0 should attempt):
//    - The Mix Approval vertical pre-check hop-walker and the self-terminating
//      vertical-chain / emp-level-climb mechanisms described in
//      WorkflowEngineService.cs's file header (VerticalPrecheckMarker,
//      VerticalChainMarker, EmpLevelClimbMarker). Those live in
//      WorkflowEngineService.AssignVerticalChainHopAsync /
//      AssignEmpLevelClimbAsync in HRM today (not shown in this port).
// ============================================================================

public sealed record ApproverSource(string Field, string ResolverKind, List<long> UserIds, string? Note = null);

public sealed class WorkflowApproverResolver
{
    private readonly WorkflowDbContext _db;
    private readonly IOrgDirectorySource _org;
    private readonly IWorkflowUserDirectory _users;

    public WorkflowApproverResolver(WorkflowDbContext db, IOrgDirectorySource org, IWorkflowUserDirectory users)
    {
        _db = db;
        _org = org;
        _users = users;
    }

    /// <summary>The same WorkflowDbContext this resolver was built against — exposed so
    /// callers that already hold a resolver (e.g. WorkflowService.GetUserRelateAsync)
    /// don't need a second db parameter just to reach ApplyDelegationAsync.</summary>
    public WorkflowDbContext Db => _db;

    public async Task<List<long>> GetApproverUserIdsAsync(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
    {
        var sources = await GetUserBySourceAsync(sub, job, ct);
        return sources.SelectMany(s => s.UserIds).Distinct().ToList();
    }

    public async Task<List<ApproverSource>> GetUserBySourceAsync(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
    {
        var found = new List<ApproverSource>();
        foreach (var field in new Func<wf_sub_workflow_master, job_master, CancellationToken, Task<ApproverSource?>>[]
        {
            IsCustomUserAsync,
            UserId123Async,
            IsAdhocUserAsync,
            IsCustomRoleAsync,
            IsLOAAsync,
            SupervisorChainAsync,
            IsApproverSameCostCenterAsync,
            IsApproverSameOrgAsync,
        })
        {
            var one = await field(sub, job, ct);
            if (one is not null) found.Add(one);
        }
        return found;
    }

    private async Task<ApproverSource?> IsCustomUserAsync(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
    {
        if (!sub.iscustomUser) return null;
        var q = _db.wf_custom_users.Where(c => c.workflowid == sub.workflowid && c.wlevel == sub.wlevel && c.isactive);
        if (job.loaid is long loa) q = q.Where(c => c.loaid == loa);
        var ids = await q.Select(c => c.userid).ToListAsync(ct);
        return new("iscustomUser", "USER", ids, ids.Count == 0 ? "ติ๊กไว้แต่ยังไม่ได้เลือกใคร" : null);
    }

    private Task<ApproverSource?> UserId123Async(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
    {
        var ids = new[] { sub.userid1, sub.userid2, sub.userid3 }.Where(u => u is not null).Select(u => u!.Value).ToList();
        return Task.FromResult<ApproverSource?>(ids.Count == 0 ? null : new ApproverSource("userid1/2/3", "USER", ids));
    }

    private async Task<ApproverSource?> IsAdhocUserAsync(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
    {
        if (!sub.isAdhocUser || job.jobmasterid <= 0) return null;
        var ids = await _db.wf_adhoc_users
            .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == sub.wlevel && a.isactive == true)
            .OrderBy(a => a.orderTh)
            .Select(a => a.userid).ToListAsync(ct);
        return new("isAdhocUser", "USER", ids, ids.Count == 0 ? "ยังไม่มีใครถูกเรียกเข้ามา" : null);
    }

    private async Task<ApproverSource?> IsCustomRoleAsync(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
    {
        if (!sub.iscustomRole) return null;
        var q = _db.wf_custom_roles.Where(c => c.workflowid == sub.workflowid && c.wlevel == sub.wlevel && c.isactive != false);
        if (job.loaid is long loa) q = q.Where(c => c.loaid == loa);
        var roleIds = await q.Select(c => c.roleid).ToListAsync(ct);
        // TODO(seam): original reads db.sc_user_roles (HRM identity) directly.
        var users = roleIds.Count == 0 ? new List<long>() : await _users.GetUserIdsInRolesAsync(roleIds, ct);
        return new("iscustomRole", "ROLE", users.ToList(),
            roleIds.Count == 0 ? "ติ๊กไว้แต่ยังไม่ได้เลือก role" : $"{roleIds.Count} role");
    }

    private async Task<ApproverSource?> IsLOAAsync(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
    {
        if (!sub.isLOA) return null;
        var (users, note) = await ResolveLoaAsync(sub, job, ct);
        return new("isLOA", "LOA", users, note);
    }

    private async Task<(List<long> Users, string? Note)> ResolveLoaAsync(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
    {
        var amount = job.reqamont;
        if (amount is null) return (new(), "งานนี้ไม่ได้ระบุจำนวนเงิน");

        var bands = await _db.wf_loas
            .Where(l => l.wfid == sub.workflowid && l.nowWorkflowid == sub.workflowid
                     && l.nowLevel == sub.wlevel && l.isactive != false)
            .ToListAsync(ct);

        var band = bands
            .Where(l => amount >= (l.min ?? decimal.MinValue) && amount <= (l.max ?? decimal.MaxValue))
            .Where(l => string.IsNullOrEmpty(l.orgcode) || l.orgcode == job.reqOrg)
            .OrderByDescending(l => !string.IsNullOrEmpty(l.orgcode))
            .FirstOrDefault();
        if (band is null) return (new(), $"ไม่พบแถบวงเงินที่ครอบ {amount:N2}");

        job.loaid = band.loaid;

        var users = await _db.wf_loa_users
            .Where(u => u.loaid == band.id && u.isactive)
            .Select(u => u.userid).ToListAsync(ct);
        return (users, $"วงเงิน {band.min:N0}–{band.max:N0}");
    }

    // หัวหน้าตามผังองค์กร — SupervisorLevels ผ่านหัวหน้ากี่ชั้น
    private Task<ApproverSource?> SupervisorChainAsync(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
        => SupervisorAtHopAsync(sub, job, 1, ct);

    public async Task<ApproverSource?> SupervisorAtHopAsync(wf_sub_workflow_master sub, job_master job, int hop, CancellationToken ct)
    {
        var levels = SupervisorLevels(sub);
        if (levels <= 0) return null;

        var field = sub.isNeedsupervisorapprove > 0 ? "isNeedsupervisorapprove"
                  : sub.isupperrole ? "isupperrole" : "isupperuser";
        var (boss, note) = await ResolveOrgChainAsync(job, hop, ct);
        return new(field, "ORG", boss is long b ? new List<long> { b } : new List<long>(),
            levels > 1 ? $"หัวหน้าชั้นที่ {hop}/{levels}" + (note is null ? "" : $" · {note}") : note);
    }

    public static int SupervisorLevels(wf_sub_workflow_master sub) =>
        (sub.isNeedsupervisorapprove ?? 0) > 0 ? sub.isNeedsupervisorapprove!.Value
        : (sub.isupperrole || sub.isupperuser) ? 1 : 0;

    // TODO(seam): the original (Model/wf_sub_workflow_master.Behavior.cs,
    // ResolveOrgChainAsync) climbs com_organization.parent_code directly and
    // resolves approver_empid -> sc_user.userid at each hop, skipping the
    // requester's own row. Ported here against IOrgDirectorySource /
    // IWorkflowUserDirectory instead — but NOTE this is a live per-hop call
    // through the interface, not a read of Advance.Workflow.Org's own
    // wf_org_type table. That table (see Advance.Workflow.Org/README.md) has
    // NO approver column today (orgcode/orgcodefull/upperorg/istoplevel only)
    // — so a standalone deployment's IOrgDirectorySource implementation has
    // nowhere to persist "who approves for this org unit" yet. This is
    // flagged in EXTRACTION-PLAN.md as a schema decision the real workflow
    // author needs to make before Phase 1 cutover (add an ApproverEmpId
    // column to wf_org_type, or a separate assignment table).
    private async Task<(long? UserId, string? Note)> ResolveOrgChainAsync(job_master job, int climb, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(job.reqOrg)) return (null, "ผู้ขอไม่มีหน่วยงาน (reqOrg ว่าง)");

        var org = await _org.GetOrganizationAsync(job.reqOrg, ct);
        var levelsFound = 0;
        for (var hop = 0; org is not null && hop < 20; hop++)
        {
            if (!string.IsNullOrWhiteSpace(org.ApproverEmpId))
            {
                var approver = await _users.GetUserByEmpIdAsync(org.ApproverEmpId, ct);
                if (approver is { IsDisabled: false } u && u.UserId != job.createuserid)
                {
                    levelsFound++;
                    if (levelsFound >= climb)
                        return (u.UserId, $"{org.Code}" + (climb > 1 ? $" (หัวหน้าชั้นที่ {climb})" : ""));
                }
            }
            if (string.IsNullOrWhiteSpace(org.ParentCode))
            {
                return (null, levelsFound == 0
                    ? "ไต่จนสุดผังแล้วไม่พบผู้อนุมัติ"
                    : $"ผังมีหัวหน้าแค่ {levelsFound} ชั้น แต่ตั้งไว้ {climb} ชั้น");
            }
            org = await _org.GetOrganizationAsync(org.ParentCode, ct);
        }
        return (null, "ไต่จนสุดผังแล้วไม่พบผู้อนุมัติ");
    }

    private async Task<ApproverSource?> IsApproverSameCostCenterAsync(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
    {
        if (!sub.isApproverSameCostCenter) return null;
        if (string.IsNullOrWhiteSpace(job.costcenter))
            return new("isApproverSameCostCenter", "ORG", new(), "งานนี้ไม่มี cost center");

        // TODO(seam): original queries com_organizations by CostCenterCode directly;
        // IOrgDirectorySource has no "by cost center" lookup yet — add one if this
        // strategy is needed for real before cutover (see EXTRACTION-PLAN.md).
        return new("isApproverSameCostCenter", "ORG", new(), "TODO(seam): cost-center org lookup not ported — see EXTRACTION-PLAN.md");
    }

    private async Task<ApproverSource?> IsApproverSameOrgAsync(wf_sub_workflow_master sub, job_master job, CancellationToken ct)
    {
        if (!sub.isApproverSameOrg) return null;
        if (string.IsNullOrWhiteSpace(job.reqOrg))
            return new("isApproverSameOrg", "ORG", new(), "ผู้ขอไม่มีหน่วยงาน");

        // TODO(seam): original queries db.sc_users by orgcode directly.
        var users = await _users.GetUsersInOrgAsync(job.reqOrg, ct);
        var ids = users.Where(u => !u.IsDisabled && u.UserId != job.createuserid).Select(u => u.UserId).ToList();
        return new("isApproverSameOrg", "ORG", ids, $"หน่วยงาน {job.reqOrg}");
    }
}
