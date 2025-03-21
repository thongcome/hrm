using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pc_RFQServiceSelect")]
public partial class pc_RFQServiceSelect
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long rfqID { get; set; }

    [StringLength(50)]
    
    public string? rfqNo { get; set; }

    public long serviceid { get; set; }

    [StringLength(50)]
    
    public string servicecode { get; set; } = null!;

    public int serviceLevel { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(2)]
    
    public string? status { get; set; }

    public bool? isActive { get; set; }

    public long jobmasterid { get; set; }
}
