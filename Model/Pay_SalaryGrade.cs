using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Pay grade / salary band master data — a NEW table, not a resurrection of
// pos_position's min_salary/max_salary/normal_salary (those are float, not
// decimal, and have zero live code touching them anywhere in this codebase
// — the same disconnected-master-data situation as PosCode/DeptgrpCode).
// Employees are assigned a grade via Hremployee.SalaryGradeId — a soft
// link with no DB FK constraint (same convention Hremployee.OrganizationId
// already uses; no navigation property either side), since Hremployee is a
// legacy-scaffolded table where every other cross-table reference is soft.
[Table("Pay_SalaryGrade")]
public class Pay_SalaryGrade
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(20)]
    public string GradeCode { get; set; } = null!;

    [Required, StringLength(200)]
    public string GradeName { get; set; } = null!;

    [Column(TypeName = "decimal(15,2)")]
    public decimal MinSalary { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal MidSalary { get; set; }
    [Column(TypeName = "decimal(15,2)")]
    public decimal MaxSalary { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
