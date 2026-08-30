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
//
// "Active" here means EmployeeStatusHelper.CanTransact (IsActive AND not yet
// past ResignDate) rather than a raw `ResignDate == null` check — every
// consumer of this helper is an operational/day-to-day context (who's
// scheduled for a shift, who should receive an announcement, who's on the
// team calendar), not a forward-looking talent program, so someone who has
// merely given advance notice must still show up here (they're still
// working). A raw null-check silently dropped notice-period employees
// entirely, discovered via Leave's team calendar missing a real completed
// leave request. See EmployeeStatusHelper.cs's own comment for why the two
// checks aren't interchangeable everywhere.
public static class OrgEmployeeResolverHelper
{
    public static async Task<List<Hremployee>> ResolveOrganizationSubtreeAsync(
        HRMContext context, string companyId, long organizationId, CancellationToken ct = default)
    {
        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.id == organizationId, ct);
        if (org is null || string.IsNullOrWhiteSpace(org.orgcodefull))
            return new();

        var candidates = await context.Hremployee
            .Where(e => e.companyid == companyId && e.orgcodefull != null && e.orgcodefull.StartsWith(org.orgcodefull))
            .ToListAsync(ct);
        return candidates.Where(EmployeeStatusHelper.CanTransact).ToList();
    }
}
