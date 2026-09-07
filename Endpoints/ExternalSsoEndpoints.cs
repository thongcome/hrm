namespace HRM.Endpoints;

using System.Security.Claims;
using HRM.Data;
using HRM.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Consumer-side SSO login (HRM as the OIDC Relying Party) — the other half
// of AD/SSO scaffold (CEO, 2026-09-07: "a link on the customer's own web
// portal that, when clicked, logs the employee straight in with their own
// permissions"). This is the opposite direction from OidcEndpoints.cs (HRM
// as an OIDC PROVIDER, issuing tokens to downstream apps like ERP) — here
// HRM is the one trusting an external IdP's assertion about who someone is.
//
// Inert until a real provider is configured (ExternalAuth:Sso:Providers is
// empty by default — see Program.cs), since {provider} routes to whichever
// named OpenIdConnect scheme Program.cs registered for that name; an
// unconfigured provider name just 404s/challenges nothing.
public static class ExternalSsoEndpoints
{
    public static void MapExternalSsoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth/sso");

        group.MapGet("/{provider}/login", (
            string provider, string? returnUrl, SignInManager<ApplicationUser> signInManager) =>
        {
            var redirectUrl = $"/auth/sso/{provider}/callback"
                + (string.IsNullOrEmpty(returnUrl) ? "" : $"?returnUrl={Uri.EscapeDataString(returnUrl)}");
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Results.Challenge(properties, new[] { provider });
        });

        group.MapGet("/{provider}/callback", async (
            string provider, string? returnUrl,
            SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
            ExternalIdentityProvisioningService provisioning) =>
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null)
                return Results.LocalRedirect("/login?error=1");

            var externalKey = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? info.ProviderKey;
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var displayName = info.Principal.FindFirstValue(ClaimTypes.Name);

            var resolved = await provisioning.ResolveAsync(provider, externalKey, email, displayName);
            if (!resolved.Succeeded || resolved.ScUser is null)
                return Results.LocalRedirect($"/login?error=1&ssoError={Uri.EscapeDataString(resolved.Error ?? "")}");

            var appUser = await userManager.Users.FirstOrDefaultAsync(u => u.userid == resolved.ScUser.userid);
            if (appUser is null)
                return Results.LocalRedirect($"/login?error=1&ssoError={Uri.EscapeDataString("บัญชีนี้ยังไม่ได้เชื่อมกับระบบ Identity — ติดต่อผู้ดูแลระบบ")}");

            // Role/menu/company claims attach automatically here via
            // ScUserClaimsPrincipalFactory, same as the password-login path
            // in LoginEndpoints.cs — no claims-building code needed.
            await signInManager.SignInAsync(appUser, isPersistent: false);

            return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        });
    }
}
