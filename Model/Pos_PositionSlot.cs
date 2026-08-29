using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// เลขที่อัตรา (headcount / position slot) — ported from legacy PIS
// "positionlist" table. Each row is one concrete, countable headcount slot
// (a "seat") — distinct from Pos_ExecType (the reusable title text). A slot
// belongs to one org unit, uses one title, and may currently be occupied by
// one employee (HremployeeId null = ว่าง/vacant). This is what the legacy
// "แสดงอัตรา" headcount tree counts and what "จำนวนพนักงานแยกตามตำแหน่ง" reports from.
[Table("Pos_PositionSlot")]
public class Pos_PositionSlot
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    // Display number shown to users (legacy "pos_code") — not the primary
    // key, since the legacy system itself separates a surrogate PK (posid)
    // from this human-facing running number.
    [StringLength(20)]
    public string? PosCode { get; set; }

    public long? PosExecTypeId { get; set; }

    public long? OrganizationId { get; set; }

    public long? EmployeeTypeId { get; set; }

    // Denormalized snapshot of the title/org names at slot-creation time —
    // mirrors the legacy table's own denormalization (name/abbname columns
    // alongside the FK codes), kept here for the same reason: fast list
    // rendering without a join, and stability if the title/org is renamed later.
    [StringLength(500)]
    public string? Name { get; set; }
    [StringLength(200)]
    public string? AbbName { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? ExpireDate { get; set; }

    [StringLength(200)]
    public string? MinGraduate { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? MinSalary { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal? NormalSalary { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal? MaxSalary { get; set; }

    public bool IsManpower { get; set; } = true;
    public bool IsBoss { get; set; }
    public bool IsActive { get; set; } = true;

    // Who currently occupies this slot — null means vacant (ว่าง), which the
    // legacy headcount tree displays explicitly rather than hiding the slot.
    public long? HremployeeId { get; set; }

    // Denormalized snapshot of the occupant's EmpNo — same reasoning as
    // Name/AbbName above (fast list rendering without a join), kept in sync
    // by Services/Shared/EmployeePositionSync.cs whenever HremployeeId
    // changes. Null when the slot is vacant.
    [StringLength(6)]
    public string? EmpNo { get; set; }

    [StringLength(50)]
    public string? PaymentNo { get; set; }

    [StringLength(2000)]
    public string? Remark { get; set; }

    public DateTime? CreateDate { get; set; }
    [StringLength(50)]
    public string? CreateBy { get; set; }
}
