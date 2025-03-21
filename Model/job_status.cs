using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("job_status")]
[Index("jobstatuscode", Name = "IX_job_status")]
public partial class job_status
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long jobstatusid { get; set; }

    [StringLength(20)]
    
    public string jobstatuscode { get; set; } = null!;

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    [StringLength(250)]
    
    public string name_en { get; set; } = null!;

    [StringLength(20)]
    
    public string? businessstatus { get; set; }

    [StringLength(250)]
    
    public string? bizName { get; set; }

    public bool? isactive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }
}
