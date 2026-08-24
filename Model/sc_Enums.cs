namespace HRM.Models;

// Advance Security (sc_*): the "data-scope" axis, orthogonal to
// sc_role_menu's action-level flags (isactive/canedit/candownload). A role
// can have zero, one, or many sc_role_scope rows; zero rows means
// unrestricted (sees everything) — see RoleScopeSnapshot.FromClaims for why
// that default matters for a safe rollout.
public enum RoleScopeType
{
    Company = 1,
    Org = 2,
    Branch = 3,
    CostCenter = 4,
}
