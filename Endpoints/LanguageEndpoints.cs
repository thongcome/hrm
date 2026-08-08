namespace HRM.Endpoints;

// Sets the UI-language cookie that JsonLocalizationService reads at circuit
// start. Done as a real HTTP endpoint (not a Blazor event handler) because
// Blazor Server can't reliably write a Set-Cookie header from within a live
// SignalR circuit — same reasoning as /login-handler in LoginEndpoints.cs.
public static class LanguageEndpoints
{
    public static void MapLanguageEndpoints(this WebApplication app)
    {
        app.MapGet("/set-language", (HttpContext httpContext, string? lang, string? returnUrl) =>
        {
            var culture = lang == "en" ? "en" : "th";

            httpContext.Response.Cookies.Append(
                JsonLocalizationService.CookieName,
                culture,
                new CookieOptions
                {
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    SameSite = SameSiteMode.Lax,
                });

            return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        });
    }
}
