using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("log_system_log")]
public partial class log_system_log
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    [Column(TypeName = "numeric(18, 0)")]
    public decimal id { get; set; }

    [StringLength(250)]
    
    public string? username { get; set; }

    [StringLength(250)]
    
    public string? activity { get; set; }

    [StringLength(250)]
    
    public string? ipaddress { get; set; }

    [StringLength(250)]
    
    public string? hostname { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime accesstime { get; set; }

    [StringLength(250)]
    
    public string? actstatus { get; set; }

    [Column(TypeName = "ntext")]
    public string? remark { get; set; }

    [Column(TypeName = "ntext")]
    public string? oldvalue { get; set; }

    [Column(TypeName = "ntext")]
    public string? newvalue { get; set; }

    public long userid { get; set; }

    public byte[]? moddate { get; set; }
}
