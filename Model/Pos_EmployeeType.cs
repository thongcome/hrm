using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประเภทพนักงาน (employee-type lookup for the Position module) — ported from
// legacy PIS "employeetype" table (Position/EmployeeType/EmployeeTypeCreate.jsp).
// Distinct from Hremployee.EmptypeCode (a free-text code already in use for
// payroll) — this is the Position module's own classification used to scope
// position slots and position titles (พนักงานประจำ/สัญญาจ้าง/ชั่วคราว/กรรมการ).
[Table("Pos_EmployeeType")]
public class Pos_EmployeeType
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(1000)]
    public string? Remark { get; set; }

    public bool IsActive { get; set; } = true;
}
