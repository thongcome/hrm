using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("mas_reason")]
public partial class mas_reason
{
    [Key]   
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(250)]
    
    public string? name_en { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool isActive { get; set; } = true;

    [StringLength(1000)]
    public string? description { get; set; }

    public long? workflowid { get; set; }

    public int? wlevel { get; set; }

    [InverseProperty("mas_reason")]
    public virtual ICollection<job_user_list> job_user_lists { get; set; } = new List<job_user_list>();
}
