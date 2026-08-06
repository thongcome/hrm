namespace HRM.Services.Shared;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Resolves a manager's level-1 direct reports: orgs whose approver_empid is
// this manager's EmpNo, then employees whose OrganizationId is in that set
// (ResignDate == null, excluding the manager themselves). This exact logic
// was first written in IdpAssessmentService.GetDirectReportsAsync — per the
// standing "extract once a second use case proves the pattern" principle
// (see OrgEmployeeResolverHelper.cs), factored out here on its second use
// (Talent Management). Static, not a DI service — same convention as
// EntitySearchHelper.cs/OrgEmployeeResolverHelper.cs. Deliberately NOT
// merged with PerfAssignmentResolverService.ResolveSubordinateChainAsync,
// which is multi-level with rater-direction weighting — a different shape,
// not a clean fit for this level-1-only helper.
public static class DirectReportResolverHelper
{
    public static async Task<List<Hremployee>> ResolveDirectReportsAsync(
        HRMContext context, Hremployee manager, CancellationToken ct = default)
    {
        var reportOrgIds = await context.com_organizations
            .Where(o => o.approver_empid != null && o.approver_empid == manager.EmpNo)
            .Select(o => o.id)
            .ToListAsync(ct);

        if (reportOrgIds.Count == 0)
            return new();

        return await context.Hremployee
            .Where(e => e.ResignDate == null && e.OrganizationId != null && reportOrgIds.Contains(e.OrganizationId!.Value) && e.id != manager.id)
            .ToListAsync(ct);
    }
}
