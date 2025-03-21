using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("error_log_file")]
public partial class error_log_file
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long logid { get; set; }

    public DateOnly? logdate { get; set; }

    [StringLength(250)]
    
    public string? logfile { get; set; }

    [StringLength(250)]
    
    public string? logpath { get; set; }
}
