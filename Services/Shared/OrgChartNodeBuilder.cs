namespace HRM.Services.Shared;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Extracted from Components/Pages/Org/OrgChart.razor so the same
// d3-org-chart node-building logic can be reused with a selectable root —
// OrgChart.razor still renders the whole-company chart (rootOrgCode: null),
// while OrganizationAdmin.razor's new "ผังองค์กร (ภาพ)" tab can root the
// same chart at any single org unit the user picks, so it starts from the
// correct node for that สังกัด instead of always showing the whole company.
public static class OrgChartNodeBuilder
{
    // OrgId/EmployeeId ride along purely so the JS side can report back which
    // org or employee a card click targeted (see org-chart-d3.js's
    // data-org-id/data-employee-id attributes) — click-through navigation to
    // /org/organizations/{OrgId} or /employee/{EmployeeId}.
    public record ChartNode(string Id, string? ParentId, long OrgId, string OrgName, long? EmployeeId, string? PersonName,
        string? Title, string? PhotoUrl, string? Initials, bool IsVacant);

    // rootOrgCode: null = every top-level unit (istop/no parent_code), same
    // as the original whole-company behavior — synthesizes an invisible
    // wrapper root if there's more than one. A specific code = only that
    // org and its descendants, rooted at that org's own representative card
    // (ParentId = null for it), so the chart never accidentally pulls in
    // siblings or the rest of the company.
    public static async Task<List<ChartNode>> BuildAsync(HRMContext context, string companyId, string? rootOrgCode, CancellationToken ct = default)
    {
        var nodes = new List<ChartNode>();

        var allOrgs = await context.com_organizations.ToListAsync(ct);

        var slots = await context.Pos_PositionSlots
            .Where(s => s.CompanyId == companyId && s.IsActive && s.OrganizationId != null)
            .ToListAsync(ct);
        var orgIdToCode = allOrgs.ToDictionary(o => o.id, o => o.code);
        var slotsByOrgCode = slots
            .Where(s => orgIdToCode.ContainsKey(s.OrganizationId!.Value) && orgIdToCode[s.OrganizationId!.Value] != null)
            .ToLookup(s => orgIdToCode[s.OrganizationId!.Value]!);

        var empIds = slots.Where(s => s.HremployeeId is not null).Select(s => s.HremployeeId!.Value).Distinct().ToList();
        var employeesById = await context.Hremployee
            .Where(e => empIds.Contains(e.id))
            .ToDictionaryAsync(e => e.id, e => e, ct);

        // Every org unit contributes one node per position slot (a vacant
        // placeholder node if it has none). Only the org's first slot
        // (index 0 — the "representative" card) is wired into the
        // org-hierarchy chain via ParentId; any additional slots in the
        // same org hang directly off that representative card.
        void AddOrgNodes(com_organization org, string? parentId)
        {
            var orgSlots = slotsByOrgCode[org.code!].OrderBy(s => s.PosCode).ToList();
            string? representativeId = null;

            if (orgSlots.Count == 0)
            {
                var id = $"{org.code}_0";
                representativeId = id;
                nodes.Add(new ChartNode(id, parentId, org.id, org.name ?? org.code!, null, null, null, null, null, IsVacant: true));
            }
            else
            {
                for (var i = 0; i < orgSlots.Count; i++)
                {
                    var slot = orgSlots[i];
                    var id = $"{org.code}_{i}";
                    if (i == 0) representativeId = id;

                    if (slot.HremployeeId is long empId && employeesById.TryGetValue(empId, out var emp))
                    {
                        var name = $"{emp.EmpName} {emp.EmpSurname}".Trim();
                        var photoUrl = !string.IsNullOrEmpty(emp.PhotoStoragePath) ? $"/org/files/employee-photo/{emp.id}" : null;
                        var initials = !string.IsNullOrEmpty(emp.EmpName) ? emp.EmpName!.Substring(0, 1).ToUpperInvariant() : "?";
                        nodes.Add(new ChartNode(id, i == 0 ? parentId : representativeId, org.id, org.name ?? org.code!,
                            emp.id, name, slot.Name, photoUrl, initials, IsVacant: false));
                    }
                    else
                    {
                        nodes.Add(new ChartNode(id, i == 0 ? parentId : representativeId, org.id, org.name ?? org.code!,
                            null, null, slot.Name, null, null, IsVacant: true));
                    }
                }
            }

            var children = allOrgs.Where(o => o.parent_code == org.code).OrderBy(o => o.orgcodefull).ToList();
            foreach (var child in children)
            {
                AddOrgNodes(child, representativeId);
            }
        }

        if (!string.IsNullOrEmpty(rootOrgCode))
        {
            var singleRoot = allOrgs.FirstOrDefault(o => o.code == rootOrgCode);
            if (singleRoot is not null)
                AddOrgNodes(singleRoot, null);
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
            // OrgId=0 is a sentinel for "not a real org row" — org-chart-d3.js
            // only wires up the header's click-through when orgId > 0, so
            // clicking this synthetic wrapper card does nothing (there's
            // nowhere real to navigate to).
            nodes.Add(new ChartNode(syntheticRootId, null, 0, "บริษัท", null, null, null, null, null, IsVacant: true));
        }

        foreach (var root in validRoots)
        {
            AddOrgNodes(root, syntheticRootId);
        }

        return nodes;
    }
}
