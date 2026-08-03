using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประวัติการสมรส — ported from legacy PIS "married"/"marry" tables
// (personal/married/PersonMarryCreate.jsp). Kept separate from Hrd_Family
// (legacy also splits these): this is the marriage registration event
// itself, not a family-member row.
[Table("Hrd_Marriage")]
public class Hrd_Marriage
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(50)]
    public string? MarriageNo { get; set; }
    [StringLength(200)]
    public string? MarriedAt { get; set; }
    public DateOnly? RegisterDate { get; set; }
    [StringLength(50)]
    public string? BookNo { get; set; }

    [StringLength(300)]
    public string? SpouseName { get; set; }
    public int? SpouseAge { get; set; }
    [StringLength(200)]
    public string? SpouseCareer { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal? SpouseIncome { get; set; }

    public int? ChildCount { get; set; }
    public int? ChildStudyingCount { get; set; }
    public int? ChildNotStudyingCount { get; set; }

    [StringLength(1000)]
    public string? Remark { get; set; }

    public DateTime? ModDate { get; set; }
    [StringLength(50)]
    public string? ModBy { get; set; }
}
