namespace HRM.Services.Security;

using System.Security.Claims;
using HRM.Models;
using HRM.Services.Login;
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
// Who may switch where (CEO 26 ก.ย. 2569, decision D3 in
// docs/SecurityCore_Master_Comparison_v1.1.md):
//   - the Admin role: every company (CEO 18 ก.ย. 2569, "advadmin … ทำได้ทุกอย่าง");
//   - anyone else: the companies granted by an active Company-type
//     sc_role_scope row on one of their roles (the "scope_company" claims),
//     set at /admin/system/role-scopes;
//   - plus, always, their own home company and the company they are in now.
// A role with NO scope rows used to mean "may switch to any company", which
// let a UITEST PAYROLL_APPROVER open ADVD's payroll runs. It now means "own
// company only" for switching. Only switching changed: the row-level data
// filters (RoleScopeSnapshot.AllowsEmployee) keep "no scope rows = no
// restriction" until that meaning is changed library-wide (ADP.AI master).
public class CompanySwitchService
{
    public record SwitchableCompany(string Code, string Name);

    public const string AdminRoleName = "Admin";

    public async Task<List<SwitchableCompany>> GetSwitchableCompaniesAsync(ClaimsPrincipal user, HRMContext context, CancellationToken ct = default)
    {
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

        return Allowed(byCode, user, await HomeCompanyAsync(user, context, ct));
    }

    public async Task<bool> IsSwitchAllowedAsync(ClaimsPrincipal user, string targetCompCode, HRMContext context, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetCompCode)) return false;
        var allowed = await GetSwitchableCompaniesAsync(user, context, ct);
        return allowed.Any(c => c.Code == targetCompCode);
    }

    // The pure rule, unit-tested in CompanySwitchRuleTests.
    public static List<SwitchableCompany> Allowed(IReadOnlyList<SwitchableCompany> companies, ClaimsPrincipal user, string? homeCompany)
    {
        var isAdmin = user.FindAll(ClaimTypes.Role)
            .Any(r => string.Equals(r.Value, AdminRoleName, StringComparison.OrdinalIgnoreCase));
        var granted = user.FindAll("scope_company").Select(c => c.Value).ToHashSet();

        var allowed = isAdmin
            ? companies.ToList()
            : companies.Where(c => granted.Contains(c.Code)).ToList();

        // Home and current company are always listed — this is a switcher, not
        // a restriction on what the user already sees, and it is the way back.
        foreach (var code in new[] { homeCompany, user.FindFirst("payroll_company")?.Value })
        {
            if (string.IsNullOrWhiteSpace(code) || allowed.Any(c => c.Code == code)) continue;
            allowed.Add(companies.FirstOrDefault(c => c.Code == code) ?? new SwitchableCompany(code, code));
        }

        return allowed.OrderBy(c => c.Code).ToList();
    }

    // The company the account belongs to — the same resolution the claims
    // factory uses for "payroll_company" at sign-in, so it survives a switch.
    private static async Task<string?> HomeCompanyAsync(ClaimsPrincipal user, HRMContext context, CancellationToken ct)
    {
        if (!long.TryParse(user.FindFirst("sc_userid")?.Value, out var userId)) return null;
        var scUser = await context.sc_users.AsNoTracking().FirstOrDefaultAsync(u => u.userid == userId, ct);
        return scUser is null ? null : await PayrollCompanyResolver.ResolveAsync(context, scUser, ct);
    }
}
