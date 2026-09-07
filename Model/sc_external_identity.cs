using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Maps one external SSO identity (an OIDC "sub" claim, one per provider) to
// an existing sc_user — AD/SSO scaffold (CEO, 2026-09-07: design for both
// AD/LDAP and SSO, prepare structure only, no real customer IdP yet).
// AD/LDAP does NOT use this table: the login form already collects a
// loginname the existing sc_user lookup can match directly, so LdapAuthService
// only needs to replace the password check, not identity resolution.
// SSO has no typed loginname (it's a redirect+callback), so this table is
// what ExternalIdentityProvisioningService links the FIRST time a given
// external identity signs in (JIT link), then reuses on every later login.
// ScUserId is a soft link (no FK/navigation) — same convention as
// AuditLog.ActorUserId and every other *UserId column across this codebase
// that references sc_user.userid.
[Table("sc_external_identity")]
[Index(nameof(Provider), nameof(ExternalKey), IsUnique = true)]
public class sc_external_identity
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string Provider { get; set; } = null!; // matches ExternalAuth:Sso:Providers[].Name

    [Required, StringLength(250)]
    public string ExternalKey { get; set; } = null!; // the provider's "sub" (ClaimTypes.NameIdentifier)

    public long ScUserId { get; set; }

    // From the IdP's claims at link time — display only, never used for
    // matching (matching happens once, by email/loginname, at JIT-link time).
    [StringLength(250)]
    public string? DisplayNameSnapshot { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime LinkedDate { get; set; } = DateTime.Now;
    public DateTime? LastLoginDate { get; set; }
}
