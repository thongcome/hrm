using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Raw scan/check-in events — the ingestion layer. One row per punch, from a
// device-log import, a manual HR entry, or (once ESS resumes) a WFH
// self-check-in. Att_DailyAttendance is always derived/rebuildable from this.
[Table("Att_PunchLog")]
public class Att_PunchLog
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    public long HremployeeId { get; set; }

    // the employee code as it appeared in the device export, kept even after
    // matching so unmatched/mismatched rows can be diagnosed later
    [Required, StringLength(50)]
    public string RawEmpCode { get; set; } = null!;

    public long? DeviceId { get; set; }

    public DateTime PunchTime { get; set; }

    public AttPunchDirection Direction { get; set; } = AttPunchDirection.Unknown;

    public AttPunchSource Source { get; set; } = AttPunchSource.Device;

    [Column(TypeName = "decimal(9,6)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal? Longitude { get; set; }

    public long? ImportBatchId { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Att_Device? Device { get; set; }
    public virtual Att_ImportBatch? ImportBatch { get; set; }
}
