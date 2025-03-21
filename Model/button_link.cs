using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("button_link")]
public partial class button_link
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long btlinkid { get; set; }

    public long btid { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    public long? workflowid { get; set; }

    [StringLength(50)]
    
    public string? wlevel { get; set; }

    [StringLength(10)]
    
    public string? role { get; set; }

    [StringLength(50)]
    
    public string? bcode { get; set; }

    public bool? isactive { get; set; }

    [StringLength(50)]
    
    public string? wfcode { get; set; }

    public bool? isshow { get; set; }
}
