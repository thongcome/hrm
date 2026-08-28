using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// "Table of Authority" — legacy JSP-era delegation-of-authority table, same
// category as sc_menu was before this session wired it up: correct shape,
// just never connected to anything (0 rows, no code references, found by
// searching for "delegate" before modeling a new table — see CRUD skill
// rule 5). Repurposed here as the backing store for temporary
// approver-delegation on com_organization (2026-08-28): StartDateDG/
// EnddateDG/approverEmpid/delegateEmplD/isactive/remark already matched
// almost exactly; OrganizationId + the two attachment columns below are the
// only genuinely new additions this table needed. The legacy
// approvelevel/ApprovelevelText/LineExco/wlevel/Company/Position/SectTh/
// SectEn/DeptTh/DeptEn columns look like they were meant for a
// workflow-approval-level authority matrix rather than per-org boss
// delegation specifically — left unused (null) for this purpose, not
// removed, in case that original use ever gets revisited.
[Table("toa")]
public partial class toa
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long toaid { get; set; }

    [StringLength(50)]

    public string? comcode { get; set; }

    [StringLength(50)]

    public string? a { get; set; }

    public int? approvelevel { get; set; }

    [StringLength(250)]

    public string? ApprovelevelText { get; set; }

    [StringLength(250)]

    public string? LineExco { get; set; }

    // The org's approver BEFORE this delegation — what to revert to once it
    // ends (mirrors OriginalApproverEmpId in the design this table replaced).
    [Column("approverEmpid")]
    [StringLength(50)]
    public string? OriginalApproverEmpId { get; set; }

    [StringLength(250)]

    public string? NameEn { get; set; }

    // Column order in the legacy table (approverEmpid, NameEn, NameTh,
    // delegateEmplD) put this directly adjacent to the delegate reference —
    // repurposed as the delegate's display name rather than adding a new
    // column for it.
    [Column("NameTh")]
    [StringLength(250)]
    public string? DelegateName { get; set; }

    // Legacy column name kept as-is in the DB (typo'd casing from the
    // original schema) — mapped to a clean C# name via [Column].
    [Column("delegateEmplD")]
    [StringLength(50)]
    public string? DelegateEmpId { get; set; }

    [StringLength(250)]

    public string? Company { get; set; }

    [StringLength(250)]

    public string? Position { get; set; }

    [StringLength(250)]

    public string? SectTh { get; set; }

    [StringLength(250)]

    public string? SectEn { get; set; }

    [StringLength(250)]

    public string? DeptTh { get; set; }

    [StringLength(250)]

    public string? DeptEn { get; set; }

    public bool isactive { get; set; }

    [Column("StartDateDG")]
    public DateOnly? StartDate { get; set; }

    [Column("EnddateDG")]
    public DateOnly? EndDate { get; set; }

    public int? wlevel { get; set; }

    public string? remark { get; set; }

    // New columns (2026-08-28) — the org this delegation applies to, and
    // optional supporting evidence (email/document HR received authorizing
    // it), stored directly like Pay_EmployeeDocument rather than through
    // the generic doc_center table since this is always exactly one
    // attachment per delegation.
    public long? OrganizationId { get; set; }
    [StringLength(500)]
    public string? AttachmentFileName { get; set; }
    [StringLength(500)]
    public string? AttachmentStoragePath { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public virtual com_organization? Organization { get; set; }
}
