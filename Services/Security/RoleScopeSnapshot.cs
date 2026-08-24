namespace HRM.Services.Security;

using System.Security.Claims;

// Resolved shape of a signed-in user's sc_role_scope grants, computed once
// from claims (see ScUserClaimsPrincipalFactory) and attached to an
// HRMContext instance via HRMContext.CurrentScope so the EF query filter on
// Hremployee (Model/HRMContext.Security.cs) can read it as plain instance
// state — no DI, no AsyncLocal. See ScopedDbContextExtensions.cs for why
// those two more "obvious" mechanisms don't actually work in this codebase.
public class RoleScopeSnapshot
{
    public bool IsUnrestricted { get; init; }
    public HashSet<string> CompanyIds { get; init; } = new();

    // Exact-match prefixes plus their pre-formed "prefix.%" LIKE patterns —
    // computed once here so the EF query filter expression never needs to
    // concatenate strings inside the tree (string concatenation on a
    // per-element basis inside HasQueryFilter risks not translating to SQL
    // cleanly; plain EF.Functions.Like(column, capturedValue) always does).
    public HashSet<string> OrgExactCodes { get; init; } = new();
    public List<string> OrgLikePatterns { get; init; } = new();

    public HashSet<string> CostCenterCodes { get; init; } = new();

    public static readonly RoleScopeSnapshot Unrestricted = new() { IsUnrestricted = true };

    public static RoleScopeSnapshot FromClaims(ClaimsPrincipal user)
    {
        // Union semantics across a user's roles: if ANY role they hold is
        // unrestricted (has zero sc_role_scope rows configured — see
        // ScUserClaimsPrincipalFactory), the whole session is unrestricted,
        // even if another role they also hold IS scoped. RBAC access is
        // additive; a scoped role never takes visibility away that an
        // unrestricted role already grants. This also covers the
        // deliberate, safe rollout default: every role starts with zero
        // sc_role_scope rows, so nobody's visibility changes until an
        // admin actually configures a scope for their role.
        if (user.HasClaim("scope_unrestricted", "1"))
            return Unrestricted;

        var companyIds = user.FindAll("scope_company").Select(c => c.Value).ToHashSet();
        var orgPrefixes = user.FindAll("scope_org").Select(c => c.Value).ToList();
        var costCenters = user.FindAll("scope_costcenter").Select(c => c.Value).ToHashSet();

        if (companyIds.Count == 0 && orgPrefixes.Count == 0 && costCenters.Count == 0)
            return Unrestricted;

        return new RoleScopeSnapshot
        {
            IsUnrestricted = false,
            CompanyIds = companyIds,
            OrgExactCodes = orgPrefixes.ToHashSet(),
            OrgLikePatterns = orgPrefixes.Select(p => p + ".%").ToList(),
            CostCenterCodes = costCenters,
        };
    }

    // Plain C# check (not translated to SQL) for write-time validation —
    // e.g. EmployeePositionSync uses this to stop a scoped admin from
    // assigning an employee into an org outside their own scope. Mirrors
    // the exact same three-way logic as the EF query filter.
    public bool AllowsEmployee(string? companyId, string? orgCodeFull, string? costCenterCode)
    {
        if (IsUnrestricted) return true;
        if (companyId is not null && CompanyIds.Contains(companyId)) return true;
        if (orgCodeFull is not null)
        {
            if (OrgExactCodes.Contains(orgCodeFull)) return true;
            if (OrgExactCodes.Any(prefix => orgCodeFull.StartsWith(prefix + ".", StringComparison.Ordinal))) return true;
        }
        if (costCenterCode is not null && CostCenterCodes.Contains(costCenterCode)) return true;
        return false;
    }
}
