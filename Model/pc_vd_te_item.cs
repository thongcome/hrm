using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pc_vd_te_item")]
public partial class pc_vd_te_item
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long? vendorid { get; set; }

    [StringLength(50)]
    
    public string? vendorcode { get; set; }

    public long rfqid { get; set; }

    [StringLength(50)]
    
    public string? techcriteria_no { get; set; }

    [StringLength(50)]
    
    public string pr_no { get; set; } = null!;

    [StringLength(50)]
    
    public string? pr_item { get; set; }

    public int? no1 { get; set; }

    public int? no2 { get; set; }

    public int? no3 { get; set; }

    public string? criteria { get; set; }

    [StringLength(250)]
    
    public string? subject { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public string? doc1 { get; set; }

    public string? doc2 { get; set; }

    public string? doc3 { get; set; }

    public string? remark { get; set; }

    public string? te_comment { get; set; }

    [StringLength(50)]
    
    public string? isPassCondition { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? point { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? point_evaluate { get; set; }

    public bool? isActive { get; set; }

    [StringLength(50)]
    
    public string? condition { get; set; }

    [StringLength(250)]
    
    public string? approveBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approveDate { get; set; }

    public long? pc_te_id { get; set; }

    public long? prid { get; set; }

    [StringLength(50)]
    
    public string? topicNo { get; set; }

    public long? teitemid { get; set; }
}
