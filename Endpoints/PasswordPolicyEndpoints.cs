namespace HRM.Endpoints;

using System.Security.Claims;
using HRM.Data;
using HRM.Middleware;
using HRM.Models;
using HRM.Services.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// The modern replacement for SysMgmt/User/PasswdAction.jsp: the one place a
// user sets their own password when the system is refusing to let them past
// the door. Same three checks the JSP did (old password must verify, new one
// twice, then release the force flag), with Identity owning the hashing and
// the token/lockout state instead of solar.Encrypt's sqrt loop.
//
// Kept as a real form POST endpoint rather than a Blazor event handler for
// the same reason as /login-handler: signing state changes must happen on a
// plain HTTP request, not over the SignalR circuit, or the auth cookie can't
// be rewritten (RefreshSignInAsync at the end needs a live response).
public static class PasswordPolicyEndpoints
{
    public static void MapPasswordPolicyEndpoints(this WebApplication app)
    {
        app.MapPost("/force-change-password-handler", async (
            HttpContext httpContext,
            IDbContextFactory<HRMContext> dbFactory,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IPasswordHasher<sc_user> passwordHasher,
            PasswordPolicyService policy) =>
        {
            static IResult BackToForm(string error) =>
                Results.LocalRedirect($"{ForcePasswordChangeMiddleware.ChangePath}?error={error}");

            // The middleware only pins authenticated users, so reaching this
            // handler anonymously means the session expired mid-form.
            if (httpContext.User?.Identity?.IsAuthenticated != true)
                return Results.LocalRedirect("/login");

            var scUserIdClaim = httpContext.User.FindFirstValue("sc_userid");
            if (!long.TryParse(scUserIdClaim, out var scUserId))
                return Results.LocalRedirect("/login");

            var form = await httpContext.Request.ReadFormAsync();
            var currentPassword = form["currentPassword"].ToString();
            var newPassword = form["newPassword"].ToString();
            var confirmPassword = form["confirmPassword"].ToString();

            if (newPassword != confirmPassword)
                return BackToForm("mismatch");

            // PasswdAction.jsp allowed re-submitting the same password, which
            // defeats the whole point of forcing the change (an admin reset
            // to a known default would stay that default). Rejected here.
            if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
                return BackToForm("same");

            await using var context = await dbFactory.CreateDbContextAsync();
            var scUser = await context.sc_users.FirstOrDefaultAsync(u => u.userid == scUserId);
            if (scUser is null || scUser.isdisable || scUser.iscancel || !scUser.isActivate)
                return Results.LocalRedirect("/logout");

            var appUser = await userManager.Users.FirstOrDefaultAsync(u => u.userid == scUserId);
            if (appUser is null)
                return BackToForm("invalid");

            var result = await userManager.ChangePasswordAsync(appUser, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                // Distinguish "your current password is wrong" from "the new
                // one doesn't meet the policy" — the same distinction the
                // reset-password flow already makes, because conflating them
                // sends people round the loop guessing which half failed.
                var isPasswordPolicyFailure = result.Errors.Any(e =>
                    e.Code.StartsWith("Password", StringComparison.Ordinal)
                    && !e.Code.Contains("Mismatch", StringComparison.Ordinal));
                return BackToForm(isPasswordPolicyFailure ? "weak" : "wrongcurrent");
            }

            // sc_user.password stays a legacy/reference mirror (never read for
            // sign-in) exactly as in every other password path in this app.
            scUser.password = passwordHasher.HashPassword(scUser, newPassword);
            policy.StampPasswordChanged(scUser, "force-change");
            context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = scUser.userid,
                ActorName = scUser.loginname,
                Action = AuditActionType.Update,
                EntityType = "sc_user",
                RecordId = scUser.userid.ToString(),
                IsSensitiveDataAccess = false,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                Note = "force-change-password",
            });
            await context.SaveChangesAsync();

            // Rebuilds the principal through ScUserClaimsPrincipalFactory, so
            // the pwd_change_required claim is gone and the middleware stops
            // pinning this user — without this the change would succeed and
            // the user would still be trapped on the form.
            await signInManager.RefreshSignInAsync(appUser);

            return Results.LocalRedirect("/?pwdchanged=1");
        }).RequireRateLimiting("login");
    }
}
