namespace Advance.SecurityCore.Web;

using System.Reflection;

// Options a host passes to AddAdvanceSecurityCore(...). Collects the things
// that necessarily differ per host app (which assembly has the @page routes
// to scan for AD.CRUDManage, which connection string to use) so the
// extension method itself stays a single call in the host's Program.cs
// instead of the ~40+ lines this replaces in HRM today (see
// EXTRACTION-PLAN.md's Program.cs line-range inventory).
public sealed class SecurityCoreOptions
{
    // Required. The connection string SecurityDbContext uses. In HRM this
    // would be the SAME "DefaultConnection" HRMContext/ApplicationDbContext
    // already use (shared database during the staged migration — see
    // EXTRACTION-PLAN.md's migration order) — NOT a second database.
    public string ConnectionString { get; set; } = null!;

    // Required. Assemblies to scan for @page-routed Razor components, for
    // ProgramRoleService/ScProgramRouteSeeder's route-based AD.CRUDManage
    // seeding. In HRM this was hardcoded to typeof(HRM.Components.App).Assembly;
    // here the host supplies it (usually just its own top-level assembly)
    // — see ProgramRoleService.cs's header.
    public List<Assembly> RouteAssemblies { get; } = new();

    // The top-level layout component AccountLayout.razor should render
    // inside — see Components/Account/Shared/HostLayout.cs. Left null =
    // Identity pages render with no outer app chrome (usable, just
    // unstyled) until a host sets this.
    public Type? HostLayoutType { get; set; }

    // Password/lockout policy — same shape as HRM's appsettings
    // "PasswordPolicy" section; bind from config or set directly.
    public Action<PasswordPolicyOptionsBuilder>? ConfigurePasswordPolicy { get; set; }
}

// Thin builder so a host can either bind from IConfiguration (the normal
// path, via services.Configure<PasswordPolicyOptions>(configuration.GetSection(...)))
// or set values inline for a quick standalone/test host — kept here only as
// a documented option, not required.
public sealed class PasswordPolicyOptionsBuilder
{
    public int MaxAgeDays { get; set; }
    public int ExpiryWarningDays { get; set; } = 7;
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}
