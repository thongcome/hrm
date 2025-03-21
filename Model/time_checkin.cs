using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("time_checkin")]
public partial class time_checkin
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long? userid { get; set; }

    [StringLength(50)]
    
    public string? emp_code { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? checkin_time { get; set; }

    public bool? isActive { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? moddate { get; set; }

    [StringLength(250)]
    
    public string? latitude { get; set; }

    [StringLength(250)]
    
    public string? longitude { get; set; }

    [StringLength(50)]
    
    public string? comp_code { get; set; }

    public long? taskid { get; set; }
}
