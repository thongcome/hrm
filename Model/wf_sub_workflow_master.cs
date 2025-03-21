using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("wf_sub_workflow_master")]
[Index("wlevel", "workflowid", Name = "IX_wf_sub_workflow_master")]
public partial class wf_sub_workflow_master
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long subworkflowid { get; set; }

    [StringLength(250)]
    
    public string? subject { get; set; }

    public long workflowid { get; set; }

    public int wlevel { get; set; }

    public bool isAdhocUser { get; set; }=false;

    public bool iscustomApprover { get; set; } = false;

    public bool isupperrole { get; set; } = false;

    public bool isupperuser { get; set; } = false;

    public bool iscustomRole { get; set; } = false;

    public bool iscustomUser { get; set; } = false;

    public bool iscondition { get; set; } = false;

    public bool isorcondition { get; set; } = false;

    public bool isandcondition { get; set; } = false;

    [Column(TypeName = "decimal(6, 2)")]
    public decimal? andpercent { get; set; }

    [StringLength(50)]
    
    public string forwardstatus { get; set; } = null!;

    [StringLength(50)]
    
    public string standstatus { get; set; } = null!;

    [StringLength(50)]
    
    public string backwardstatus { get; set; } = null!;

    [StringLength(50)]
    
    public string? approvedstatus { get; set; }

    [StringLength(50)]
    
    public string? declinestatus { get; set; }

    public bool istop { get; set; }

    [Column(TypeName = "text")]
    public string? remark { get; set; }

    public long? status { get; set; }

    public bool isReturnSender { get; set; } = false;

    public int? empLevel { get; set; }

    public bool isshow { get; set; } = false;

    public bool isLOA { get; set; } = false;

    [StringLength(50)]
    
    public string? loacode { get; set; }

    [StringLength(50)]
    
    public string? wfcode { get; set; }

    public bool isAutoApproveAllow { get; set; } = false;

    public bool isNeedBudgetApproval { get; set; } = false;

    public bool isPool { get; set; } = false;

    public int? backwardlevel { get; set; }

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

    public bool isApproverSameOrg { get; set; } = false;

    public bool isApproverSameCostCenter { get; set; } = false;

    public bool isManualButton { get; set; } = false;

    [StringLength(250)]
    
    public string? ApproveController { get; set; }

    [StringLength(250)]
    
    public string? ApproveAction { get; set; }

    [InverseProperty("subworkflowmaster")]
    public virtual ICollection<job_user_list> job_user_lists { get; set; } = new List<job_user_list>();

    [InverseProperty("subworkflow")]
    public virtual ICollection<wf_budget> wf_budgets { get; set; } = new List<wf_budget>();

    [InverseProperty("subworkflow")]
    public virtual ICollection<wf_custom_role> wf_custom_roles { get; set; } = new List<wf_custom_role>();

    [InverseProperty("subworkflow")]
    public virtual ICollection<wf_custom_user> wf_custom_users { get; set; } = new List<wf_custom_user>();

    [InverseProperty("subworkflow")]
    public virtual ICollection<wf_decision_status> wf_decision_statuses { get; set; } = new List<wf_decision_status>();
}
