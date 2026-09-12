namespace Advance.Host.Domain;

/// <summary>
/// A SaaS subscriber/customer account in a standalone Advance.Payroll or
/// Advance.Workflow deployment. Deliberately small — this is NOT a
/// re-implementation of Advance.SecurityCore's user/role/login tables, and
/// NOT a company/legal-entity master (that stays the consuming product's own
/// concern, e.g. Advance.Payroll's pay_employee.CompanyId). This table exists
/// only to answer "which subscriber does this row belong to" for the global
/// query filter described in HostDbContext.
/// </summary>
public class Tenant
{
    /// <summary>
    /// Stable external id used as the TenantId value stamped on every
    /// ITenantScoped row elsewhere in the system — a string (not an identity
    /// int) so it can be a short human-readable code (matching the house
    /// style already used for Company codes) and so it never needs to be
    /// looked up before it can be used as a foreign key value from another
    /// database/module.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>Short code shown in the product UI (e.g. subdomain segment).</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
