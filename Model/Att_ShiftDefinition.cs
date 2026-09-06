using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Master shift definitions — only meaningful when Att_CompanySetting.TrackingMode
// is ShiftBased. EndTime < StartTime means the shift crosses midnight.
[Table("Att_ShiftDefinition")]
public class Att_ShiftDefinition
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(20)]
    public string ShiftCode { get; set; } = null!;

    [Required, StringLength(100)]
    public string ShiftName { get; set; } = null!;

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int BreakMinutes { get; set; }

    // Flexible time: employees may arrive/leave freely as long as they are in
    // during the core hours — lateness is measured against CoreStart, early
    // leave against CoreEnd (REQ-066).
    public bool IsFlexible { get; set; }
    public TimeOnly? CoreStartTime { get; set; }
    public TimeOnly? CoreEndTime { get; set; }

    public bool IsActive { get; set; } = true;
}
