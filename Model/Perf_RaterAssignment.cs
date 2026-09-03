using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// ผู้ประเมิน 1 คน สำหรับ instance 1 ชุด (legacy: EvalBossList/EvalSupervisor) —
// รวมฟิลด์ "จุดเด่น"/"จุดที่ควรปรับปรุง"/"จริยธรรม" ที่ legacy เก็บแยกไว้
// (EvalSumEdit.jsp) เข้ามาเป็นคอลัมน์ตรงนี้เลย เพราะเป็นข้อมูล 1:1 กับ
// ผู้ประเมินคนนี้ ไม่ต้องแยกตาราง
[Table("Perf_RaterAssignment")]
[Index(nameof(EvaluationInstanceId))]
[Index(nameof(RaterHremployeeId))]
public class Perf_RaterAssignment
{
    [Key]
    public long Id { get; set; }

    public long EvaluationInstanceId { get; set; }

    // Soft link to Hremployee.id — no navigation/FK constraint, to avoid a
    // second SQL Server cascade path into Hremployee alongside
    // Perf_EvaluationInstance.HremployeeId (see that file's comment).
    // Nullable because PerfAssignmentResolverService creates a row here even
    // when no one could be resolved for a Superior chain level (missing
    // com_organization.approver_empid partway up) — Status=Skipped with
    // RaterHremployeeId=null documents the gap instead of silently omitting
    // the level, mirroring Pos_PositionSlot.HremployeeId's vacant-slot convention.
    public long? RaterHremployeeId { get; set; }

    public PerfRaterDirection Direction { get; set; }

    // สัดส่วนน้ำหนักของทิศทางนี้ในคะแนนรวมสุดท้าย (คัดลอกมาจาก
    // Perf_RaterDirectionConfig ตอนสร้าง instance — ถ้าหลายคนอยู่ทิศทางเดียวกัน
    // เช่น peer 3 คน จะถัวเฉลี่ยกันเองภายในน้ำหนักนี้ตอนคำนวณ)
    [Column(TypeName = "decimal(5,2)")]
    public decimal WeightInFinalScore { get; set; }

    public PerfRaterStatus Status { get; set; } = PerfRaterStatus.Pending;
    public DateTime? SubmittedDate { get; set; }

    [StringLength(2000)]
    public string? Strengths { get; set; }
    [StringLength(2000)]
    public string? AreasToImprove { get; set; }
    public PerfEthicsRating? EthicsRating { get; set; }

    // บังคับกรอกเฉพาะเมื่อคะแนนถัวเฉลี่ยของ rater คนนี้ตกในเกรดที่
    // Perf_GradeBand.RequiresJustification = true (ตรวจใน PerfScoringService)
    [StringLength(1000)]
    public string? QualityJustification { get; set; }
    [StringLength(1000)]
    public string? QuantityJustification { get; set; }
    [StringLength(1000)]
    public string? EfficiencyJustification { get; set; }

    // GradeDirect method (Perf_EvaluationType.MethodType == GradeDirect): the
    // rater picks a grade outright instead of scoring indicators one by one.
    // DirectGrade is the chosen Perf_GradeBand.Grade; DirectScorePercent is
    // that band's representative percent (midpoint), stored so aggregation in
    // RecomputeInstanceScoreAsync can treat this rater exactly like a scored
    // one. Both null for the ScaleWeighted/RankByResult paths.
    [StringLength(10)]
    public string? DirectGrade { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DirectScorePercent { get; set; }

    public virtual Perf_EvaluationInstance EvaluationInstance { get; set; } = null!;
}
