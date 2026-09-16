namespace HRM.Services.Shared;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Extracted from Components/Pages/Org/OrgChart.razor so the same
// d3-org-chart node-building logic can be reused with a selectable root —
// OrgChart.razor still renders the whole-company chart (rootOrgCode: null),
// while OrganizationAdmin.razor's new "ผังองค์กร (ภาพ)" tab can root the
// same chart at any single org unit the user picks, so it starts from the
// correct node for that สังกัด instead of always showing the whole company.
//
// Lazy-load redesign (16 ก.ย. 2569): the original BuildAsync built one
// chart node per Pos_PositionSlot for the WHOLE company up front — fine for
// a small company, but AutoX-scale data (ADVD: 157 org units, 7,000 active
// slots) meant a single BuildAsync call queried 7,000+ slot/employee rows
// and shipped ~1.7MB of JSON to the browser for d3-org-chart to lay out in
// one shot, which is what made a live demo visibly lag (measured: ~1.3s of
// DB time alone, before the client-side render cost). The org tree itself
// is small and shallow (157 units, 3 levels) — it's the PEOPLE, not the
// units, that don't scale. So the chart now loads in two tiers:
//   1. BuildOrgSkeletonAsync — one lightweight node per ORG UNIT (157 rows),
//      no employee data at all, just a headcount badge from one grouped
//      COUNT query. This is what OrgChart.razor's initial load calls.
//   2. BuildEmployeeCardsForOrgAsync — the original per-org card-building
//      logic, now scoped to exactly ONE org unit's own slots, called only
//      when the user expands that unit's node client-side (see
//      OrgChart.razor's GetEmployeeCardsForOrgAsync JSInvokable and
//      org-chart-d3.js's onExpandOrCollapse hook).
public static class OrgChartNodeBuilder
{
    // OrgId/EmployeeId ride along purely so the JS side can report back which
    // org or employee a card click targeted (see org-chart-d3.js's
    // data-org-id/data-employee-id attributes) — click-through navigation to
    // /org/organizations/{OrgId} or /employee/{EmployeeId}.
    // IsOrgNode distinguishes a department-level skeleton card (no person,
    // no slot — just org name + HeadCount badge) from a person/position
    // card, so org-chart-d3.js can render the two differently and knows an
    // org node's children haven't been fetched yet.
    public record ChartNode(string Id, string? ParentId, long OrgId, string OrgName, long? EmployeeId, string? PersonName,
        string? Title, string? PhotoUrl, string? Initials, bool IsVacant, bool IsOrgNode = false, int HeadCount = 0);

