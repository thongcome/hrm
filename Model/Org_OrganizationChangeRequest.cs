using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Staged, effective-dated com_organization change — covers exactly the 3
// change kinds that require workflow approval (create/move/change-boss).
// Minor field edits (name, remark, dates, etc.) never create a row here;
// OrganizationAdmin.razor still writes those straight to com_organization.
// One table with a discriminator + nullable Old/New columns instead of 3
// tables — all 3 kinds share the same lifecycle (submit -> workflow job ->
// lazy-apply -> applied) and the same history-page rendering.
[Table("Org_OrganizationChangeRequest")]
[Index(nameof(IsApplied), nameof(EffectiveFrom))]
[Index(nameof(TargetOrganizationId))]
[Index(nameof(JobMasterId))]
public class Org_OrganizationChangeRequest
{
    [Key]
    public long Id { get; set; }

    public OrgOrganizationChangeType ChangeType { get; set; }

    // Existing org being changed — null for NewOrganization (row doesn't exist yet).
    public long? TargetOrganizationId { get; set; }
    [StringLength(50)]
    public string? TargetOrganizationCode { get; set; } // snapshot at request time, for display

    // --- ChangeParent fields (also used by NewOrganization for its initial parent) ---
    [StringLength(250)]
    public string? OldParentCode { get; set; }
    [StringLength(250)]
    public string? NewParentCode { get; set; }
    public bool? NewIsTop { get; set; }

    // --- ChangeApprover fields ---
    [StringLength(50)]
    public string? OldApproverEmpId { get; set; }
    [StringLength(250)]
    public string? OldApproverName { get; set; }
    [StringLength(50)]
    public string? NewApproverEmpId { get; set; }
    [StringLength(250)]
    public string? NewApproverName { get; set; }

    // --- NewOrganization fields (full create payload) ---
    [StringLength(50)]
    public string? NewCode { get; set; }
    [StringLength(500)]
    public string? NewName { get; set; }
    [StringLength(500)]
    public string? NewNameEn { get; set; }
    [StringLength(50)]
    public string? NewAbbr { get; set; }
    [StringLength(50)]
    public string? NewAbbrEn { get; set; }
    public bool? NewIsActive { get; set; }
    public bool? NewIsManPowerCount { get; set; }
    [StringLength(20)]
    public string? NewSectionTypeCode { get; set; }
    public long? NewSubSectionTypeId { get; set; }
    [StringLength(20)]
    public string? NewCostCenterCode { get; set; }
    public DateTime? NewStartDate { get; set; }
    public DateTime? NewEndDate { get; set; }
    [StringLength(2000)]
    public string? NewRemark { get; set; }

    // --- Lifecycle ---
    public DateOnly EffectiveFrom { get; set; }

    // Soft link to job_master.jobmasterid — null only in the brief window
    // between inserting this row and StartJobAsync succeeding. If
    // StartJobAsync throws, this stays null forever and the row becomes a
    // harmless orphan: ApplyDueChangesAsync only ever looks at rows where
    // JobMasterId != null, so an orphan can never mutate com_organization.
    public long? JobMasterId { get; set; }

    public bool IsApplied { get; set; }
    public DateTime? AppliedDate { get; set; }

    public long RequestedByUserId { get; set; }
    [StringLength(50)]
    public string? RequestedByEmpId { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.Now;

    [StringLength(1000)]
    public string? RequestNote { get; set; }

    // Guards against two concurrent lazy-apply calls (e.g. two browser tabs)
    // both trying to apply the same due row — the loser's SaveChanges fails
    // and is swallowed; the next apply pass re-derives the due list fresh.
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
