namespace Advance.SecurityCore.Web;

using Advance.SecurityCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

// Mirrors AddAdvanceSecurityCore: the single middleware/endpoint wiring call
// a host's Program.cs makes, in the same relative pipeline position HRM
// uses today (see EXTRACTION-PLAN.md section 1 for HRM's exact line
// numbers this replaces):
//
//   app.UseAuthentication();
//   app.UseAuthorization();
//   app.UseAdvanceSecurityCore();   // <-- force-change-password + MFA gates + login endpoints
//   app.UseRateLimiter();
//   app.UseAntiforgery();
//   ... app.MapRazorComponents<App>() ...
//
// Order matters and is NOT enforced by this method — UseAuthentication/
// UseAuthorization must run first (ForcePasswordChangeMiddleware and
// RequireMfaMiddleware both read HttpContext.User), and UseRateLimiter must
// run before this if MapLoginEndpoints' RequireRateLimiting("login") is to
// take effect, since the "login" rate-limiter policy is registered in
// AddAdvanceSecurityCore but the UseRateLimiter() middleware call itself is
// still the host's responsibility (a host may have other rate-limited
// endpoints of its own to combine with).
public static class ApplicationBuilderExtensions
{
    public static WebApplication UseAdvanceSecurityCore(this WebApplication app)
    {
        app.UseMiddleware<ForcePasswordChangeMiddleware>();
        app.UseMiddleware<RequireMfaMiddleware>();

        app.MapLoginEndpoints();
        app.MapAdditionalIdentityEndpoints(); // from Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs

        // NOT mapped here — see ServiceCollectionExtensions.cs header:
        //   app.MapPasswordPolicyEndpoints();   // force-change-password-handler — not in this extraction
        //   app.MapForgotPasswordEndpoints();   // self-service reset — not in this extraction
        //   app.MapExternalSsoEndpoints();      // /auth/sso/{name}/login — not in this extraction

        return app;
    }

    // Startup seeding — HRM calls the equivalent of these on EVERY startup
    // (all environments), never touching existing rows. A host calls this
    // once after app.Build(), same spot HRM calls
    // ScProgramRouteSeeder/ProgramRoleService/ScMenuNavSeeder today.
    public static async Task SeedAdvanceSecurityCoreAsync(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<SecurityCoreOptions>();

        await ScProgramRouteSeeder.SeedAsync(app.Services, options.RouteAssemblies);
        await ProgramRoleService.SeedAsync(app.Services, options.RouteAssemblies);

        var contributors = app.Services.GetServices<IMenuNavContributor>();
        await ScMenuNavSeeder.SeedAsync(app.Services, contributors);
    }
}
