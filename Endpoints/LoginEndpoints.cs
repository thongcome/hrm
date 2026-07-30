namespace HRM.Endpoints;

using HRM.Data;
using HRM.Models;
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
            SignInManager<ApplicationUser> signInManager) =>
        {
            var form = await httpContext.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();
            var returnUrl = form["returnUrl"].ToString();

            await using var context = await dbFactory.CreateDbContextAsync();
            var scUser = await context.sc_users.FirstOrDefaultAsync(u => u.loginname == username);

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
                    var result = await signInManager.CheckPasswordSignInAsync(appUser, password, lockoutOnFailure: true);
                    if (result.Succeeded)
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
