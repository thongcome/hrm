using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pc_vd_te")]
public partial class pc_vd_te
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long vendorid { get; set; }

    [StringLength(50)]
    
    public string? vendorcode { get; set; }

    public long rfqid { get; set; }

    [StringLength(50)]
    
    public string? techcriteria_no { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public string? remark { get; set; }

    public string? te_comment { get; set; }

    [StringLength(250)]
    
    public string? condition { get; set; }

    public bool? isPass { get; set; }

    public bool? isAgree { get; set; }

    public bool? isActive { get; set; }

    [StringLength(250)]
    
    public string? approveBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approveDate { get; set; }

    public long? userid { get; set; }

    public int? orderTh { get; set; }

    public bool? isSentMember { get; set; }
}
