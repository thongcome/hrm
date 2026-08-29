using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// SAP OM-style headcount budget: ties Pos_PositionSlot creation to an
// approved count so HR can see (and optionally be blocked) when a new slot
// would exceed the plan. Scope narrows as fields are set: null Organization
// + null PosExecType = whole-company cap; Organization set = that org's
// subtree (matched via com_organization.orgcodefull, same convention as
// OrgEmployeeResolverHelper); PosExecType set = restricted to that job.
// "Used" is always the CURRENT live count of Pos_PositionSlot rows matching
// this scope with IsActive && IsManpower true — FiscalYear is a label for
// which year's approved plan this is, not a filter on slot creation date
// (there's no meaningful "headcount as of a past date" need yet — see the
// effective-dating discussion this budget feature came out of).
[Table("Pos_HeadcountBudget")]
public class Pos_HeadcountBudget
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? BudgetCode { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public int FiscalYear { get; set; }

    // null = whole company
    public long? OrganizationId { get; set; }

    // null = any job within the org scope above
    public long? PosExecTypeId { get; set; }

    public int ApprovedCount { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public long? CreatedByUserId { get; set; }
}
