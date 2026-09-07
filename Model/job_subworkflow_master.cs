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

    // Snapshotted from wf_sub_workflow_master.backwardlevel at job-start time
    // (see StartJobAsync) — same freeze rationale as isLOA/isNeedsupervisorapprove
    // above. Null = no bounce-back configured for this level; reject fails the
    // job outright exactly as before this feature existed.
    public int? backwardlevel { get; set; }

    // Snapshotted from wf_sub_workflow_master.verticalMaxLevel — the
    // self-terminating vertical-climb cap (CEO, 2026-09-07). `istop` above
    // is deliberately NOT frozen to a fixed value for this level when this
    // is set: WorkflowEngineService flips THIS SAME row's istop dynamically,
    // per job, the moment each hop is issued, once it knows whether that hop
    // is the last one (hop count reached, or the org chart ran out first).
    public int? verticalMaxLevel { get; set; }

    // Snapshotted from wf_sub_workflow_master.isPool (CEO, 2026-09-07): a
    // pool level's PENDING rows also surface on the shared /wf/pool page (in
    // addition to each candidate's own /wf/my-inbox) so a team can see and
    // claim shared work, not just wait for it to show up individually.
    // Deliberately does NOT change how the level completes — that's still
    // isorcondition (whoever acts first) exactly as configured; isPool is
    // additive display/discovery only, never a second completion rule.
    public bool isPool { get; set; }

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

    // Per-level duration tracking (CEO, 2026-09-07 follow-up): starttime is
    // stamped the first time this level's round is issued
    // (AssignLevelApproversAsync — a Mix Approval pre-check hop re-issuing
    // the SAME level does NOT re-stamp it, since the level itself hasn't
    // restarted), endtime the moment this level's involvement in the job
    // truly concludes (TryAdvanceLevelAsync — approved/rejected/bounced/
    // advanced to the next level; NOT set on a non-terminal vertical-chain
    // hop, since that's still the same level continuing). Feeds the
    // "ค้างมากี่วัน" passive-expire badge (wf_workflow.wexpireday) — no
    // background job computes this, it's read lazily whenever a list page
    // loads, same apply-on-read pattern as everywhere else in this engine.
    [Column(TypeName = "datetime")]
    public DateTime? starttime { get; set; }
    [Column(TypeName = "datetime")]
    public DateTime? endtime { get; set; }

    // Snapshotted from wf_sub_workflow_master.wfcode = the OWNING workflow's
    // stable wf_workflow.workflowcode (CEO, 2026-09-07 follow-up: "เผื่อมี
    // migration DB ในอนาคต ถ้าใช้ running [workflowid] จับ มันจะเพี้ยนตอน
    // migration") — a migration/re-import can shift auto-increment
    // workflowid values; this stable code survives that so historical rows
    // stay identifiable. Populated alongside workflowid, not instead of it —
    // no existing lookup is rewired to use this yet.
    [StringLength(50)]
    public string? wfcode { get; set; }

    // Full-level snapshot (CEO, 2026-09-07 follow-up: "field อื่นๆ คิดง่ายๆ
    // คือเราต้องอ่าน subworkflow ที่ level เดียวกัน ไปใส่ job_subworkflowmaster
    // ทั้งหมด" — stop hand-picking which config fields matter, just freeze
    // every remaining wf_sub_workflow_master column onto this row too, same
    // freeze rationale as isLOA/verticalMaxLevel/etc. above). Excluded on
    // purpose: wf_sub_workflow_master.status (a long?, confirmed unused
    // anywhere in epms or HRM) — collides in name with the unrelated
    // `status` (string) column already on this table, which is populated
    // from standstatus, not from that field.
    public bool? isAdhocUser { get; set; }
    public bool? iscustomApprover { get; set; }
    [StringLength(50)]
    public string? approvedstatus { get; set; }
    [StringLength(50)]
    public string? declinestatus { get; set; }
    public bool? isReturnSender { get; set; }
    [StringLength(50)]
    public string? loacode { get; set; }
    public bool? isAutoApproveAllow { get; set; }
    public bool? isNeedBudgetApproval { get; set; }
    [StringLength(50)]
    public string? sitinstatus { get; set; }
    public int? aa_id { get; set; }
    public int? aa_level { get; set; }
    [StringLength(250)]
    public string? controller { get; set; }
    [StringLength(250)]
    public string? action { get; set; }
    [StringLength(250)]
    public string? displayName { get; set; }
    public long? userid1 { get; set; }
    public long? userid2 { get; set; }
    public long? userid3 { get; set; }
    [StringLength(250)]
    public string? subject { get; set; }
    [StringLength(250)]
    public string? subjectBiz { get; set; }
    [StringLength(500)]
    public string? describeBiz { get; set; }
    [StringLength(500)]
    public string? describe { get; set; }
    [StringLength(250)]
    public string? actionEdit { get; set; }
    [StringLength(250)]
    public string? subject_en { get; set; }
    [StringLength(250)]
    public string? subjectBiz_en { get; set; }
    [StringLength(250)]
    public string? describeBiz_en { get; set; }
    [StringLength(250)]
    public string? describe_en { get; set; }
    public bool? isApproverSameOrg { get; set; }
    public bool? isApproverSameCostCenter { get; set; }
    public bool? isManualButton { get; set; }
    [StringLength(250)]
    public string? ApproveController { get; set; }
    [StringLength(250)]
    public string? ApproveAction { get; set; }
}
