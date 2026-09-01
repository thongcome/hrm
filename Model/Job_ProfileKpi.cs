using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One "KPI ประจำตำแหน่ง" line item of a Job Description for a Pos_ExecType —
// JD block B, CEO-approved follow-up to the structured JD of 2026-09-01
// (Job_ProfileDuty / Job_ProfileQualification). These are the standing
// performance indicators the position is measured on, printed on the JD
// sheet alongside duties and qualifications.
//
// PosExecTypeId is a soft link (no nav) — same convention as
// Job_ProfileDuty.PosExecTypeId / Job_CompetencyRequirement.PosExecTypeId,
// indexed in HRMContext.OnModelCreating for the per-position load every
// JobProfileDetail/print visit does.
public class Job_ProfileKpi
{
    [Key]
    public long Id { get; set; }

    public long PosExecTypeId { get; set; }

    // ชื่อตัวชี้วัด, e.g. "ยอดขายต่อไตรมาส", "อัตราการลาออกของทีม".
    [Required, StringLength(300)]
    public string Name { get; set; } = null!;

    // เป้าหมายแบบคำอธิบาย — free text for targets that aren't a single
    // number ("ไม่เกิน 5% ต่อปี", "ภายใน 3 วันทำการ").
    [StringLength(500)]
    public string? TargetDescription { get; set; }

    // หน่วยของค่าเป้าหมาย, e.g. "%", "บาท", "วัน".
    [StringLength(50)]
    public string? Unit { get; set; }

    // ค่าเป้าหมายแบบตัวเลข — nullable; a KPI may be described only by
    // TargetDescription.
    [Column(TypeName = "decimal(18,2)")]
    public decimal? TargetValue { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
