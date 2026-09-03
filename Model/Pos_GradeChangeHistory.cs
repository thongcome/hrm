using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Audit trail of an employee's job-grade change (promotion / demotion). Grade is
// per-person and lives on Hremployee.EMPLEVEL_CODE (→ pos_position_level); this
// records every move of it with old/new grade + level, reason and actor, so a
// promotion is never a silent field edit. Owner rule (2026-09-03): grade drives
// promotion — higher grade is promoted first.
[Table("Pos_GradeChangeHistory")]
[Index(nameof(HremployeeId))]
[Index(nameof(CompanyId))]
public class Pos_GradeChangeHistory
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(20)]
    public string? EmpNo { get; set; }

    [StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [StringLength(2)]
    public string? OldGradeCode { get; set; }
    [StringLength(2)]
    public string? NewGradeCode { get; set; }

    // Decimal so a half-rung (7.5) is recorded exactly.
    [Column(TypeName = "decimal(5,2)")]
    public decimal? OldPlevel { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal? NewPlevel { get; set; }

    public bool IsPromotion { get; set; } // NewPlevel > OldPlevel

    [StringLength(1000)]
    public string? Reason { get; set; }

    public DateTime EffectiveDate { get; set; }

    public long ChangedByUserId { get; set; }
    public DateTime ChangedDate { get; set; } = DateTime.Now;
}
