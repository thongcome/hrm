using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("wf_emailTemplate")]
public partial class wf_emailTemplate
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long? workflowid { get; set; }

    public int? wlevelTarget { get; set; }

    public int? wlevelFrom { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    public string? body { get; set; }

    public string? param { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(2000)]
    public string? remark { get; set; }

    [StringLength(500)]
    public string? files { get; set; }

    [StringLength(500)]
    public string? file_path { get; set; }

    public int? paramNo { get; set; }
}
