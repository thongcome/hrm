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
            LdapAuthService ldapAuth,
            HRM.Services.Security.PasswordPolicyService policy,
            HRM.Services.Security.SecurityAlertService alerts) =>
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
                    // Lockout, checked before the password is even looked at —
                    // otherwise a locked account keeps burning attempts and
                    // reports "wrong password" forever. The legacy system
                    // (solar.SecurityBean.addInvalidCount/disableUser) had the
                    // same counter but no way back except an admin; Identity's
                    // LockoutEnd expires on its own.
                    if (await userManager.IsLockedOutAsync(appUser))
                        return LockedOut(returnUrl);

                    // AD/SSO scaffold (CEO, 2026-09-07): AuthProvider=="AD"
                    // means this account's password is verified against the
                    // configured AD/LDAP server (LDAP BIND) instead of the
                    // local Identity hash — everything downstream (SignInAsync,
                    // claims, audit) is identical either way.
                    bool passwordOk;
                    if (scUser.AuthProvider == "AD")
                    {
                        passwordOk = (await ldapAuth.TryBindAsync(username, password)).Succeeded;
                        // CheckPasswordSignInAsync does this for local
                        // accounts; the AD branch has to drive the counter
                        // itself or AD-backed accounts would be the one door
                        // in the building with no lockout on it.
                        if (!passwordOk)
                            await userManager.AccessFailedAsync(appUser);
                    }
                    else
                    {
                        // PasswordSignInAsync (ไม่ใช่ CheckPasswordSignInAsync) เพื่อให้ 2FA ของ Identity ทำงาน: ถ้าผู้ใช้เปิด
                        // แอปยืนยันตัวตนไว้ จะได้ RequiresTwoFactor + cookie ชั่วคราว แล้วไปกรอกรหัสที่ /Account/LoginWith2fa
                        var signIn = await signInManager.PasswordSignInAsync(appUser, password, isPersistent: false, lockoutOnFailure: true);
                        passwordOk = signIn.Succeeded || signIn.RequiresTwoFactor;
                        requiresTwoFactor = signIn.RequiresTwoFactor;
                    }

                    if (!passwordOk)
                    {
                        // Mirror Identity's counter onto the legacy sc_user
                        // fields. Identity (AspNetUsers.AccessFailedCount /
                        // LockoutEnd) stays the authority — these two columns
                        // exist so the admin screen and any legacy report see
                        // the truth instead of a permanently-zero column.
                        // Identity zeroes AccessFailedCount at the moment it
                        // sets LockoutEnd, so reading it straight through
                        // would leave the legacy column showing 0 on exactly
                        // the attempt that mattered. Report the threshold
                        // instead once the account is actually locked.
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

                        // That last attempt may be the one that tripped the
                        // lockout — say so rather than letting them find out
                        // by trying the correct password and still failing.
                        if (isNowLockedOut)
                            return LockedOut(returnUrl);
                    }

                    if (passwordOk)
                    {
                        // ผู้ใช้รหัสผ่านในระบบถูก sign-in โดย PasswordSignInAsync แล้ว — ออก cookie ซ้ำเฉพาะทาง AD (LDAP)
                        if (scUser.AuthProvider == "AD")
                            await signInManager.SignInAsync(appUser, isPersistent: false);

                        // Successful sign-in clears the counter on both sides
                        // (the JSP system's clearInvalidCount(), which ran at
                        // exactly this point).
                        await userManager.ResetAccessFailedCountAsync(appUser);
                        scUser.invalidpwcount = 0;

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

                        if (requiresTwoFactor)
                            return Results.LocalRedirect($"/Account/LoginWith2fa?ReturnUrl={Uri.EscapeDataString(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl)}&RememberMe=false");
                        return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
                    }
                }
            }

            if (!failureAlerted)
                await alerts.OnLoginFailedAsync(username, clientIp, lockedOut: false);   // บัญชีไม่มี/ปิดใช้ ก็นับต่อ IP
            var errorRedirect = "/login?error=1";
            if (!string.IsNullOrEmpty(returnUrl))
                errorRedirect += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
            return Results.LocalRedirect(errorRedirect);
        }).RequireRateLimiting("login");

        // Distinct from error=1 on purpose: "you are locked out for a while"
        // is actionable (wait, or call an admin), "wrong password" is not the
        // same problem. This does leak that the account exists — accepted,
        // because a lockout the user can't see is a support call every time,
        // and the rate limiter already caps enumeration attempts.
        static IResult LockedOut(string returnUrl)
        {
            var url = "/login?error=locked";
            if (!string.IsNullOrEmpty(returnUrl))
                url += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
            return Results.LocalRedirect(url);
        }

        app.MapGet("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.LocalRedirect("/");
        });
    }
}
