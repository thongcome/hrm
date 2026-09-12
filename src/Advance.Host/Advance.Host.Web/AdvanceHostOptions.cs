namespace Advance.Host.Web;

/// <summary>How the active tenant is resolved for each request.</summary>
public enum TenantResolutionMode
{
    /// <summary>Read a claim (default name "tenant_id") off the signed-in user's ClaimsPrincipal.</summary>
    Claim,

    /// <summary>Read a fixed request header (default name "X-Tenant-Id") — mainly for API/service-to-service callers.</summary>
    Header,

    /// <summary>
    /// Every request resolves to the same fixed tenant — this is the mode a
    /// single-company on-premise install of Advance.Payroll or
    /// Advance.Workflow uses (see DESIGN-NOTES.md: "a tenant can have
    /// multiple companies" only matters for the multi-tenant SaaS
    /// deployment; an on-prem install is one tenant with N companies, or
    /// even just one).
    /// </summary>
    Fixed,
}

/// <summary>
/// Configuration for <see cref="ServiceCollectionExtensions.AddAdvanceHost"/>.
/// </summary>
public class AdvanceHostOptions
{
    /// <summary>Connection string for <c>HostDbContext</c> (the host shell's own small Tenant table).</summary>
    public required string ConnectionString { get; set; }

    public TenantResolutionMode TenantResolution { get; set; } = TenantResolutionMode.Claim;

    /// <summary>Claim type read when <see cref="TenantResolution"/> is <see cref="TenantResolutionMode.Claim"/>.</summary>
    public string TenantClaimType { get; set; } = "tenant_id";

    /// <summary>Header name read when <see cref="TenantResolution"/> is <see cref="TenantResolutionMode.Header"/>.</summary>
    public string TenantHeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>Tenant id used when <see cref="TenantResolution"/> is <see cref="TenantResolutionMode.Fixed"/>.</summary>
    public string? FixedTenantId { get; set; }
}
