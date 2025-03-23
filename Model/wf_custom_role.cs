using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("wf_custom_role")]
public partial class wf_custom_role
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long? subworkflowid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? modate { get; set; }

    public long? modby { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    [Required]
    public bool? isactive { get; set; }

    public long workflowid { get; set; }

    public int wlevel { get; set; }

    public long roleid { get; set; }

    [StringLength(50)]
    
    public string? rolecode { get; set; }

    public int? emplevel { get; set; }

    [StringLength(50)]
    
    public string? poscode { get; set; }

    [StringLength(50)]
    
    public string? orgcode { get; set; }

    public long? loaid { get; set; }

    public bool? isHeader { get; set; }

    [ForeignKey("subworkflowid")]
    [InverseProperty("wf_custom_roles")]
    public virtual wf_sub_workflow_master? subworkflow { get; set; }
}
