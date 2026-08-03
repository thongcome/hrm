using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ชื่อตำแหน่ง (position-title master) — ported from legacy PIS "pos_exec_type"
// table (Position/Detail/PositionCreate.jsp under personal/, and the
// "ข้อมูลชื่อตำแหน่ง" JSP form). This is the reusable title (e.g. "CEO",
// "ผู้จัดการฝ่ายบุคคล") that individual Pos_PositionSlot rows point at —
// one title can be used by many headcount slots across different org units.
[Table("Pos_ExecType")]
public class Pos_ExecType
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public long? EmployeeTypeId { get; set; }

    [StringLength(20)]
    public string? Code { get; set; }

    [Required, StringLength(500)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? AbbName { get; set; }

    [StringLength(500)]
    public string? NameEn { get; set; }

    [StringLength(200)]
    public string? AbbNameEn { get; set; }

    public bool IsTopLevel { get; set; }

    public bool IsBoss { get; set; }

    public bool IsActive { get; set; } = true;

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    [StringLength(1000)]
    public string? Remark { get; set; }

    public DateTime? CreateDate { get; set; }
    [StringLength(50)]
    public string? CreateBy { get; set; }
}
