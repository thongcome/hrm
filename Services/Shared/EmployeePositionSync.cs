using HRM.Models;
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
    public static async Task SyncAsync(
        HRMContext context,
        Pos_PositionSlot slot,
        long? oldHremployeeId,
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
    }
}
