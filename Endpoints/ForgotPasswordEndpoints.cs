namespace HRM.Endpoints;

using HRM.Data;
using HRM.Models;
using HRM.Services;
using HRM.Services.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Self-service password reset, mirroring LoginEndpoints.cs's shape: plain HTML
// forms (Components/Login/ForgotPassword.razor, ResetPassword.razor) posting
// to real HTTP endpoints here, not Blazor circuit event handlers — the same
// "response already started" constraint applies, and RequireRateLimiting only
// throttles real per-request endpoints, not SignalR circuit calls (see
// Endpoints/CareerEndpoints.cs for the same reasoning on a public endpoint).
//
// Deliberately does NOT email a new plaintext password the way
// PayrollEmployeeAdmin.razor's admin-triggered reset does — that flow was a
// conscious choice for an HR-operated action; this one is self-service and
// unauthenticated, so it uses ASP.NET Core Identity's token-based reset
// (GeneratePasswordResetTokenAsync/ResetPasswordAsync) so the user picks
// their own new password and a leaked email only grants a single-use,
// time-limited link rather than a working password outright.
public static class ForgotPasswordEndpoints
{
    public static void MapForgotPasswordEndpoints(this WebApplication app)
    {
        app.MapPost("/forgot-password-handler", async (
            HttpContext httpContext,
            IDbContextFactory<HRMContext> dbFactory,
            UserManager<ApplicationUser> userManager,
            EmailSender emailSender) =>
        {
            var form = await httpContext.Request.ReadFormAsync();
            var username = form["username"].ToString();

            await using var context = await dbFactory.CreateDbContextAsync();
            var scUser = await context.sc_users.FirstOrDefaultAsync(u => u.loginname == username);

            // Same gate as /login-handler, and — just as there — this never
            // changes what the caller sees on failure: whether the loginname
            // doesn't exist, the account is disabled, there's no linked
            // Identity account, or there's simply no email on file, the
            // response is identical. Leaking any of those distinctions here
            // would let this endpoint be used to enumerate valid usernames.
            if (scUser is not null && !scUser.isdisable && !scUser.iscancel && scUser.isActivate)
            {
                var appUser = await userManager.Users.FirstOrDefaultAsync(u => u.userid == scUser.userid);
                if (appUser is not null)
                {
                    string? email = null;
                    if (!string.IsNullOrWhiteSpace(scUser.empid))
                    {
                        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == scUser.empid);
                        if (emp is not null)
                            email = await EmployeeEmailResolver.ResolveAsync(context, emp.id);
                    }

                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        var token = await userManager.GeneratePasswordResetTokenAsync(appUser);
                        var resetUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/reset-password" +
                            $"?u={Uri.EscapeDataString(scUser.loginname)}&t={Uri.EscapeDataString(token)}";

                        var body = $"<p>คุณได้ขอรีเซ็ตรหัสผ่านสำหรับบัญชี <b>{scUser.loginname}</b> ในระบบ HumanOk</p>" +
                            $"<p><a href=\"{resetUrl}\">คลิกที่นี่เพื่อตั้งรหัสผ่านใหม่</a></p>" +
                            "<p>ลิงก์นี้จะหมดอายุใน 1 ชั่วโมง หากคุณไม่ได้เป็นผู้ขอ กรุณาเพิกเฉยต่ออีเมลฉบับนี้</p>";
                        try
                        {
                            await emailSender.SendEmailAsync(email, "รีเซ็ตรหัสผ่านเข้าระบบ HumanOk", body);
                        }
                        catch (Exception ex)
                        {
                            // Best-effort, same as TrySendCredentialEmailAsync — never let an
                            // SMTP hiccup turn into a visible failure that would also leak
                            // "yes, this account exists and has an email on file".
                            Serilog.Log.Error(ex, "Failed to send password-reset email for {LoginName}", scUser.loginname);
                        }
                    }
                }
            }

            return Results.LocalRedirect("/forgot-password?sent=1");
        }).RequireRateLimiting("forgot-password");

        app.MapPost("/reset-password-handler", async (
            HttpContext httpContext,
            IDbContextFactory<HRMContext> dbFactory,
            IPasswordHasher<sc_user> passwordHasher,
            UserManager<ApplicationUser> userManager) =>
        {
            var form = await httpContext.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var token = form["token"].ToString();
            var newPassword = form["newPassword"].ToString();
            var confirmPassword = form["confirmPassword"].ToString();

            string BackToForm(string error) =>
                $"/reset-password?u={Uri.EscapeDataString(username)}&t={Uri.EscapeDataString(token)}&error={error}";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
                return Results.LocalRedirect("/forgot-password");

            if (newPassword != confirmPassword)
                return Results.LocalRedirect(BackToForm("mismatch"));

            await using var context = await dbFactory.CreateDbContextAsync();
            var scUser = await context.sc_users.FirstOrDefaultAsync(u => u.loginname == username);
            if (scUser is null || scUser.isdisable || scUser.iscancel || !scUser.isActivate)
                return Results.LocalRedirect(BackToForm("invalid"));

            var appUser = await userManager.Users.FirstOrDefaultAsync(u => u.userid == scUser.userid);
            if (appUser is null)
                return Results.LocalRedirect(BackToForm("invalid"));

            var result = await userManager.ResetPasswordAsync(appUser, token, newPassword);
            if (!result.Succeeded)
            {
                var isPasswordPolicyFailure = result.Errors.Any(e => e.Code.StartsWith("Password", StringComparison.Ordinal));
                return Results.LocalRedirect(BackToForm(isPasswordPolicyFailure ? "weak" : "invalid"));
            }

            // The token round-trip through Identity above is the actual proof
            // of ownership; sc_user.password stays in sync purely as the
            // legacy/reference field the same way every other reset path in
            // this app keeps it in sync (see UserAdmin.razor's
            // ResetPasswordAsync / PayrollEmployeeAdmin.razor's
            // ConfirmResetPasswordAsync) — it is never read for sign-in.
            scUser.password = passwordHasher.HashPassword(scUser, newPassword);
            // Unlike an admin-issued reset, the user just chose this password
            // themselves, so there's nothing to force them to change again.
            scUser.isforcechanged = false;
            scUser.moddate = DateTime.Now;
            scUser.modby = "self-service-reset";
            await context.SaveChangesAsync();

            // Proven ownership of the account via a valid emailed token, so
            // clear any lockout from earlier failed sign-in attempts rather
            // than leaving the user locked out immediately after resetting.
            await userManager.SetLockoutEndDateAsync(appUser, null);
            await userManager.ResetAccessFailedCountAsync(appUser);

            return Results.LocalRedirect("/login?resetsuccess=1");
        }).RequireRateLimiting("login");
    }
}
