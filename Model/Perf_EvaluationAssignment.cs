using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// "กติกา" การมอบหมายการประเมิน (legacy: SetAssignEvaluation + SetEvalUserMap +
// SetEvalTypeMap รวมกัน) — ไม่ใช่รายการประเมินจริง แค่บอกว่า "รอบนี้ แบบประเมินนี้
// ทิศทางนี้ ให้ใช้กับใคร (หน่วยงาน/ชื่อตำแหน่ง/รายบุคคล)" — PerfAssignmentResolverService
// จะ resolve แถวนี้เป็น Perf_EvaluationInstance/Perf_RaterAssignment จริงอีกที
[Table("Perf_EvaluationAssignment")]
public class Perf_EvaluationAssignment
{
    [Key]
    public long Id { get; set; }

    public long EvaluationPeriodId { get; set; }
    public long EvaluationTypeId { get; set; }
    public long RaterDirectionConfigId { get; set; }

    public PerfTargetScope TargetScope { get; set; }

    // ใช้เฉพาะเมื่อ TargetScope = Organization — รวมหน่วยงานย่อยทั้งหมดผ่าน
    // com_organization.orgcodefull LIKE '{code}%'
    public long? TargetOrganizationId { get; set; }
    // ใช้เฉพาะเมื่อ TargetScope = Position
    public long? TargetPosExecTypeId { get; set; }
    // ใช้เฉพาะเมื่อ TargetScope = Employee
    public long? TargetHremployeeId { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // ตั้ง true หลังกดปุ่ม "สร้างรายการประเมินจริง" แล้ว — กันกดซ้ำสร้าง instance ซ้ำ
    public bool IsResolved { get; set; }
    public DateTime? ResolvedDate { get; set; }

    public virtual Perf_EvaluationPeriod EvaluationPeriod { get; set; } = null!;
    public virtual Perf_EvaluationType EvaluationType { get; set; } = null!;
    public virtual Perf_RaterDirectionConfig RaterDirectionConfig { get; set; } = null!;
}
