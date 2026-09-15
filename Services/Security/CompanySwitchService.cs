namespace HRM.Services.Security;

using System.Security.Claims;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Lets a signed-in user view/act on a different company's data than the one
// they logged in as, without re-authenticating — for a holding-company style
// setup (see com_organization.isCompany rows: ADHOLD / ADDIGITAL / ADMOVIE)
// where one HR person legitimately works across a parent company and its
// subsidiaries. Endpoints/CompanySwitchEndpoints.cs is the only writer of the
// "payroll_company" claim this produces; every one of the ~155 existing
// call sites that reads that claim keeps working unchanged, since only the
// claim's VALUE changes, never its shape.
//
// Deliberately reuses the existing sc_role_scope / RoleScopeSnapshot
// machinery (Services/Security/RoleScopeSnapshot.cs) as the permission gate
// instead of inventing a new grant table: a role with zero Company-type
// scope rows is unrestricted (may switch to any company) — the same safe
// default already documented there — a role with Company-type rows is
// limited to exactly those company codes. An admin locks this down today at
// /admin/system/role-scopes; no new admin screen needed for this feature.
public class CompanySwitchService
{
    public record SwitchableCompany(string Code, string Name);

    public async Task<List<SwitchableCompany>> GetSwitchableCompaniesAsync(ClaimsPrincipal user, HRMContext context, CancellationToken ct = default)
    {
        var scope = RoleScopeSnapshot.FromClaims(user);

        // isCompany marks a com_organization node as its own legal company
        // boundary (distinct from istop, which just marks the top of
        // whichever tree fragment a node sits in — a tree can have more than
        // one isCompany node, e.g. ADHOLD's own subsidiaries ADDIGITAL/
        // ADMOVIE nested under it). comp_code on those rows is the exact
        // string every Hremployee/payroll_company claim uses as CompanyId
        // (verified against live data — see the plan this was built from).
        var companyNodes = await context.com_organizations
            .Where(o => o.isCompany && o.comp_code != null)
            .Select(o => new { o.comp_code, o.name })
            .ToListAsync(ct);

        // Dedupe by comp_code — known pre-existing data-quality debris
        // (two stale root nodes both stamped comp_code="AD") collapses into
        // one entry here with no separate cleanup needed.
        var byCode = companyNodes
            .Where(o => !string.IsNullOrWhiteSpace(o.comp_code))
            .GroupBy(o => o.comp_code!)
            .Select(g => new SwitchableCompany(g.Key, g.First().name ?? g.Key))
            .ToList();

        var currentCompanyId = user.FindFirst("payroll_company")?.Value;

        var allowed = scope.IsUnrestricted
            ? byCode
            : byCode.Where(c => scope.CompanyIds.Contains(c.Code)).ToList();

        // A user always keeps their own current company in the list even if
        // it somehow fell outside their granted scope — this is a switcher,
        // not an additional restriction on top of what they already see.
        if (currentCompanyId is not null && allowed.All(c => c.Code != currentCompanyId))
        {
            var own = byCode.FirstOrDefault(c => c.Code == currentCompanyId)
                ?? new SwitchableCompany(currentCompanyId, currentCompanyId);
            allowed = allowed.Prepend(own).ToList();
        }

        return allowed.OrderBy(c => c.Code).ToList();
    }

    public async Task<bool> IsSwitchAllowedAsync(ClaimsPrincipal user, string targetCompCode, HRMContext context, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetCompCode)) return false;
        var allowed = await GetSwitchableCompaniesAsync(user, context, ct);
        return allowed.Any(c => c.Code == targetCompCode);
    }
}
