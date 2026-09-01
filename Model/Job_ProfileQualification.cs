using System.ComponentModel.DataAnnotations;

namespace HRM.Models;

// One line item of a Job Description's "คุณสมบัติประจำตำแหน่ง"
// (qualifications: education / years of experience / license / skill /
// other) for a Pos_ExecType — companion to Job_ProfileDuty (CEO order,
// 2026-09-01, structured JD).
//
// PosExecTypeId is a soft link (no nav) — same convention as
// Job_CompetencyRequirement.PosExecTypeId. LinkedCompetencyId is a real FK
// with explicit fluent config in HRMContext.OnModelCreating.
//
// IncludeInCompetency/LinkedCompetencyId exist here too (not only on
// duties) because the CEO's competency-link requirement covers "each duty
// AND qualification row" — same semantics as Job_ProfileDuty: unflagging
// never deletes the Job_CompetencyRequirement it once created.
public class Job_ProfileQualification
{
    [Key]
    public long Id { get; set; }

    public long PosExecTypeId { get; set; }

    public JobQualificationType QualType { get; set; }

    [Required, StringLength(300)]
    public string Text { get; set; } = null!;

    // จำเป็น (must-have) vs ควรมี (nice-to-have) — same distinction as
    // Job_CompetencyRequirement.IsCritical.
    public bool IsRequired { get; set; } = true;

    public int SortOrder { get; set; }

    public bool IncludeInCompetency { get; set; }

    public long? LinkedCompetencyId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Comp_Competency? LinkedCompetency { get; set; }
}
