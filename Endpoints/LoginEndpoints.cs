namespace HRM.Endpoints;

using HRM.Data;
using HRM.Models;
using HRM.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Login, extracted out of Program.cs so it's a single, self-contained place
// for the team to customize going forward. Sign-in itself is delegated to
// ASP.NET Core Identity's SignInManager/UserManager (battle-tested framework
// code) instead of hand-rolled password verification + claims + cookie
// sign-in — this file's job is just the bridge: look up the sc_user by
// loginname (the identifier people actually know), find its linked
// ApplicationUser, then hand off to Identity. Role/menu/company claims are
// attached automatically by ScUserClaimsPrincipalFactory during
// SignInManager.SignInAsync — no claims-building code lives here at all,
// which is exactly the kind of duplication that caused a real bug once
// (see ScUserClaimsPrincipalFactory.cs).
public static class LoginEndpoints
{
    public static void MapLoginEndpoints(this WebApplication app)
    {
        app.MapPost("/login-handler", async (
            HttpContext httpContext,
            IDbContextFactory<HRMContext> dbFactory,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            LdapAuthService ldapAuth) =>
        {
            var form = await httpContext.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();
            var returnUrl = form["returnUrl"].ToString();

            await using var context = await dbFactory.CreateDbContextAsync();
            var scUser = await context.sc_users.FirstOrDefaultAsync(u => u.loginname == username);

            // An account provisioned for SSO-only login (AuthProvider set to
            // a configured ExternalAuth:Sso:Providers name, not "AD") has no
            // usable local password — send it straight to that provider's
            // redirect instead of failing the password form silently.
            if (scUser is not null && !string.IsNullOrEmpty(scUser.AuthProvider) && scUser.AuthProvider != "AD")
            {
                var ssoReturn = string.IsNullOrEmpty(returnUrl) ? "" : $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
                return Results.LocalRedirect($"/auth/sso/{scUser.AuthProvider}/login{ssoReturn}");
            }

            // sc_user-level gates Identity has no concept of — checked before
            // ever touching the Identity sign-in machinery.
            if (scUser is not null && !scUser.isdisable && !scUser.iscancel && scUser.isActivate)
            {
                var appUser = await userManager.Users.FirstOrDefaultAsync(u => u.userid == scUser.userid);

                // No linked ApplicationUser yet (sc_user not backfilled) —
                // fall through to the same generic error as a wrong password,
                // so this endpoint never reveals which accounts exist.
                if (appUser is not null)
                {
                    // AD/SSO scaffold (CEO, 2026-09-07): AuthProvider=="AD"
                    // means this account's password is verified against the
                    // configured AD/LDAP server (LDAP BIND) instead of the
                    // local Identity hash — everything downstream (SignInAsync,
                    // claims, audit) is identical either way.
                    var passwordOk = scUser.AuthProvider == "AD"
                        ? (await ldapAuth.TryBindAsync(username, password)).Succeeded
                        : (await signInManager.CheckPasswordSignInAsync(appUser, password, lockoutOnFailure: true)).Succeeded;

                    if (passwordOk)
                    {
                        await signInManager.SignInAsync(appUser, isPersistent: false);

                        // Written directly against the already-open context
                        // rather than via IAuditLogger — SignInAsync only
                        // updates the response cookie, so httpContext.User
                        // in this same request is still the pre-login
                        // principal and IAuditLogger's HttpContext-based
                        // actor resolution would see no one signed in yet.
                        scUser.lasttimelogin = DateTime.Now;
                        context.AuditLogs.Add(new AuditLog
                        {
                            ActorUserId = scUser.userid,
                            ActorName = scUser.loginname,
                            Action = AuditActionType.View,
                            EntityType = "sc_user",
                            RecordId = scUser.userid.ToString(),
                            IsSensitiveDataAccess = false,
                            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                            Note = "login",
                        });
                        await context.SaveChangesAsync();

                        return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
                    }
                }
            }

            var errorRedirect = "/login?error=1";
            if (!string.IsNullOrEmpty(returnUrl))
                errorRedirect += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
            return Results.LocalRedirect(errorRedirect);
        }).RequireRateLimiting("login");

        app.MapGet("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.LocalRedirect("/");
        });
    }
}
