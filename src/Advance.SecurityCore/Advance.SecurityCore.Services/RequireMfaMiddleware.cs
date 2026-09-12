namespace Advance.SecurityCore.Services;

using Advance.SecurityCore.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

// Copied from HRM's Middleware/RequireMfaMiddleware.cs. The gating menu code
// ("SYS_ADMIN") is left hardcoded exactly as HRM has it — see
// SecurityAlertService.cs's header for why, and EXTRACTION-PLAN.md for the
// suggestion to make it configurable in a later version.
public sealed class RequireMfaMiddleware(RequestDelegate next, IOptionsMonitor<SecurityAlertOptions> options, IMemoryCache cache)
{
    public const string SetupPath = "/Account/Manage/EnableAuthenticator";
    public const string AdminGateMenuCode = "SYS_ADMIN";

    private static readonly string[] AlwaysAllowed =
    {
        "/Account/", "/logout", "/login", "/_blazor", "/_framework", "/_content", "/Error", "/force-change-password",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (options.CurrentValue.RequireMfaForAdmin
            && context.User?.Identity?.IsAuthenticated == true
            && context.User.HasClaim("menu", AdminGateMenuCode)
            && !IsAllowed(context.Request.Path))
        {
            var userId = context.User.FindFirst("sc_userid")?.Value ?? context.User.Identity.Name ?? "";
            var key = $"mfa:enabled:{userId}";
            if (!cache.TryGetValue(key, out bool enabled))
            {
                var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.GetUserAsync(context.User);
                enabled = user is not null && await userManager.GetTwoFactorEnabledAsync(user);
                cache.Set(key, enabled, TimeSpan.FromSeconds(enabled ? 300 : 20));
            }
            if (!enabled)
            {
                context.Response.Redirect($"{SetupPath}?reason=admin");
                return;
            }
        }
        await next(context);
    }

    private static bool IsAllowed(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value)) return false;
        foreach (var allowed in AlwaysAllowed)
            if (value.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) return true;
        var last = value.LastIndexOf('/');
        return last >= 0 && value.IndexOf('.', last) > last;
    }
}
