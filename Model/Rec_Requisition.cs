using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A request to open headcount — either Replacement (fills an existing
// vacant Pos_PositionSlot) or NewHeadcount (a slot is auto-created on
// approval, see RecRequisitionService.SyncStatusFromJobAsync). All FKs here
// are soft links, matching Pos_PositionSlot/Job_CompetencyRequirement's own
// convention (no [ForeignKey]/nav property).
[Table("Rec_Requisition")]
public class Rec_Requisition
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? RequisitionCode { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public RequisitionType RequisitionType { get; set; }

    // Set only for Replacement — the existing vacant slot this requisition
    // will fill once approved.
    public long? TargetPositionSlotId { get; set; }

    public long PosExecTypeId { get; set; }

    public long OrganizationId { get; set; }

    public int OpeningsCount { get; set; } = 1;

    [StringLength(1000)]
    public string? Justification { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? ProposedMinSalary { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal? ProposedMaxSalary { get; set; }

    public long RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;

    public DateOnly? TargetStartDate { get; set; }

    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;

    public long? JobMasterId { get; set; }

    // Filled in automatically after approval, only for NewHeadcount — see
    // RecRequisitionService.SyncStatusFromJobAsync.
    public long? NewPositionSlotId { get; set; }
}
