using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("emp_overtime_request")]
public partial class emp_overtime_request
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string empid { get; set; } = null!;

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime requestDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime starttime { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime endtime { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? workhour { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? workminute { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? ot_rate { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(50)]
    
    public string? approveby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approvedate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? real_starttime { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? real_endtime { get; set; }

    [StringLength(1000)]
    public string? objective { get; set; }

    [StringLength(1000)]
    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? orgcode { get; set; }

    [StringLength(500)]
    public string? orgname { get; set; }

    [StringLength(500)]
    public string? com_code_all { get; set; }

    // Added to drive the Workflow Approval Engine (Services/Workflow/
    // WorkflowEngineService.cs) — this table pre-existed as an orphaned
    // scaffold (0 rows, no code referencing it) before being repurposed as
    // the first real "generic document" wired through Block 7 routing.
    // Soft links only, same convention as Pay_AdhocPayItem.HremployeeId /
    // Pay_PayrollRun snapshot fields elsewhere in this app — no FK
    // constraints, checked in application code.
    public long? hremployeeid { get; set; }

    [StringLength(6)]
    public string? companyid { get; set; }

    // job_master.jobmasterid of the approval job started for this request
    // (via StartJobAsync, reftable="emp_overtime_request", refid=this.id).
    // Null until submitted; the request stays editable/re-submittable in
    // that state. Mirrors Pay_AdhocPayItem.ConsumedByPayrollRunId's
    // "not consumed yet" null convention.
    public long? jobmasterid { get; set; }

    // Set once an approved request has been turned into an HRW_OT row for
    // payroll to actually pay (a deliberate manual HR action, not automatic
    // — see OtRequestList.razor — same "explicit pull, not auto-inject"
    // pattern as Pay_AdhocPayItem consumption).
    public long? hrwOtId { get; set; }
}
