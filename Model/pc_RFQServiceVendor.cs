using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pc_RFQServiceVendor")]
public partial class pc_RFQServiceVendor
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long vendorid { get; set; }

    [StringLength(50)]
    
    public string? vendorcode { get; set; }

    public long rfqID { get; set; }

    [StringLength(50)]
    
    public string? rfqNo { get; set; }

    public long? serviceid { get; set; }

    [StringLength(50)]
    
    public string? servicecode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? emaildate { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    public bool? isActive { get; set; }

    public long? jobmasterid { get; set; }

    [StringLength(500)]
    public string? link { get; set; }

    [StringLength(2000)]
    public string? remark { get; set; }

    public bool? isAttachQuotation { get; set; }

    public bool? isAttachTechDoc { get; set; }

    [StringLength(250)]
    
    public string? email { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? submitDate { get; set; }

    public long? userid { get; set; }

    [StringLength(50)]
    
    public string? jobstatus { get; set; }

    public long? workflowid { get; set; }

    public int? wlevel { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? submitBiddingDate { get; set; }

    [StringLength(250)]
    
    public string? submitBiddingBy { get; set; }

    public bool? isVendor { get; set; }

    [StringLength(250)]
    
    public string? NickName { get; set; }

    public bool? isBidWin { get; set; }

    public bool? isDocComplete { get; set; }

    [StringLength(250)]
    
    public string? ApproveBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApproveDate { get; set; }

    public bool? isTEComplete { get; set; }

    public long? teid { get; set; }

    [StringLength(250)]
    
    public string? remark_add { get; set; }

    public bool? isTePass { get; set; }

    [StringLength(250)]
    
    public string? ApproveTEBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApproveTEDate { get; set; }

    [StringLength(250)]
    
    public string? TeCondition { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? totalPrice { get; set; }

    [StringLength(2500)]
    public string? vendorComment { get; set; }

    public bool? isBidAllow { get; set; }

    [StringLength(250)]
    
    public string? PriceApproveBy { get; set; }

    public bool? isPriceApprove { get; set; }

    public bool? isRequstClarify { get; set; }

    public int? negoCount { get; set; }

    public bool? isRequisitionState { get; set; }

    [ForeignKey("rfqID")]
    [InverseProperty("pc_RFQServiceVendors")]
    public virtual pc_rfq rfq { get; set; } = null!;

    [ForeignKey("vendorid")]
    [InverseProperty("pc_RFQServiceVendors")]
    public virtual vd_general_info vendor { get; set; } = null!;
}
