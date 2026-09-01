using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Job;

// Derives the reporting line ("รายงานต่อ" / subordinate count) for a
// Pos_ExecType, read-only, entirely from existing org structure — never
// keyed in by HR (CEO order 2026-09-01).
//
// How the derivation works (and its limits):
// - Pos_ExecType is a reusable TITLE, not a single seat. The org linkage
//   goes through Pos_PositionSlot (active slots with this PosExecTypeId),
//   each belonging to one com_organization — so the reporting line is
//   per-slot, and one title used in several org units yields several rows.
// - Non-boss slot → reports to its own org's boss: the org's occupied
//   IsBoss slot's title + com_organization.boss_name (kept in sync by
//   EmployeePositionSync.RecomputeBossAsync). Subordinate count = 0.
// - IsBoss slot → reports to the PARENT org's boss (com_organization
//   .parentID chain). Subordinate count = active employees whose
//   Hremployee.OrganizationId is this org (ResignDate == null), excluding
//   the boss occupant themselves — same level-1 semantics as
//   DirectReportResolverHelper (orgs' approver defaults to their boss).
// Limits, deliberate:
// - A title with no active position slot has no derivable reporting line
//   (the UI states this instead of guessing).
// - Temporary approver delegation (toa) is ignored — that is delegated
//   approval AUTHORITY, not the organizational reporting line.
// - A vacant boss position upward shows "(ว่าง)"; we do not skip levels.
// - Subordinates are level-1 headcount by org snapshot, not the recursive
//   subtree (orgcodefull LIKE) — matches what an org chart labels as
//   direct reports.
// Static helper, not a DI service — same convention as
// DirectReportResolverHelper/EntitySearchHelper (also avoids needing a
// Program.cs registration).
public static class JobReportingLineHelper
{
    public sealed record ReportingLineRow(
        long SlotId,
        string? PosCode,
        string? OrgName,
        bool IsBoss,
        string? ReportsToTitle,   // Pos_ExecType.Name of the supervising boss slot (null when none derivable)
        string? ReportsToName,    // com_organization.boss_name of the supervising org (null = vacant/unknown)
        int SubordinateCount);

    public static async Task<List<ReportingLineRow>> ResolveAsync(
        HRMContext context, long posExecTypeId, CancellationToken ct = default)
    {
        var slots = await context.Pos_PositionSlots
            .Where(s => s.PosExecTypeId == posExecTypeId && s.IsActive && s.OrganizationId != null)
            .OrderBy(s => s.PosCode)
            .ToListAsync(ct);
        if (slots.Count == 0) return new();

        var orgIds = slots.Select(s => s.OrganizationId!.Value).Distinct().ToList();
        var orgs = await context.com_organizations
            .Where(o => orgIds.Contains(o.id))
            .ToDictionaryAsync(o => o.id, ct);

        // Parent orgs of boss slots (their boss is who a boss slot reports to).
        var parentIds = orgs.Values
            .Where(o => o.parentID != null)
            .Select(o => o.parentID!.Value)
            .Distinct()
            .Where(id => !orgs.ContainsKey(id))
            .ToList();
        if (parentIds.Count > 0)
        {
            foreach (var p in await context.com_organizations.Where(o => parentIds.Contains(o.id)).ToListAsync(ct))
                orgs[p.id] = p;
        }

        // Occupied boss slots of every involved org, to resolve the
        // supervising TITLE (not just the boss person's name).
        var allOrgIds = orgs.Keys.ToList();
        var bossSlots = await context.Pos_PositionSlots
            .Where(s => s.OrganizationId != null && allOrgIds.Contains(s.OrganizationId.Value) && s.IsBoss && s.IsActive)
            .ToListAsync(ct);
        var bossTitleIds = bossSlots.Where(s => s.PosExecTypeId != null).Select(s => s.PosExecTypeId!.Value).Distinct().ToList();
        var bossTitles = await context.Pos_ExecTypes
            .Where(t => bossTitleIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);
        string? BossTitleOf(long orgId)
        {
            var slot = bossSlots.FirstOrDefault(s => s.OrganizationId == orgId);
            return slot?.PosExecTypeId is long tid ? bossTitles.GetValueOrDefault(tid) : null;
        }

        // Active-headcount per org, one grouped query (for boss-slot rows).
        var headcounts = await context.Hremployee
            .Where(e => e.ResignDate == null && e.OrganizationId != null && allOrgIds.Contains(e.OrganizationId.Value))
            .GroupBy(e => e.OrganizationId!.Value)
            .Select(g => new { OrgId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.OrgId, g => g.Count, ct);

        var result = new List<ReportingLineRow>();
        foreach (var slot in slots)
        {
            var org = orgs.GetValueOrDefault(slot.OrganizationId!.Value);
            if (org is null) continue;

            string? reportsToTitle, reportsToName;
            var subordinates = 0;

            if (slot.IsBoss)
            {
                var parent = org.parentID is long pid ? orgs.GetValueOrDefault(pid) : null;
                reportsToTitle = parent is null ? null : BossTitleOf(parent.id);
                reportsToName = parent?.boss_name;

                var count = headcounts.GetValueOrDefault(org.id);
                // Exclude the boss occupant from their own subordinate count.
                subordinates = slot.HremployeeId != null && count > 0 ? count - 1 : count;
            }
            else
            {
                reportsToTitle = BossTitleOf(org.id);
                reportsToName = org.boss_name;
            }

            result.Add(new ReportingLineRow(slot.Id, slot.PosCode, org.name, slot.IsBoss, reportsToTitle, reportsToName, subordinates));
        }
        return result;
    }
}
