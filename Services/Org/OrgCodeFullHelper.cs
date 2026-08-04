using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Org;

// Fixed-width hierarchical path, 2 digits per level, no delimiter — same
// scheme as the existing backfilled com_organization.orgcodefull values.
// Computed by taking the next available 2-digit suffix among siblings.
// Shared by OrganizationAdmin.razor's direct-save path and
// OrgChangeRequestService's apply step, so both always compute against
// live sibling data at the moment of the actual write, not a stale value
// snapshotted earlier.
public static class OrgCodeFullHelper
{
    public static async Task<string> ComputeNextOrgCodeFullAsync(HRMContext context, string? parentOrgCodeFull)
    {
        IQueryable<com_organization> siblings = string.IsNullOrEmpty(parentOrgCodeFull)
            ? context.com_organizations.Where(o => o.istop || string.IsNullOrEmpty(o.parent_code))
            : context.com_organizations.Where(o => o.orgcodefull != null && o.orgcodefull.StartsWith(parentOrgCodeFull) && o.orgcodefull.Length == parentOrgCodeFull.Length + 2);

        var siblingCodes = await siblings.Select(o => o.orgcodefull).ToListAsync();
        var maxSuffix = 0;
        foreach (var full in siblingCodes)
        {
            if (string.IsNullOrEmpty(full) || full.Length < 2) continue;
            var suffix = full[^2..];
            if (int.TryParse(suffix, out var n) && n > maxSuffix) maxSuffix = n;
        }
        var nextSuffix = (maxSuffix + 1).ToString("D2");
        return (parentOrgCodeFull ?? "") + nextSuffix;
    }
}
