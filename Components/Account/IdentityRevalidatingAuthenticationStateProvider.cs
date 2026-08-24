using HRM.Data;
using HRM.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace HRM.Components.Account
{
    // Server-side AuthenticationStateProvider that revalidates the
    // connected user on an interval. Originally just the Identity security
    // stamp (unchanged below); extended for Advance Security slice 2 to
    // also check sc_user.permversion (so a role/menu/scope change takes
    // effect within one revalidation, not just at next login),
    // sc_user_session.isrevoked (so an admin-revoked session actually gets
    // kicked out, not just marked in the DB), and sc_user's own
    // disabled/cancelled/deactivated flags (same gap ScUserClaimsPrincipalFactory
    // already checks at sign-in, but nothing previously re-checked it for
    // an already-open session).
    //
    // 5 minutes, not the original 30 — "changes take effect immediately"
    // per the security blueprint doesn't mean instant (that would need a
    // push mechanism this app has no infrastructure for), but 30 minutes of
    // staleness for a revoked session or an admin-yanked role is too long
    // for a security control. 5 minutes is a deliberate trade-off between
    // that and the DB load of every open circuit re-querying sc_user on
    // every interval — revisit if either side of that trade-off turns out
    // to matter in practice.
    internal sealed class IdentityRevalidatingAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory,
            IOptions<IdentityOptions> options)
        : RevalidatingServerAuthenticationStateProvider(loggerFactory)
    {
        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(5);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            // Get the user manager from a new scope to ensure it fetches fresh data
            await using var scope = scopeFactory.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var principal = authenticationState.User;

            if (!await ValidateSecurityStampAsync(userManager, principal))
                return false;

            var hrmDbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
            await using var context = await hrmDbFactory.CreateDbContextAsync(cancellationToken);
            return await ValidateScUserStateAsync(context, principal, cancellationToken);
        }

        private async Task<bool> ValidateSecurityStampAsync(UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return false;
            }
            else if (!userManager.SupportsUserSecurityStamp)
            {
                return true;
            }
            else
            {
                var principalStamp = principal.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType);
                var userStamp = await userManager.GetSecurityStampAsync(user);
                return principalStamp == userStamp;
            }
        }

        private static async Task<bool> ValidateScUserStateAsync(HRMContext context, ClaimsPrincipal principal, CancellationToken ct)
        {
            var scUserIdClaim = principal.FindFirstValue("sc_userid");
            // No sc_userid claim means this principal never had SC_* claims
            // attached in the first place (e.g. Identity-only accounts not
            // linked to an sc_user) — nothing here to revalidate against.
            if (!long.TryParse(scUserIdClaim, out var scUserId))
                return true;

            var scUser = await context.sc_users.FirstOrDefaultAsync(u => u.userid == scUserId, ct);
            if (scUser is null || scUser.isdisable || scUser.iscancel || !scUser.isActivate)
                return false;

            var principalPermVersion = principal.FindFirstValue("permversion");
            if (principalPermVersion != scUser.permversion.ToString())
                return false;

            var sessionToken = principal.FindFirstValue("sessionid");
            if (!string.IsNullOrEmpty(sessionToken))
            {
                var session = await context.sc_user_sessions.FirstOrDefaultAsync(s => s.sessiontoken == sessionToken, ct);
                if (session is null || session.isrevoked)
                    return false;

                session.lastseendate = DateTime.Now;
                await context.SaveChangesAsync(ct);
            }

            return true;
        }
    }
}
