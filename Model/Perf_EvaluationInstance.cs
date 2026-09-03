using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// การประเมินจริง 1 ชุดของพนักงาน 1 คน ในรอบ 1 รอบ (legacy: EvalMaster) — เก็บ
// snapshot ตำแหน่ง/สังกัด ณ ตอนสร้าง instance แทนตารางแยก evalpositionlist/
// evalsectionlist/evalpersonposition ของเดิม (ตาม pattern denormalized-columns
// ของ Pay_PayrollEmployee) เพื่อไม่ให้ผลประเมินเก่าเพี้ยนถ้าโครงสร้างองค์กร/
// ตำแหน่งถูกแก้ทีหลัง
[Table("Perf_EvaluationInstance")]
[Index(nameof(EvaluationPeriodId), nameof(HremployeeId))]
[Index(nameof(JobMasterId))]
public class Perf_EvaluationInstance
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? InstanceCode { get; set; }

    public long EvaluationPeriodId { get; set; }
    public long EvaluationTypeId { get; set; }

    // Soft link to Hremployee.id — no DB FK constraint, same convention as
    // Pos_PositionSlot.HremployeeId. Deliberately not a navigation property:
    // Perf_RaterAssignment also links to Hremployee independently
    // (RaterHremployeeId), and a real FK here would create a second cascade
    // path into the same rows SQL Server rejects at migration time.
    public long HremployeeId { get; set; }

    [StringLength(6)]
    public string? SnapshotEmpNo { get; set; }
    [StringLength(200)]
    public string? SnapshotEmpName { get; set; }
    [StringLength(200)]
    public string? SnapshotPositionName { get; set; }
    [StringLength(50)]
    public string? SnapshotOrganizationCode { get; set; }
    [StringLength(500)]
    public string? SnapshotOrganizationName { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal? FinalScorePercent { get; set; }
    [StringLength(10)]
    public string? FinalGrade { get; set; }

    public PerfInstanceStatus Status { get; set; } = PerfInstanceStatus.Draft;

    // เชื่อมกับ job_master ผ่าน WorkflowEngineService.StartJobAsync — null จนกว่า
    // จะกดส่งอนุมัติ (เหมือน Org_OrganizationChangeRequest.JobMasterId)
    public long? JobMasterId { get; set; }

    // Per-person merit ADJUSTMENT on top of the grade band's standard increase%
    // (AutoX #5: "เงินขึ้นจากการประเมิน + adjust"). The applied raise% =
    // GradeBand.SalaryIncreasePercent + MeritAdjustmentPercent. Null/0 = no adjust.
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MeritAdjustmentPercent { get; set; }
    [StringLength(500)]
    public string? MeritAdjustmentReason { get; set; }

    // กัน apply ซ้ำตอนขึ้นเงินเดือนตามผลประเมิน (idempotent เหมือน
    // Org_OrganizationChangeRequest.IsApplied)
    public bool IsMeritApplied { get; set; }
    public DateTime? MeritAppliedDate { get; set; }

    public virtual Perf_EvaluationPeriod EvaluationPeriod { get; set; } = null!;
    public virtual Perf_EvaluationType EvaluationType { get; set; } = null!;
}
