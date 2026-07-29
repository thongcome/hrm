using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One row per company — the master switch for the whole Attendance module.
// TrackingMode = None means every other Att_* page for this company shows an
// "not enabled" state instead of demanding data entry.
[Table("Att_CompanySetting")]
public class Att_CompanySetting
{
    [Key]
    [StringLength(50)]
    public string CompanyId { get; set; } = null!;

    public AttTrackingMode TrackingMode { get; set; } = AttTrackingMode.None;

    // used only when TrackingMode == SimpleInOut (no shifts defined)
    public TimeOnly? DefaultWorkStart { get; set; }
    public TimeOnly? DefaultWorkEnd { get; set; }

    public bool AllowWfh { get; set; }
}
