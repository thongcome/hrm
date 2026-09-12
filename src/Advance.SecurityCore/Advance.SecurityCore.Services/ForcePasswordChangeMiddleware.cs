namespace Advance.SecurityCore.Services;

using Microsoft.AspNetCore.Http;

// Copied verbatim (logic-wise) from HRM's Middleware/ForcePasswordChangeMiddleware.cs
// — already fully generic, no HRM-specific paths beyond the standard Identity
// scaffold routes every host built on this package will also have.
public sealed class ForcePasswordChangeMiddleware
{
    public const string ClaimType = ScUserClaimsPrincipalFactory.ForcePasswordChangeClaimType;
    public const string ChangePath = "/force-change-password";
    public const string ChangeHandlerPath = "/force-change-password-handler";

    private readonly RequestDelegate _next;
    public ForcePasswordChangeMiddleware(RequestDelegate next) => _next = next;

    private static readonly string[] AlwaysAllowed =
    {
        ChangePath,
        ChangeHandlerPath,
        "/logout",
        "/login",
        "/Account/Logout",
        "/Account/Login",
        "/_blazor",
        "/_framework",
        "/_content",
        "/Error",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var reason = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(ClaimType)?.Value
            : null;

        if (reason is not null && !IsAllowed(context.Request.Path))
        {
            context.Response.Redirect($"{ChangePath}?reason={Uri.EscapeDataString(reason)}");
            return;
        }

        await _next(context);
    }

    private static bool IsAllowed(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value)) return false;

        foreach (var allowed in AlwaysAllowed)
        {
            if (value.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) return true;
        }

        var lastSlash = value.LastIndexOf('/');
        return lastSlash >= 0 && value.AsSpan(lastSlash).Contains('.');
    }
}
