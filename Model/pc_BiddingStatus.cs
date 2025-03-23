using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pc_BiddingStatus")]
public partial class pc_BiddingStatus
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? Name { get; set; }

    [StringLength(250)]
    
    public string? Name_en { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? modate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }
}
