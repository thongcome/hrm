using Advance.Host.Data;
using Advance.Host.Domain;
using Advance.Host.Web.SecurityCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Advance.Host.Web;

/// <summary>
/// "Day one" DI wiring for a standalone Advance.Payroll.Web or
/// Advance.Workflow.Web Program.cs — see Plan_Split_Payroll_Workflow_v1.1.md
/// section 3.2. Registers: HostDbContext (Tenant table), tenant resolution
/// (<see cref="ICurrentTenant"/>) + auto-stamping
/// (<see cref="TenantSaveChangesInterceptor"/>), and the Excel import helper.
/// Menu/AD.CRUDManage rights and Identity/login themselves are
/// Advance.SecurityCore's responsibility, not this project's — see
/// <see cref="IAdvanceSecurityCoreMarker"/> for why that composition is
/// currently a placeholder.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdvanceHost(this IServiceCollection services, Action<AdvanceHostOptions> configure)
    {
        var options = new AdvanceHostOptions { ConnectionString = string.Empty };
        configure(options);
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException($"{nameof(AdvanceHostOptions)}.{nameof(AdvanceHostOptions.ConnectionString)} must be set.");

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenant, HttpContextCurrentTenant>();
        services.AddScoped<TenantSaveChangesInterceptor>();

        services.AddDbContext<HostDbContext>((sp, dbOptions) =>
        {
            dbOptions.UseSqlServer(options.ConnectionString);
            // Consuming products' own DbContext (PayrollDbContext,
            // WorkflowDbContext) should add the same interceptor the same
            // way, resolving it from DI rather than `new`-ing it, so it
            // shares the request-scoped ICurrentTenant.
            dbOptions.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());
        });

        // Placeholder registration — see IAdvanceSecurityCoreMarker's header
        // comment. TryAddScoped so a consumer that HAS already called a real
        // AddAdvanceSecurityCore(...) (once that project exists) keeps its
        // own registration; this is only a safety net so DI resolution never
        // fails outright during Phase 0.
        services.TryAddScoped<IAdvanceSecurityCoreMarker, NullAdvanceSecurityCoreMarker>();

        return services;
    }
}
