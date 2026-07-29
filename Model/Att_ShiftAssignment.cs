using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// The rotating per-employee, per-day shift schedule — one row per employee
// per work date. Absence of a row for a given employee/date under
// ShiftBased tracking means "no shift assigned that day" (not an error).
[Table("Att_ShiftAssignment")]
public class Att_ShiftAssignment
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public long ShiftDefinitionId { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
    public virtual Att_ShiftDefinition ShiftDefinition { get; set; } = null!;
}
