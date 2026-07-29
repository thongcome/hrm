using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Aggregated per-employee-per-day summary, derived from Att_PunchLog (+
// Att_ShiftAssignment if present). Always rebuildable from raw punches —
// never the source of truth itself.
[Table("Att_DailyAttendance")]
public class Att_DailyAttendance
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }
    public DateOnly WorkDate { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    public long? ShiftDefinitionId { get; set; }

    public DateTime? FirstIn { get; set; }
    public DateTime? LastOut { get; set; }
    public int? WorkedMinutes { get; set; }

    public bool IsLate { get; set; }
    public bool IsEarlyLeave { get; set; }
    public bool IsAbsent { get; set; }

    public AttWorkLocation WorkLocation { get; set; } = AttWorkLocation.Office;

    [StringLength(500)]
    public string? Remark { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Att_ShiftDefinition? ShiftDefinition { get; set; }
}
