namespace HRM.Middleware;

// The modern stand-in for what doLogin.jsp did in one line: refuse to hand a
// user the application until they've replaced a password they didn't choose
// (or one that has expired).
//
//     if( secure.isForceChanged() ) response.sendRedirect(".../PID=SCN01074");
//
// The JSP version simply never put the SecurityBean into the session, so an
// unchanged password meant "not logged in at all". That doesn't translate to
// a cookie-auth Blazor app: the change-password form itself has to know who
// is asking, and the safest way to prove that is an authenticated request.
// So here the user IS signed in, but carries a "pwd_change_required" claim
// (stamped by ScUserClaimsPrincipalFactory), and this middleware keeps them
// pinned to the change-password page until it's gone. The claim disappears on
// the RefreshSignInAsync that PasswordPolicyEndpoints issues after a
// successful change.
//
// Sits in the pipeline right after UseAuthentication/UseAuthorization so
// httpContext.User is populated, and before endpoint execution so no page,
// endpoint or file download runs for a user in this state.
public sealed class ForcePasswordChangeMiddleware
{
    public const string ClaimType = "pwd_change_required";
    public const string ChangePath = "/force-change-password";
    public const string ChangeHandlerPath = "/force-change-password-handler";

    private readonly RequestDelegate _next;
    public ForcePasswordChangeMiddleware(RequestDelegate next) => _next = next;

    // Everything that must keep working while a user is pinned: the form and
    // its handler, the way out (logout/login), and the plumbing every Blazor
    // page needs to render at all. Blazor Server's circuit lives on /_blazor —
    // blocking it would leave the change-password page itself inert.
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
            // 302 to the form rather than 403: this is a "finish setting up
            // your account" state, not an authorization failure, and the user
            // has no other way to resolve it. The claim value ("forced" or
            // "expired") rides along so the page can say which it is.
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

        // Static assets (css/js/fonts/images) — a redirect here would only
        // produce a broken-looking form. Anything with a file extension in its
        // last segment is treated as an asset; app routes in this codebase
        // never carry one.
        var lastSlash = value.LastIndexOf('/');
        return lastSlash >= 0 && value.AsSpan(lastSlash).Contains('.');
    }
}
