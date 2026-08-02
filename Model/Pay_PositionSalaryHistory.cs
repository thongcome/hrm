using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Modeled after the legacy JSP-era [positionhistory] table (old/new position
// code+name, ChangeType, OrderNo/OrderDate, CreateBy/CreateDate) — per user
// decision to reuse that shape rather than a salary-only table, since in
// this system a position change and a salary adjustment are usually the
// same HR event (a promotion/regrade order that also changes pay). Two
// things the legacy table didn't have, added here because the feature can't
// work without them:
//   - HremployeeId/EmpNo: the legacy table had NO employee reference column
//     at all (not droppable — every row must belong to exactly one person).
//   - OldSalary/NewSalary: the whole point of "ประวัติปรับเงินเดือน".
// The legacy `pos_pos_code2` column's meaning was undocumented in the DDL
// handed over and dropped rather than guessed at blindly.
//
// One write path today: PayrollEmployeeAdmin.razor's edit-save flow, when it
// detects Hremployee.SalaryAmt and/or PosCode actually changed from the
// value the form was loaded with.
[Table("Pay_PositionSalaryHistory")]
public class Pay_PositionSalaryHistory
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(6)]
    public string? EmpNo { get; set; }
    [StringLength(6)]
    public string? CompanyId { get; set; }

    [StringLength(15)]
    public string? OldPositionCode { get; set; }
    [StringLength(200)]
    public string? OldPositionName { get; set; }
    [StringLength(15)]
    public string? NewPositionCode { get; set; }
    [StringLength(200)]
    public string? NewPositionName { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? OldSalary { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal? NewSalary { get; set; }

    // Single-char category code, same shape as the legacy `changetype` — no
    // documented mapping came with the legacy DDL, so this is a fresh set of
    // values defined for this system: P=เลื่อนตำแหน่ง(Promotion),
    // D=ลดตำแหน่ง(Demotion), T=โยกย้าย(Transfer), S=ปรับเงินเดือนอย่างเดียว(Salary only), O=อื่นๆ(Other)
    [StringLength(1)]
    public string? ChangeType { get; set; }

    [StringLength(20)]
    public string? OrderNo { get; set; }
    public DateTime? OrderDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public long? ChangedByUserId { get; set; }
    public DateTime ChangedDate { get; set; } = DateTime.Now;

    public virtual Hremployee Hremployee { get; set; } = null!;
}