    // Tier 1: org-unit skeleton only — no Pos_PositionSlot/Hremployee rows
    // touched at all beyond one grouped COUNT, so this stays fast no matter
    // how many employees a company has (it scales with org-UNIT count,
    // which stays small even for a large company — see class comment).
    public static async Task<List<ChartNode>> BuildOrgSkeletonAsync(HRMContext context, string companyId, string? rootOrgCode, CancellationToken ct = default)
    {
        var allOrgs = await context.com_organizations.ToListAsync(ct);

        var headcountByOrgId = await context.Pos_PositionSlots
            .Where(s => s.CompanyId == companyId && s.IsActive && s.OrganizationId != null && s.HremployeeId != null)
            .GroupBy(s => s.OrganizationId!.Value)
            .Select(g => new { OrgId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrgId, x => x.Count, ct);

        var nodes = new List<ChartNode>();

        ChartNode BuildSkeletonNode(com_organization org, string? parentId) =>
            new(org.code!, parentId, org.id, org.name ?? org.code!, null, null, null, null, null,
                IsVacant: false, IsOrgNode: true, HeadCount: headcountByOrgId.GetValueOrDefault(org.id));

        void AddOrgSkeleton(com_organization org, string? parentId)
        {
            nodes.Add(BuildSkeletonNode(org, parentId));
            var children = allOrgs.Where(o => o.parent_code == org.code).OrderBy(o => o.orgcodefull).ToList();
            foreach (var child in children)
                AddOrgSkeleton(child, org.code);
        }

        if (!string.IsNullOrEmpty(rootOrgCode))
        {
            var singleRoot = allOrgs.FirstOrDefault(o => o.code == rootOrgCode);
            if (singleRoot is not null)
                AddOrgSkeleton(singleRoot, null);
            return nodes;
        }

        var roots = allOrgs.Where(o => o.istop || string.IsNullOrEmpty(o.parent_code)).OrderBy(o => o.orgcodefull).ToList();
        var validRoots = roots.Where(o => !string.IsNullOrEmpty(o.code)).ToList();

        // d3-org-chart (via d3.stratify internally) requires exactly one node
        // with a null parentId. This org structure can have more than one
        // top-level unit (istop=true) at once, so synthesize a single
        // invisible wrapper root when that happens.
        string? syntheticRootId = null;
        if (validRoots.Count > 1)
        {
            syntheticRootId = "__root__";
            nodes.Add(new ChartNode(syntheticRootId, null, 0, "บริษัท", null, null, null, null, null,
                IsVacant: true, IsOrgNode: true, HeadCount: 0));
        }

        foreach (var root in validRoots)
            AddOrgSkeleton(root, syntheticRootId);

        return nodes;
    }

    // Tier 2: the position/employee cards for exactly ONE org unit — called
    // on demand when that unit's skeleton node is expanded client-side.
    // Parented directly under the org skeleton node (org.code), NOT chained
    // representative-card-to-representative-card the way the old whole-tree
    // builder did — each org's cards now sit under their own visible
    // department box instead of masquerading as it.
    public static async Task<List<ChartNode>> BuildEmployeeCardsForOrgAsync(HRMContext context, string companyId, string orgCode, DateOnly? asOfDate = null, CancellationToken ct = default)
    {
        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.code == orgCode, ct);
        if (org is null) return new List<ChartNode>();

        var slots = await context.Pos_PositionSlots
            .Where(s => s.CompanyId == companyId && s.IsActive && s.OrganizationId == org.id)
            .OrderBy(s => s.PosCode)
            .ToListAsync(ct);

        var empIds = slots.Where(s => s.HremployeeId is not null).Select(s => s.HremployeeId!.Value).Distinct().ToList();
        var employeesById = await context.Hremployee
            .Where(e => empIds.Contains(e.id))
            .ToDictionaryAsync(e => e.id, e => e, ct);

        var nodes = new List<ChartNode>();
        if (slots.Count == 0)
        {
            nodes.Add(new ChartNode($"{org.code}_0", org.code, org.id, org.name ?? org.code!, null, null, null, null, null, IsVacant: true));
            return nodes;
        }

        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var id = $"{org.code}_{i}";

            Hremployee? occupant = null;
            if (slot.HremployeeId is long empId && employeesById.TryGetValue(empId, out var emp))
                occupant = emp;
            if (occupant is not null && asOfDate is DateOnly asOf
                && EstablishmentVacancyHelper.IsEffectivelyVacantAsOf(occupant.id, occupant.ResignDate, asOf))
                occupant = null;

            if (occupant is not null)
            {
                var name = $"{occupant.EmpName} {occupant.EmpSurname}".Trim();
                var photoUrl = !string.IsNullOrEmpty(occupant.PhotoStoragePath) ? $"/org/files/employee-photo/{occupant.id}" : null;
                var initials = !string.IsNullOrEmpty(occupant.EmpName) ? occupant.EmpName!.Substring(0, 1).ToUpperInvariant() : "?";
                nodes.Add(new ChartNode(id, org.code, org.id, org.name ?? org.code!, occupant.id, name, slot.Name, photoUrl, initials, IsVacant: false));
            }
            else
            {
                nodes.Add(new ChartNode(id, org.code, org.id, org.name ?? org.code!, null, null, slot.Name, null, null, IsVacant: true));
            }
        }

        return nodes;
    }
}
