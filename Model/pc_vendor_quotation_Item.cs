using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pc_vendor_quotation_Item")]
public partial class pc_vendor_quotation_Item
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string quotation_no_ref { get; set; } = null!;

    [StringLength(50)]
    
    public string rfq_no { get; set; } = null!;

    [StringLength(50)]
    
    public string? rfq_itemno { get; set; }

    [StringLength(50)]
    
    public string PRNo { get; set; } = null!;

    public int pr_itemno { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? amount { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? net_amount { get; set; }

    public string? descript { get; set; }

    [StringLength(50)]
    
    public string? type { get; set; }

    [StringLength(50)]
    
    public string? mat_code { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public string? remark { get; set; }

    public long? unit_type { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? unit_amount { get; set; }

    public string? remark_special { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? nego_price { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? nego_amount { get; set; }

    public bool? isActive { get; set; }

    public long? prid { get; set; }

    public long? rfqid { get; set; }

    public long? vendorid { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? qty { get; set; }

    [StringLength(250)]
    
    public string? uom { get; set; }

    public long? quotationID { get; set; }

    [StringLength(250)]
    
    public string? ApproverPrice { get; set; }

    public bool isApproverTech { get; set; }

    public bool isApprovePrice { get; set; }

    public bool? isPriceAction { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DateApproverTech { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DateApproverPrice { get; set; }

    public long? ApproverUserID { get; set; }

    public long? ApproverUserIDPrice { get; set; }

    [StringLength(250)]
    
    public string? ApproverUserPrice { get; set; }

    [StringLength(250)]
    
    public string? NextApproverUserPrice { get; set; }

    [StringLength(250)]
    
    public string? requestor_empid { get; set; }

    [StringLength(250)]
    
    public string? NextApprover { get; set; }

    public bool? isRequisitionerState { get; set; }

    public bool? isTechAction { get; set; }

    [StringLength(250)]
    
    public string? ApproverTech { get; set; }

    public bool isApproverPrice { get; set; }

    [StringLength(250)]
    
    public string? CURRENCY_KEY { get; set; }

    public bool? isPriceRequisitionState { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? budget { get; set; }
}
