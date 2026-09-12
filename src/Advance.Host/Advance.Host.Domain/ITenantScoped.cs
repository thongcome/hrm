namespace Advance.Host.Domain;

/// <summary>
/// Marks an entity as belonging to exactly one <see cref="Tenant"/>. This is
/// the multi-tenancy axis for the standalone Advance.Payroll / Advance.Workflow
/// products — it is deliberately separate from (and sits ABOVE) HRM's existing
/// string CompanyId convention. See DESIGN-NOTES.md "TenantId vs CompanyId"
/// for the full reasoning; short version: one Tenant (a SaaS subscriber) can
/// own several Companies, and every company-scoped table keeps its own
/// CompanyId exactly as HRM does today — TenantId is an additional, outer
/// column added only on tables that are new to the standalone products
/// (e.g. pay_employee), not a replacement for CompanyId.
///
/// A consuming DbContext (Advance.Payroll.Data's PayrollDbContext, say) marks
/// its own entities with this interface and applies one shared global query
/// filter for all of them — see Advance.Host.Data.HostDbContext's header
/// comment for exactly how.
/// </summary>
public interface ITenantScoped
{
    string TenantId { get; set; }
}

/// <summary>
/// Ambient "who is asking" service for tenant scoping — resolved once per
/// request/circuit (HTTP header, claim, or subdomain, depending on how the
/// consuming Advance.*.Web host authenticates) and injected wherever a
/// DbContext needs to filter or stamp TenantId. Deliberately a tiny
/// interface with no dependency on HttpContext/ClaimsPrincipal/etc. so it
/// stays in Domain (zero references) — a concrete implementation
/// (HttpContextCurrentTenant, say) lives in the Web layer and is registered
/// as Scoped in AddAdvanceHost.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>
    /// The active tenant for the current request/circuit. Null before it has
    /// been resolved (e.g. during the SaveChanges call that provisions the
    /// very first tenant) — callers that require a tenant should throw on
    /// null rather than silently scoping to "no tenant".
    /// </summary>
    string? TenantId { get; }
}
