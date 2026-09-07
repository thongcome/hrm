using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Auth;

// Resolves an SSO identity (an OIDC provider's "sub" claim) to an existing
// sc_user — the SSO half of the AD/SSO scaffold (CEO, 2026-09-07: "design
// for both, prepare structure first"). AD/LDAP does NOT go through this
// service: the login form already collects a loginname the existing
// sc_user lookup in LoginEndpoints.cs can match directly, so LdapAuthService
// only needs to replace the password check. SSO has no typed loginname at
// all (it's a redirect+callback), so this is what links a given external
// identity to an sc_user the first time it's seen (JIT link, persisted in
// sc_external_identity), then reuses that link on every later login.
//
// Deliberately NEVER creates a new sc_user. AD/SSO confirms WHO someone is;
// it doesn't decide THAT they should have an HRM account — that's still an
// HR/admin decision, same fail-closed principle as every permission gate
// elsewhere in this codebase (AD.CRUDManage, ProgramRoleService, etc.). A
// person the JIT match can't find gets a clear "contact HR" error, not a
// freshly-minted account.
public class ExternalIdentityProvisioningService(IDbContextFactory<HRMContext> dbFactory)
{
    public record ResolveResult(bool Succeeded, sc_user? ScUser, string? Error);

    public async Task<ResolveResult> ResolveAsync(
        string provider, string externalKey, string? matchEmail, string? displayName, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var link = await context.sc_external_identities
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ExternalKey == externalKey && l.IsActive, ct);

        sc_user? scUser;
        var isNewLink = link is null;
        if (link is not null)
        {
            scUser = await context.sc_users.FirstOrDefaultAsync(u => u.userid == link.ScUserId, ct);
            if (scUser is null)
                return new ResolveResult(false, null, "บัญชีที่เชื่อมไว้ถูกลบไปแล้ว กรุณาติดต่อผู้ดูแลระบบ");
        }
        else
        {
            // JIT link on first login — match by email only (the one field
            // an IdP claim can be trusted to line up with sc_user.email;
            // matching by name would risk linking the wrong person on a
            // common-name collision).
            scUser = string.IsNullOrWhiteSpace(matchEmail)
                ? null
                : await context.sc_users.FirstOrDefaultAsync(u => u.email == matchEmail, ct);

            if (scUser is null)
                return new ResolveResult(false, null, "ไม่พบพนักงานที่ตรงกับบัญชีนี้ในระบบ กรุณาติดต่อฝ่ายบุคคล");

            link = new sc_external_identity
            {
                Provider = provider,
                ExternalKey = externalKey,
                ScUserId = scUser.userid,
                DisplayNameSnapshot = displayName,
                IsActive = true,
                LinkedDate = DateTime.Now,
                LastLoginDate = DateTime.Now,
            };
            context.sc_external_identities.Add(link);
            await context.SaveChangesAsync(ct);
        }

        if (scUser.isdisable || scUser.iscancel || !scUser.isActivate)
            return new ResolveResult(false, null, "บัญชีนี้ถูกระงับการใช้งาน");

        if (!isNewLink)
        {
            link.LastLoginDate = DateTime.Now;
            await context.SaveChangesAsync(ct);
        }

        return new ResolveResult(true, scUser, null);
    }
}
