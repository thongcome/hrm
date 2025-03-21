using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("wf_decision_status")]
public partial class wf_decision_status
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long workflowstatusid { get; set; }

    [StringLength(50)]
    
    public string? StepType { get; set; }

    [StringLength(250)]
    
    public string? ButtonName { get; set; }

    [StringLength(50)]
    
    public string ButtonCode { get; set; } = null!;

    [StringLength(250)]
    
    public string? ControlWF { get; set; }

    [StringLength(250)]
    
    public string? methodWF { get; set; }

    [StringLength(10)]
    
    public string statuscode { get; set; } = null!;

    [StringLength(10)]
    
    public string? bizstatuscode { get; set; }

    public long? Workflowid { get; set; }

    public long? subworkflowid { get; set; }

    [StringLength(50)]
    
    public string? wlevel { get; set; }

    [StringLength(50)]
    
    public string? moveType { get; set; }

    [ForeignKey("subworkflowid")]
    [InverseProperty("wf_decision_statuses")]
    public virtual wf_sub_workflow_master? subworkflow { get; set; }
}
