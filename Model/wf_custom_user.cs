using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("wf_custom_user")]
public partial class wf_custom_user
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

    public long? loaid { get; set; }

    public bool? isCoMember { get; set; }

    [ForeignKey("subworkflowid")]
    [InverseProperty("wf_custom_users")]
    public virtual wf_sub_workflow_master subworkflow { get; set; } = null!;

    [ForeignKey("userid")]
    [InverseProperty("wf_custom_users")]
    public virtual sc_user user { get; set; } = null!;
}
