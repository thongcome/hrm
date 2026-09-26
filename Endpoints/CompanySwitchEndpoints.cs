namespace HRM.Endpoints;

using System.Security.Claims;
using HRM.Data;
using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Switches which company's data the signed-in user sees (the "payroll_company"
// claim), without touching the password/2FA sign-in flow and without a full
// re-login. Kept as a real form POST endpoint rather than a Blazor event
// handler for the same reason as /login-handler and
// /force-change-password-handler: rewriting the auth cookie needs a live HTTP
// response, which a SignalR circuit doesn't have.
//
// Deliberately does NOT call SignInManager.RefreshSignInAsync — that re-runs
// ScUserClaimsPrincipalFactory.CreateAsync, which would recompute
// "payroll_company" from the user's own Hremployee row and silently undo the
// switch (and mint a fresh sc_user_session row, which a mere company switch
// shouldn't do either). Instead it clones the CURRENT principal's claims and
// replaces just the one claim, so every other claim (menu, role, sc_userid,
// sessionid, scope_*) — and every one of the ~155 existing call sites that
// reads "payroll_company" — keeps working completely unchanged.
//
// No antiforgery check here (unlike /login-handler) — the auth cookie is
// SameSite=Strict (Program.cs, ConfigureApplicationCookie), so it is never
// attached to a request a cross-site page originates, GET or POST. A
// forged cross-site POST to this endpoint arrives with no valid session at
// all and falls straight into the "not authenticated" branch below — the
// CSRF an antiforgery token would guard against is already closed at the
// cookie level. See CompanySwitcher.razor's header comment for the other
// half of why: that token was also unreliable to obtain from this form's
// persistent-layout home.
public static class CompanySwitchEndpoints
{
    public static void MapCompanySwitchEndpoints(this WebApplication app)
    {
        app.MapPost("/switch-company-handler", async (
            HttpContext httpContext,
            IDbContextFactory<HRMContext> dbFactory,
            CompanySwitchService switchService,
            IAuditLogger auditLogger) =>
        {
            var form = await httpContext.Request.ReadFormAsync();
            var returnUrl = SafeReturnUrl(form["returnUrl"].ToString());
            var targetCode = form["companyCode"].ToString();

            var authenticateResult = await httpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            var currentPrincipal = authenticateResult.Principal;
            if (authenticateResult.Succeeded == false || currentPrincipal is null)
                return Results.LocalRedirect("/login");

            await using var context = await dbFactory.CreateDbContextAsync();

            // Never trust the posted code — re-check server-side against the
            // same sc_role_scope-backed permission gate the switcher UI used
            // to build its option list (fail-closed on anything unexpected).
            if (!await switchService.IsSwitchAllowedAsync(currentPrincipal, targetCode, context))
                return Results.LocalRedirect(returnUrl + "?switcherror=1");

            var oldCode = currentPrincipal.FindFirst("payroll_company")?.Value;
            if (oldCode == targetCode)
                return Results.LocalRedirect(returnUrl);

            var currentIdentity = (ClaimsIdentity)currentPrincipal.Identity!;
            var newIdentity = new ClaimsIdentity(
                currentIdentity.Claims.Where(c => c.Type != "payroll_company"),
                currentIdentity.AuthenticationType);
            newIdentity.AddClaim(new Claim("payroll_company", targetCode));
            // cookie ที่ออกก่อนมี home_company: จำบริษัทที่ใช้อยู่ตอนนี้เป็นบ้านไว้ก่อนสลับออก
            if (oldCode is not null && !newIdentity.HasClaim(c => c.Type == CompanySwitchService.HomeCompanyClaim))
                newIdentity.AddClaim(new Claim(CompanySwitchService.HomeCompanyClaim, oldCode));
            var newPrincipal = new ClaimsPrincipal(newIdentity);

            await httpContext.SignInAsync(IdentityConstants.ApplicationScheme, newPrincipal, authenticateResult.Properties);

            var actorUserId = currentPrincipal.FindFirst("sc_userid")?.Value;
            await auditLogger.LogAccessAsync("CompanySwitch", actorUserId, isSensitive: false,
                note: $"{oldCode ?? "(none)"} -> {targetCode}");

            return Results.LocalRedirect(returnUrl);
        });
    }

    // returnUrl comes from a hidden form field a user's own browser posts —
    // LocalRedirect already refuses an absolute/external URL, this just
    // gives a sane default when the field is missing or empty.
    private static string SafeReturnUrl(string? returnUrl) =>
        string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
}
