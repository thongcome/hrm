using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pc_vendor_quotation")]
public partial class pc_vendor_quotation
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? quotation_no { get; set; }

    [StringLength(50)]
    
    public string? rfq_no { get; set; }

    [StringLength(50)]
    
    public string? PRNo { get; set; }

    [StringLength(250)]
    
    public string? subject { get; set; }

    [StringLength(50)]
    
    public string? type { get; set; }

    public DateOnly? rfqdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    public string? remark { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? total { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? total_net { get; set; }

    [StringLength(250)]
    
    public string? createby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? vat { get; set; }

    public bool? isVat { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? vat_amount { get; set; }

    [StringLength(50)]
    
    public string? vendor_code { get; set; }

    [StringLength(50)]
    
    public string? vendor_taxid { get; set; }

    [StringLength(250)]
    
    public string? vendor_name { get; set; }

    [StringLength(250)]
    
    public string? vendor_alias_name { get; set; }

    public long? vendorid { get; set; }

    public long? rfqid { get; set; }

    public bool? isBidwon { get; set; }

    [StringLength(2500)]
    public string? addr { get; set; }

    [StringLength(500)]
    public string? logo { get; set; }

    public bool? isactive { get; set; }

    public bool? isRequisitionerState { get; set; }

    [StringLength(250)]
    
    public string? CURRENCY_KEY { get; set; }

    public bool? isPriceApprove { get; set; }

    [StringLength(250)]
    
    public string? PriceApproveBy { get; set; }

    public int? negoCount { get; set; }
}
