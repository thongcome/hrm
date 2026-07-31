using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("job_subworkflow_master")]
public partial class job_subworkflow_master
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long jobsubworkflowid { get; set; }

    public long jobmasterid { get; set; }

    public long? workflowid { get; set; }

    public int wlevel { get; set; }

    public bool isupperrole { get; set; }

    public bool isupperuser { get; set; }

    public bool iscondition { get; set; }

    public bool isorcondition { get; set; }

    public bool isandcondition { get; set; }

    [Column(TypeName = "decimal(6, 2)")]
    public decimal? andpercent { get; set; }

    [StringLength(50)]

    public string status { get; set; } = null!;

    // Snapshotted from wf_sub_workflow_master.forwardstatus/backwardstatus
    // (Block 9, Moving Status) — status above already snapshots standstatus
    // ("pending at this level") but the table never captured the other two
    // directions, so job_master.status couldn't reflect them without a live
    // lookup. Added so the engine can set job_master.status to the
    // admin-configured label for whichever direction a level actually
    // resolved (forward = approved/completed, backward = rejected/returned).
    [StringLength(50)]
    public string? forwardstatus { get; set; }

    [StringLength(50)]
    public string? backwardstatus { get; set; }

    public bool istop { get; set; }

    // Snapshotted from wf_sub_workflow_master.isLOA at job-start time (see
    // WorkflowEngineService.StartJobAsync) — this table didn't originally
    // carry this flag even though it mirrors most of the level-config bools;
    // added here so completion (istop) and LOA-branching decisions can both
    // be made from the frozen snapshot, not a live lookup that could break
    // if the workflow config changes mid-flight (Block 4).
    public bool isLOA { get; set; }

    // Snapshotted from wf_sub_workflow_master.isNeedsupervisorapprove
    // (Block 6, Mix Approval) — same freeze rationale as isLOA above.
    public int? isNeedsupervisorapprove { get; set; }

    [Column(TypeName = "text")]
    public string? remark { get; set; }

    public bool? iscustomUser { get; set; }

    public bool? iscustomRole { get; set; }

    public int? empLevel { get; set; }

    public bool? isshow { get; set; }

    [StringLength(250)]
    
    public string? reason { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public int? jobseq { get; set; }
}
