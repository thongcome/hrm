using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Advance.Workflow.Domain.Entities;

[Table("wf_adhoc_user")]
public partial class wf_adhoc_user
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long subworkflowid { get; set; }

    public long workflowid { get; set; }

    public int wlevel { get; set; }

    public long userid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? modate { get; set; }

    public long? modby { get; set; }

    [StringLength(50)]
    
    public string? empid { get; set; }

    public bool isactive { get; set; }

    public int? emplevel { get; set; }

    public long? jobmasterid { get; set; }

    public int? orderTh { get; set; }

    [StringLength(50)]
    
    public string? mode { get; set; }

    public long? prid { get; set; }

    public long? rfqid { get; set; }

    [ForeignKey("jobmasterid")]
    [InverseProperty("wf_adhoc_users")]
    public virtual job_master? jobmaster { get; set; }

    // TODO(seam): the original HRM column also carries a navigation property to
    // sc_user (HRM's identity table, outside this project). Domain keeps only the
    // scalar `userid` FK above; resolve the display name/user record through
    // Advance.Workflow.Contracts.IWorkflowUserDirectory instead of a join.
}
