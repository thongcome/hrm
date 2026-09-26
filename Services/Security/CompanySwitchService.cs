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
// Who may switch where — FAIL-CLOSED (CEO, 26 ก.ย. 2569). Screen testing found a UITEST payroll
// approver could switch into ADVD and read its payroll, because a role with no sc_role_scope rows
// was treated as "may switch to any company". Now:
//   · the Admin role may switch to every company;
//   · everyone else sees their home company (the "home_company" claim stamped at sign-in) plus only
//     the companies a Company-type scope row explicitly grants (/admin/system/role-scopes).
// This is about the switcher only — RoleScopeSnapshot's own default for data filters is untouched.
// The home company is always switchable, so nobody can strand themselves in another company.
public class CompanySwitchService
{
    public record SwitchableCompany(string Code, string Name);

    public const string HomeCompanyClaim = "home_company";
    private const string AdminRoleName = "Admin";

    /// <summary>
    /// The pure rule: Admin → all companies; otherwise home + explicitly granted companies (+ the current one,
    /// so the list always shows where the user is). Names come from <paramref name="known"/>, else the code.
    /// </summary>
    public static List<SwitchableCompany> Allowed(IReadOnlyCollection<SwitchableCompany> known, bool isAdmin,
        IReadOnlyCollection<string> grantedCompanyCodes, string? homeCompany, string? currentCompany)
    {
        var codes = isAdmin
            ? known.Select(c => c.Code).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : grantedCompanyCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(homeCompany)) codes.Add(homeCompany);
        if (!string.IsNullOrWhiteSpace(currentCompany)) codes.Add(currentCompany);
        return codes
            .Select(code => known.FirstOrDefault(k => string.Equals(k.Code, code, StringComparison.OrdinalIgnoreCase)) ?? new SwitchableCompany(code, code))
            .OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

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

        // บริษัทที่มีใน com_company แต่ยังไม่มีโหนด isCompany ในผัง (เช่น UITEST) ใช้ชื่อจาก com_company
        var masterNames = await context.com_companies.AsNoTracking()
            .Where(c => c.code != null)
            .Select(c => new { c.code, c.name })
            .ToListAsync(ct);
        var known = byCode
            .Concat(masterNames.Where(m => byCode.All(b => !string.Equals(b.Code, m.code, StringComparison.OrdinalIgnoreCase)))
                .Select(m => new SwitchableCompany(m.code!, m.name ?? m.code!)))
            .ToList();

        var current = user.FindFirst("payroll_company")?.Value;
        var home = user.FindFirst(HomeCompanyClaim)?.Value ?? current;   // cookie from before home_company existed
        var granted = scope.IsUnrestricted ? new HashSet<string>() : scope.CompanyIds;
        return Allowed(known, user.IsInRole(AdminRoleName), granted, home, current);
    }

    public async Task<bool> IsSwitchAllowedAsync(ClaimsPrincipal user, string targetCompCode, HRMContext context, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetCompCode)) return false;
        var allowed = await GetSwitchableCompaniesAsync(user, context, ct);
        return allowed.Any(c => c.Code == targetCompCode);
    }
}
