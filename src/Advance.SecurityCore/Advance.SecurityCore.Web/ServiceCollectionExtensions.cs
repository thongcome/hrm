namespace Advance.SecurityCore.Web;

using Advance.SecurityCore.Data;
using Advance.SecurityCore.Domain;
using Advance.SecurityCore.Services;
using Advance.SecurityCore.Web.Components.Account;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// The single DI entry point a host's Program.cs calls, replacing the ~40+
// lines of Identity/auth/DI wiring currently scattered through HRM's
// Program.cs for this concern (see EXTRACTION-PLAN.md section 1 for the
// exact line ranges this consolidates). This is written as real,
// structurally-complete code per the task brief, even though it will not
// compile as a drop-in replacement for HRM's Program.cs today — HRM's
// version does several things this package deliberately does NOT own yet
// (OpenIddict SSO, the ExternalApi JWT scheme, AD/OIDC SSO provider loop,
// rate limiting for career-apply/forgot-password, MudBlazor/localization
// services, every business-module DbSet). Those stay in HRM's Program.cs;
// this method only owns the SecurityCore slice. See EXTRACTION-PLAN.md's
// "what stays HRM-specific forever" list.
//
// NOT included here (flagged, not silently dropped):
//   - PasswordPolicyEndpoints / ForgotPasswordEndpoints / ExternalSsoEndpoints
//     mapping — HRM has these as separate Endpoints/*.cs files that were not
//     in this extraction's requested copy list. A host still needs to map
//     its own equivalents (or a later SecurityCore version adds them) for
//     the force-change-password and forgot-password FORMS' POST handlers to
//     work — the middleware/claim/service plumbing for them is here, the
//     minimal-API endpoints are not.
//   - AuditLogger's write-side automatic hook (HRMContext.Audit.cs's
//     SaveChangesAsync override) — SecurityDbContext does not have it; see
//     AuditLogger.cs's header.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdvanceSecurityCore(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SecurityCoreOptions> configureOptions)
    {
        var options = new SecurityCoreOptions();
        configureOptions(options);
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("SecurityCoreOptions.ConnectionString is required.");

        Components.Account.Shared.HostLayout.LayoutType = options.HostLayoutType;

        // ---- Data ----------------------------------------------------
        services.AddDbContextFactory<SecurityDbContext>(o => o.UseSqlServer(options.ConnectionString));
        // OpenIddict/other libraries that resolve a DbContext directly via
        // DI (not through IDbContextFactory<T>) need this scoped shim —
        // same reasoning as HRM's Program.cs comment on the equivalent
        // ApplicationDbContext registration (see EXTRACTION-PLAN.md).
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<SecurityDbContext>>().CreateDbContext());

        // ---- Password policy + security alerts ------------------------
        services.Configure<PasswordPolicyOptions>(configuration.GetSection(PasswordPolicyOptions.SectionName));
        services.AddSingleton<PasswordPolicyService>();
        services.Configure<SecurityAlertOptions>(configuration.GetSection(SecurityAlertOptions.SectionName));
        services.AddSingleton<SecurityAlertCounter>();
        services.AddSingleton<SecurityAlertService>();

        var passwordPolicy = configuration.GetSection(PasswordPolicyOptions.SectionName).Get<PasswordPolicyOptions>() ?? new();

        // ---- Identity core ---------------------------------------------
        var authBuilder = services.AddAuthentication(o =>
        {
            o.DefaultScheme = IdentityConstants.ApplicationScheme;
            o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        });
        authBuilder.AddIdentityCookies();

        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.SignIn.RequireConfirmedAccount = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireUppercase = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireDigit = true;
                o.Password.RequireNonAlphanumeric = true;
                o.Lockout.MaxFailedAccessAttempts = passwordPolicy.MaxFailedAttempts;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(passwordPolicy.LockoutMinutes);
                o.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<SecurityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<ScUserClaimsPrincipalFactory>();

        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(1));

        services.ConfigureApplicationCookie(o =>
        {
            o.LoginPath = "/login";
            o.LogoutPath = "/logout";
            o.ExpireTimeSpan = TimeSpan.FromMinutes(60);
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.SameSite = SameSiteMode.Strict;
        });

        // Default no-op host-claims enricher — a host overrides this with
        // its own registration AFTER calling AddAdvanceSecurityCore (the
        // last registration of a scoped/singleton interface wins when
        // resolved directly; use TryAddScoped in the host if registration
        // ORDER can't be guaranteed instead).
        services.TryAddScoped<IHostClaimsEnricher, NullHostClaimsEnricher>();

        // Several Components/Account pages (Manage/Email.razor,
        // Register.razor, ExternalLogin.razor) inject
        // IEmailSender<ApplicationUser> for confirmation-link emails —
        // distinct from the plain IEmailSender other pages use.
        // IdentityNoOpEmailSender (copied from HRM's Components/Account/
        // IdentityNoOpEmailSender.cs) is registered as the default so the
        // package "just works" out of the box; a host with real email
        // wires its own IEmailSender<ApplicationUser> to replace it.
        services.TryAddScoped<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

        // ---- AD.CRUDManage (ProgramRoleService) + menu authorization ---
        services.AddMemoryCache();
        services.AddScoped<ProgramRoleService>();
        services.AddSingleton<IAuthorizationPolicyProvider, MenuPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, MenuAuthorizationHandler>();
        services.AddAuthorizationCore();

        // ---- Audit ------------------------------------------------------
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditLogger, AuditLogger>();

        // ---- Login (AD/LDAP bind) ---------------------------------------
        services.AddScoped<LdapAuthService>();

        // ---- Rate limiting for /login-handler ---------------------------
        services.AddRateLimiter(o =>
        {
            o.AddSlidingWindowLimiter("login", l =>
            {
                l.PermitLimit = 5;
                l.Window = TimeSpan.FromMinutes(1);
                l.SegmentsPerWindow = 4;
                l.QueueLimit = 0;
            });
            o.OnRejected = async (ctx, ct) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.HttpContext.Response.WriteAsync("Too many attempts — please wait a moment and try again.", ct);
            };
        });

        // Stashed for UseAdvanceSecurityCore's startup seeding calls, which
        // need the same route assemblies and can't easily receive them
        // again at UseX time without the host repeating itself.
        services.AddSingleton(options);

        return services;
    }
}
