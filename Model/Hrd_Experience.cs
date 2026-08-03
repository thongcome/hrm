using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประสบการณ์การทำงานก่อนเข้าทำงาน — ported from legacy PIS "experience" table
// (personal/experience/PersonexperienceCreate.jsp). Multi-row per employee.
[Table("Hrd_Experience")]
public class Hrd_Experience
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    [StringLength(300)]
    public string? Position { get; set; }
    [StringLength(300)]
    public string? Company { get; set; }

    [StringLength(1000)]
    public string? Remark { get; set; }

    public DateTime? ModDate { get; set; }
    [StringLength(50)]
    public string? ModBy { get; set; }
}
