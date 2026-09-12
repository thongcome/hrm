namespace Advance.SecurityCore.Web;

using Advance.SecurityCore.Data;
using Advance.SecurityCore.Domain;
using Advance.SecurityCore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Adapted from HRM's Endpoints/LoginEndpoints.cs. Logic is unchanged;
// namespace-only substitutions (HRMContext -> SecurityDbContext, HRM.Models
// -> Advance.SecurityCore.Domain) plus one behavioral note:
//
//   The AuthProvider SSO-redirect branch below (`Results.LocalRedirect(
//   $"/auth/sso/{scUser.AuthProvider}/login...")`) assumes a host has mapped
//   an `/auth/sso/{name}/login` route (HRM's ExternalSsoEndpoints, NOT part
//   of this extraction's requested copy list). Left in place because
//   removing it would silently change sign-in behavior for AuthProvider
//   values other than "AD"/null; a host without that route mapped will get
//   a 404 on that redirect rather than a compile error, which is the
//   correct "fail loud, not silent" tradeoff for Phase 0 — see
//   EXTRACTION-PLAN.md.
public static class LoginEndpoints
{
    public static void MapLoginEndpoints(this WebApplication app)
    {
        app.MapPost("/login-handler", async (
            HttpContext httpContext,
            IDbContextFactory<SecurityDbContext> dbFactory,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            LdapAuthService ldapAuth,
            PasswordPolicyService policy,
            SecurityAlertService alerts) =>
        {
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
            var requiresTwoFactor = false;
            var failureAlerted = false;
            var form = await httpContext.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();
            var returnUrl = form["returnUrl"].ToString();

            await using var context = await dbFactory.CreateDbContextAsync();
            var scUser = await context.sc_users.FirstOrDefaultAsync(u => u.loginname == username);

            // An account provisioned for SSO-only login (AuthProvider set to
            // a configured SSO provider name, not "AD") has no usable local
            // password — send it straight to that provider's redirect
            // instead of failing the password form silently.
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

                // No linked ApplicationUser yet — fall through to the same
                // generic error as a wrong password, so this endpoint never
                // reveals which accounts exist.
                if (appUser is not null)
                {
                    if (await userManager.IsLockedOutAsync(appUser))
                        return LockedOut(returnUrl);

                    bool passwordOk;
                    if (scUser.AuthProvider == "AD")
                    {
                        passwordOk = (await ldapAuth.TryBindAsync(username, password)).Succeeded;
                        if (!passwordOk)
                            await userManager.AccessFailedAsync(appUser);
                    }
                    else
                    {
                        var signIn = await signInManager.PasswordSignInAsync(appUser, password, isPersistent: false, lockoutOnFailure: true);
                        if (signIn.IsNotAllowed)
                        {
                            Log(httpContext).LogWarning("Login for {User} is NotAllowed by Identity (unconfirmed account) — falling back to direct sign-in", username);
                            passwordOk = (await signInManager.CheckPasswordSignInAsync(appUser, password, lockoutOnFailure: true)).Succeeded;
                            if (passwordOk) await signInManager.SignInAsync(appUser, isPersistent: false);
                        }
                        else
                        {
                            passwordOk = signIn.Succeeded || signIn.RequiresTwoFactor;
                            requiresTwoFactor = signIn.RequiresTwoFactor;
                        }
                    }

                    if (!passwordOk)
                    {
                        // Mirror Identity's counter onto the legacy sc_user
                        // fields — Identity stays the authority.
                        var isNowLockedOut = await userManager.IsLockedOutAsync(appUser);
                        scUser.invalidpwcount = isNowLockedOut
                            ? policy.Options.MaxFailedAttempts
                            : await userManager.GetAccessFailedCountAsync(appUser);
                        scUser.lastinvalidpwd = DateTime.Now;
                        context.AuditLogs.Add(new AuditLog
                        {
                            ActorUserId = scUser.userid,
                            ActorName = scUser.loginname,
                            Action = AuditActionType.View,
                            EntityType = "sc_user",
                            RecordId = scUser.userid.ToString(),
                            IsSensitiveDataAccess = false,
                            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                            Note = "login-failed",
                        });
                        await context.SaveChangesAsync();
                        await alerts.OnLoginFailedAsync(username, clientIp, isNowLockedOut);
                        failureAlerted = true;

                        if (isNowLockedOut)
                            return LockedOut(returnUrl);
                    }

                    if (passwordOk)
                    {
                        if (scUser.AuthProvider == "AD")
                            await signInManager.SignInAsync(appUser, isPersistent: false);

                        await userManager.ResetAccessFailedCountAsync(appUser);
                        scUser.invalidpwcount = 0;

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
                            Note = requiresTwoFactor ? "login-2fa-pending" : "login",
                        });
                        await context.SaveChangesAsync();

                        if (requiresTwoFactor)
                            return Results.LocalRedirect($"/Account/LoginWith2fa?ReturnUrl={Uri.EscapeDataString(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl)}&RememberMe=false");
                        return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
                    }
                }
            }

            if (!failureAlerted)
                await alerts.OnLoginFailedAsync(username, clientIp, lockedOut: false);
            var errorRedirect = "/login?error=1";
            if (!string.IsNullOrEmpty(returnUrl))
                errorRedirect += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
            return Results.LocalRedirect(errorRedirect);
        }).RequireRateLimiting("login");

        static IResult LockedOut(string returnUrl)
        {
            var url = "/login?error=locked";
            if (!string.IsNullOrEmpty(returnUrl))
                url += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
            return Results.LocalRedirect(url);
        }

        static ILogger Log(HttpContext ctx) =>
            ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Advance.SecurityCore.Web.LoginEndpoints");

        app.MapGet("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.LocalRedirect("/");
        });
    }
}
