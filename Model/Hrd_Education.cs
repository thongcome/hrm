using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประวัติการศึกษา — ported from legacy PIS "educationlevel"/"edu_level" tables
// (personal/edu/PersonEduCreate.jsp). Multi-row per employee.
[Table("Hrd_Education")]
public class Hrd_Education
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(200)]
    public string? Level { get; set; }
    [StringLength(200)]
    public string? Degree { get; set; }
    [StringLength(200)]
    public string? Major { get; set; }
    [StringLength(200)]
    public string? MajorSubject { get; set; }
    [StringLength(200)]
    public string? Faculty { get; set; }

    [StringLength(300)]
    public string? Institute { get; set; }
    [StringLength(100)]
    public string? Country { get; set; }

    public DateOnly? EntryDate { get; set; }
    public DateOnly? FinishedDate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Gpa { get; set; }

    public bool IsHonors { get; set; }

    // ระดับสูงสุด — flags the highest-degree row when an employee has
    // multiple education records, matching legacy "ismaxdegreeid".
    public bool IsHighestDegree { get; set; }

    [StringLength(1000)]
    public string? Remark { get; set; }

    public DateTime? ModDate { get; set; }
    [StringLength(50)]
    public string? ModBy { get; set; }
}
