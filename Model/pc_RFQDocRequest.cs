using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pc_RFQDocRequest")]
public partial class pc_RFQDocRequest
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? rfqno { get; set; }

    public bool? isRequired { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public long? doctypeid { get; set; }

    [StringLength(250)]
    
    public string? doctypecode { get; set; }

    public long? rfqid { get; set; }

    public bool? isActive { get; set; }

    public bool? isInclude { get; set; }

    [StringLength(2000)]
    public string? remark { get; set; }

    [StringLength(500)]
    
    public string? files { get; set; }

    [StringLength(500)]
    public string? path { get; set; }

    [StringLength(50)]
    
    public string? subMode1 { get; set; }

    public bool isDefaultRequired { get; set; }

    public int? orderTh { get; set; }

    [ForeignKey("doctypeid")]
    [InverseProperty("pc_RFQDocRequests")]
    public virtual mas_doc_type? doctype { get; set; }

    [ForeignKey("rfqid")]
    [InverseProperty("pc_RFQDocRequests")]
    public virtual pc_rfq? rfq { get; set; }
}
