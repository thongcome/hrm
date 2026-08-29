using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Auto-written audit log of every occupant change on a Pos_PositionSlot —
// written by Services/Shared/EmployeePositionSync.cs's SyncAsync whenever it
// detects HremployeeId actually changed, so it covers every caller
// (PositionSlotAdmin.razor's direct edits, RecOfferService.ConfirmHireAsync,
// EmployeeRehireService) without each of them needing to write it
// themselves. This is an EVENT LOG, not a periodic full-row snapshot like
// the legacy com_organization_his table — only the fields that actually
// matter for "who left, who came in, when, why" are kept.
[Table("Pos_PositionSlot_his")]
public class Pos_PositionSlot_his
{
    [Key]
    public long Id { get; set; }

    public long PositionSlotId { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public DateTime ChangeDate { get; set; } = DateTime.Now;

    public long? PreviousHremployeeId { get; set; }
    [StringLength(6)]
    public string? PreviousEmpNo { get; set; }

    public long? NewHremployeeId { get; set; }
    [StringLength(6)]
    public string? NewEmpNo { get; set; }

    public PositionSlotChangeType ChangeType { get; set; }

    [StringLength(2000)]
    public string? Remark { get; set; }

    public long? ChangedByUserId { get; set; }

    public virtual Pos_PositionSlot PositionSlot { get; set; } = null!;
}
