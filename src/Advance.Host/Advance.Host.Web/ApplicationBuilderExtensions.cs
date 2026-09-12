using Advance.Host.Data;
using Advance.Host.Web.SecurityCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Advance.Host.Web;

/// <summary>
/// Middleware-pipeline counterpart to <see cref="ServiceCollectionExtensions.AddAdvanceHost"/>.
/// Call this once, early in a consuming Program.cs's pipeline — after
/// UseAuthentication/UseAuthorization (tenant resolution reads the signed-in
/// user's claims by default, same ordering constraint HRM's own
/// ForcePasswordChangeMiddleware documents for the same reason) and before
/// any endpoint that touches a tenant-scoped DbContext.
///
/// STUB, deliberately: today this only (1) applies pending HostDbContext
/// migrations in Development, matching HRM's own
/// app.UseMigrationsEndPoint()-in-Development-only pattern, and (2) reserves
/// the pipeline slot so future host-level middleware (tenant-not-found
/// handling, the eventual Excel-import upload endpoint, anything
/// Advance.SecurityCore needs called from here once its real
/// UseAdvanceSecurityCore exists) has one obvious place to be added instead
/// of every consuming product's own Program.cs growing its own copy.
/// </summary>
public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAdvanceHost(this IApplicationBuilder app, bool isDevelopment)
    {
        if (isDevelopment)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HostDbContext>();
            db.Database.Migrate();
        }

        var securityCore = app.ApplicationServices.GetService<IAdvanceSecurityCoreMarker>();
        if (securityCore is { IsRegistered: false })
        {
            var logger = app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger("Advance.Host");
            logger?.LogWarning(
                "Advance.Host: no real Advance.SecurityCore registration found (IAdvanceSecurityCoreMarker.IsRegistered is false). " +
                "Login/menu/AD.CRUDManage rights are NOT wired yet — see DESIGN-NOTES.md 'Reconciling with Advance.SecurityCore'.");
        }

        return app;
    }
}
