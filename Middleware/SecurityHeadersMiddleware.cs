namespace HRM.Middleware;

// OWASP A05 (Security Misconfiguration) — the Advance baseline requires all
// five response headers on every response. Added 1 ก.ย. 2569 during the
// pre-demo security review; HRM does not reference Advance.Platform (SQL
// Server / different stack) so the headers are set here directly.
//
// CSP is scoped to what App.razor actually loads: everything is same-origin
// ('self') EXCEPT Google Fonts (googleapis stylesheet + gstatic font files).
//   - style-src needs 'unsafe-inline': both Blazor and MudBlazor inject
//     inline <style>/style="" at runtime; without it the whole UI loses its
//     styling. (Blazor Server does NOT need 'unsafe-eval' for scripts.)
//   - connect-src 'self' covers the SignalR websocket (same origin).
//   - frame-ancestors 'none' is the modern equivalent of
//     X-Frame-Options: DENY (both sent for older-browser coverage).
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    private const string Csp =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "object-src 'none'";

    public async Task InvokeAsync(HttpContext context)
    {
        var h = context.Response.Headers;
        // Only set if absent so a more specific per-response policy can win.
        if (!h.ContainsKey("Content-Security-Policy")) h.Append("Content-Security-Policy", Csp);
        if (!h.ContainsKey("X-Frame-Options")) h.Append("X-Frame-Options", "DENY");
        if (!h.ContainsKey("X-Content-Type-Options")) h.Append("X-Content-Type-Options", "nosniff");
        if (!h.ContainsKey("Referrer-Policy")) h.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        if (!h.ContainsKey("Permissions-Policy")) h.Append("Permissions-Policy", "geolocation=(self), camera=(), microphone=(), payment=()");
        await _next(context);
    }
}
