using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Configurable OT premium multiplier per day type, replacing the flat
// "type any number" ot_rate field on emp_overtime_request. One active row
// per (CompanyId, DayType) is expected, enforced in the admin page rather
// than a DB constraint (same convention as every other config table in this
// app). Multiplier is applied to the employee's computed hourly wage —
// see OtRateCalculator.
[Table("Att_OtRule")]
public class Att_OtRule
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? Code { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public OtDayType DayType { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Multiplier { get; set; }

    public bool IsActive { get; set; } = true;
}
