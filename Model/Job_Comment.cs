using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("Job_Comment")]
public partial class Job_Comment
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(1000)]
    public string comment { get; set; } = null!;

    public bool isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? mobby { get; set; }

    [StringLength(1000)]
    public string? doc1 { get; set; }

    [StringLength(1000)]
    public string? doc2 { get; set; }

    [StringLength(250)]
    
    public string? createby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }
}
