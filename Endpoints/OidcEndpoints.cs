namespace HRM.Endpoints;

using System.Security.Claims;
using HRM.Data;
using HRM.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore; // OpenIddictServerAspNetCoreHelpers.GetOpenIddictServerRequest()
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

// Makes HRM an OIDC Identity Provider (authorization-code + PKCE flow) for
// downstream HumanOk apps — first consumer is ERP, per HR-SSO-Handoff.md.
// HRM only vouches for identity + a per-client role string here
// (authentication); each client still owns its own permission mapping from
// that role (authorization) — see Sso_ClientRoleMapping's doc comment for
// why the role itself is free text rather than something HRM enumerates.
//
// This reuses the existing cookie sign-in at /login entirely (see the
// authorize handler below) — there is no separate "SSO login page".
public static class OidcEndpoints
{
    public static void MapOidcEndpoints(this WebApplication app)
    {
        app.MapMethods("/connect/authorize", new[] { "GET", "POST" }, async (
            HttpContext httpContext,
            IDbContextFactory<HRMContext> hrmDbFactory,
            UserManager<ApplicationUser> userManager) =>
        {
            var request = httpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Not signed in to HRM yet — bounce to the existing /login page
            // (ConfigureApplicationCookie's LoginPath) and come straight
            // back here afterward. No separate SSO-specific login UI.
            var cookieResult = await httpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            if (!cookieResult.Succeeded || cookieResult.Principal is null)
                return Results.Challenge(authenticationSchemes: new[] { IdentityConstants.ApplicationScheme });

            var appUser = await userManager.GetUserAsync(cookieResult.Principal);
            if (appUser is null)
                return Results.Forbid(authenticationSchemes: new[] { IdentityConstants.ApplicationScheme });

            await using var context = await hrmDbFactory.CreateDbContextAsync();
            var principal = await BuildClientPrincipalAsync(context, appUser, request.ClientId!, request.GetScopes());

            return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        });

        app.MapPost("/connect/token", async (HttpContext httpContext) =>
        {
            var request = httpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsAuthorizationCodeGrantType())
            {
                // The authorization code already carries the exact
                // principal built in /connect/authorize above (OpenIddict
                // persists it server-side, keyed by the code) — just
                // re-issue tokens from it, no re-querying needed.
                var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                if (!result.Succeeded || result.Principal is null)
                    return Results.Forbid(authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });

                return Results.SignIn(result.Principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new NotImplementedException("Only the authorization_code grant type is supported.");
        });

        app.MapGet("/connect/userinfo", (ClaimsPrincipal user) => Results.Ok(new Dictionary<string, object?>
        {
            [Claims.Subject] = user.GetClaim(Claims.Subject),
            [Claims.Name] = user.GetClaim(Claims.Name),
            [Claims.Email] = user.GetClaim(Claims.Email),
            [Claims.Role] = user.GetClaim(Claims.Role),
        })).RequireAuthorization(policy => policy
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());

        app.MapMethods("/connect/logout", new[] { "GET", "POST" }, async (HttpContext httpContext) =>
        {
            // Sign out of HRM's own session too, not just the OIDC server
            // scheme — otherwise a later SSO login silently reuses the old
            // HRM cookie without re-prompting.
            await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return Results.SignOut(authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        });
    }

    // Deliberately builds a small, clean claim set scoped to what a
    // downstream client should see (sub/name/email/role) rather than
    // reusing ScUserClaimsPrincipalFactory's principal — that one carries
    // dozens of HRM-internal claims (sc_userid, menu, menu_edit, ...) that
    // are meaningless (and none of ERP's business) outside HRM itself.
    private static async Task<ClaimsPrincipal> BuildClientPrincipalAsync(
        HRMContext context, ApplicationUser appUser, string clientId, IEnumerable<string> scopes)
    {
        var scUser = await context.sc_users.FirstOrDefaultAsync(u => u.userid == appUser.userid);
        var fullName = scUser is null ? appUser.UserName : $"{scUser.firstname} {scUser.lastname}".Trim();

        var role = await context.Sso_ClientRoleMappings
            .Where(m => m.ScUserId == appUser.userid && m.ClientId == clientId)
            .Select(m => m.Role)
            .FirstOrDefaultAsync()
            ?? "Viewer"; // matches the client's own documented safe fallback for an unmapped user

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, appUser.Id)
            .SetClaim(Claims.Name, fullName)
            .SetClaim(Claims.Email, appUser.Email)
            .SetClaim(Claims.Role, role);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);

        foreach (var claim in principal.Claims)
            claim.SetDestinations(GetDestinations(claim, principal));

        return principal;
    }

    // Standard OpenIddict claim-destination pattern: access_token always
    // gets the claim (it's what a resource server/API would introspect),
    // id_token only gets it when the client actually asked for the scope
    // that claim belongs to.
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;
                if (principal.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;
                if (principal.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                // ERP only requests openid/profile/email (no dedicated
                // "roles" scope) but still expects role in the id_token
                // per the handoff doc, so it rides along with profile.
                if (principal.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
