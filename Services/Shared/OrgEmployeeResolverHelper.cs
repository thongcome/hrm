namespace HRM.Services.Shared;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Resolves an organization into the list of active employees in its subtree
// (orgcodefull LIKE 'xxx%'). This exact logic was written near-identically in
// PerfAssignmentResolverService.ResolveTargetEmployeesAsync and
// InfoMessageService.ResolveTargetEmployeesAsync — per the standing "extract
// once a second use case proves the pattern" principle, factored out here on
// its third use (Shift Roster). Static, not a DI service — same convention
// as EntitySearchHelper.cs.
public static class OrgEmployeeResolverHelper
{
    public static async Task<List<Hremployee>> ResolveOrganizationSubtreeAsync(
        HRMContext context, string companyId, long organizationId, CancellationToken ct = default)
    {
        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.id == organizationId, ct);
        if (org is null || string.IsNullOrWhiteSpace(org.orgcodefull))
            return new();

        return await context.Hremployee
            .Where(e => e.companyid == companyId && e.ResignDate == null
                && e.orgcodefull != null && e.orgcodefull.StartsWith(org.orgcodefull))
            .ToListAsync(ct);
    }
}
