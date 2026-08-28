using HRM.Models;
using HRM.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Shared;

// Single source of truth for "which org does this employee belong to" is
// Pos_PositionSlot.HremployeeId (see Model/Pos_PositionSlot.cs doc comment).
// Hremployee.OrganizationId/orgcode/orgcodefull is a denormalized snapshot,
// kept in sync ONLY through this helper — never independently editable by HR
// (see PayrollEmployeeAdmin.razor's read-only org display). Call this from
// PositionSlotAdmin.razor.SaveAsync after all field mutations on `slot` but
// BEFORE the single context.SaveChangesAsync() call, so slot writes +
// vacated-other-slot writes + Hremployee snapshot writes land in one atomic
// save. Does not call SaveChangesAsync itself.
public static class EmployeePositionSync
{
    // oldHremployeeId must be captured by the caller from the tracked entity
    // BEFORE overwriting slot.HremployeeId with the new value — for a
    // brand-new slot (create branch), pass null (no prior occupant).
    // actorScope: pass the acting user's RoleScopeSnapshot (Advance Security
    // slice 1) to enforce "you can only assign an employee into an org
    // within your own data scope." Read-side scoping (the EF query filter
    // on Hremployee) can't catch this on its own — a brand-new assignment
    // has no existing row to filter, so this is the one explicit
    // write-time check for that slice. Omit (default null) to skip the
    // check entirely — every existing caller that hasn't been updated to
    // pass it keeps behaving exactly as before.
    // oldOrganizationId: pass the tracked entity's OrganizationId captured
    // BEFORE overwriting it (same discipline as oldHremployeeId) — only
    // needed so an IsBoss slot that just moved orgs also triggers a boss
    // recompute on the org it LEFT, not just the one it landed in. Omit
    // (default null) for callers that never move a slot between orgs (e.g.
    // RecOfferService assigning a new hire into an already-vacant slot).
    public static async Task SyncAsync(
        HRMContext context,
        Pos_PositionSlot slot,
        long? oldHremployeeId,
        RoleScopeSnapshot? actorScope = null,
        long? oldOrganizationId = null,
        CancellationToken ct = default)
    {
        var newHremployeeId = slot.HremployeeId;

        // 1) Whenever this slot now holds an employee, make sure no OTHER
        // active slot also claims them — enforces "one employee, at most one
        // active slot" as a side effect of assignment. Runs unconditionally
        // (even if HremployeeId on THIS slot didn't change) — cheap,
        // idempotent, self-healing if the DB ever got into a bad state.
        if (newHremployeeId is long newId)
        {
            var others = await context.Pos_PositionSlots
                .Where(s => s.HremployeeId == newId && s.IsActive && s.Id != slot.Id && s.CompanyId == slot.CompanyId)
                .ToListAsync(ct);
            foreach (var other in others)
                other.HremployeeId = null;
        }

        // 2) If the occupant just changed FROM someone, clear their snapshot
        // — unless they still hold another active slot elsewhere (shouldn't
        // normally happen given step 1, but don't blank a real assignment).
        if (oldHremployeeId != newHremployeeId && oldHremployeeId is long oldId)
        {
            var stillActive = await context.Pos_PositionSlots
                .AnyAsync(s => s.HremployeeId == oldId && s.IsActive && s.Id != slot.Id, ct);
            if (!stillActive)
            {
                var oldEmp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == oldId, ct);
                if (oldEmp is not null)
                {
                    oldEmp.OrganizationId = null;
                    oldEmp.orgcode = null;
                    oldEmp.orgcodefull = null;
                }
            }
        }

        // 3) (Re)write the CURRENT occupant's snapshot from THIS slot's
        // current org/active state — unconditionally, not just when
        // HremployeeId changed, so moving an occupied slot to a different
        // org or deactivating it also keeps the snapshot correct (otherwise
        // EmployeeDetail.razor/PayrollEmployeeAdmin.razor would show stale
        // data). EF's change tracker diffs by value, so re-assigning the
        // same value is a no-op — no spurious UPDATE/audit row when nothing
        // actually changed.
        if (newHremployeeId is long curId)
        {
            var curEmp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == curId, ct);
            if (curEmp is not null)
            {
                if (slot.IsActive && slot.OrganizationId is long orgId)
                {
                    var org = await context.com_organizations.FirstOrDefaultAsync(o => o.id == orgId, ct);

                    if (actorScope is not null && !actorScope.AllowsEmployee(slot.CompanyId, org?.orgcodefull, org?.CostCenterCode))
                        throw new InvalidOperationException($"คุณไม่มีสิทธิ์มอบหมายพนักงานเข้าหน่วยงาน \"{org?.name ?? slot.CompanyId}\" เพราะอยู่นอกขอบเขตข้อมูล (data scope) ของคุณ");

                    curEmp.OrganizationId = org?.id;
                    curEmp.orgcode = org?.code;
                    curEmp.orgcodefull = org?.orgcodefull;
                }
                else
                {
                    // Slot deactivated or has no org — EmployeeDetail.razor's
                    // query (IsActive filter) would no longer surface this
                    // slot for this employee, so their snapshot shouldn't
                    // claim it either.
                    curEmp.OrganizationId = null;
                    curEmp.orgcode = null;
                    curEmp.orgcodefull = null;
                }
            }
        }

        // 4) IsBoss position slots also drive com_organization.boss_name/
        // boss_emp_id (see Services/Org/OrgBossApproverService.cs's own
        // direct-override path for the other way this field gets written).
        // Recomputed from scratch off current Pos_PositionSlot state —
        // self-healing, same philosophy as step 1 — rather than trying to
        // track exactly what changed, which sidesteps needing to know
        // whether IsBoss itself was toggled on this save.
        if (slot.OrganizationId is long curOrgId)
            await RecomputeBossAsync(context, curOrgId, ct);
        if (oldOrganizationId is long prevOrgId && prevOrgId != slot.OrganizationId)
            await RecomputeBossAsync(context, prevOrgId, ct);
    }

    private static async Task RecomputeBossAsync(HRMContext context, long organizationId, CancellationToken ct)
    {
        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.id == organizationId, ct);
        if (org is null) return;

        var bossSlot = await context.Pos_PositionSlots
            .Where(s => s.OrganizationId == organizationId && s.IsBoss && s.IsActive && s.HremployeeId != null)
            .FirstOrDefaultAsync(ct);

        if (bossSlot?.HremployeeId is long bossEmpId)
        {
            var bossEmp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == bossEmpId, ct);
            if (bossEmp is not null)
            {
                var bossChanged = org.boss_emp_id != bossEmp.EmpNo;
                org.boss_emp_id = bossEmp.EmpNo;
                org.boss_name = $"{bossEmp.EmpName} {bossEmp.EmpSurname}".Trim();

                // Approver defaults to boss unless a temporary delegation is
                // active — keep them in sync the same way
                // OrgBossApproverService.SetBossDirectAsync does for the
                // manual-override path.
                if (bossChanged)
                {
                    var hasActiveDelegation = await context.toas
                        .AnyAsync(d => d.OrganizationId == organizationId && d.isactive, ct);
                    if (!hasActiveDelegation)
                    {
                        org.approver_empid = org.boss_emp_id;
                        org.approver_name = org.boss_name;
                    }
                }
            }
        }
        else
        {
            // No occupied IsBoss slot for this org right now — boss shows
            // as vacant. Deliberately don't touch approver_name here even
            // if it matched the old boss: a boss position going vacant
            // shouldn't silently blank out an approver a human explicitly
            // relies on for live workflow approvals.
            org.boss_emp_id = null;
            org.boss_name = null;
        }
    }
}
