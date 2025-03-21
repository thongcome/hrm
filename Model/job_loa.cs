using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("job_loa")]
public partial class job_loa
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long jobmasterid { get; set; }

    public int wlevel { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal value { get; set; }

    public long? jobrefid { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public long? workflowid { get; set; }

    public long? loaid { get; set; }
}
