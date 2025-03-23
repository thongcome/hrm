using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pc_pr_item")]
[Index("PRNo", Name = "IX_pc_pr_item")]
public partial class pc_pr_item
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string PRNo { get; set; } = null!;

    public int itemno { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal amount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal net_amount { get; set; }

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

    public long? mas_unit_type_id { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? unit_amount { get; set; }

    public long? currencyID { get; set; }

    [StringLength(250)]
    
    public string? SAPRequistiner { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? qty { get; set; }

    [StringLength(250)]
    
    public string? trackingNo { get; set; }

    [StringLength(250)]
    
    public string? file1 { get; set; }

    [StringLength(250)]
    
    public string? file2 { get; set; }

    [StringLength(250)]
    
    public string? file1_path { get; set; }

    [StringLength(250)]
    
    public string? file2_path { get; set; }

    public bool? isAvailable { get; set; }

    public long? pc_pr_id { get; set; }

    [StringLength(250)]
    
    public string? SAPstatus { get; set; }

    [StringLength(250)]
    
    public string? DOC_TYPE { get; set; }

    [StringLength(250)]
    
    public string? DOC_CATEGORY { get; set; }

    [StringLength(250)]
    
    public string? PLANT { get; set; }

    [StringLength(250)]
    
    public string? PR_GROUP { get; set; }

    [StringLength(250)]
    
    public string? SHORT_TEXT { get; set; }

    [StringLength(250)]
    
    public string? ITEM_CATEGORY { get; set; }

    [StringLength(250)]
    
    public string? UOM { get; set; }

    [StringLength(250)]
    
    public string? REQUEST_DATE { get; set; }

    [StringLength(250)]
    
    public string? RELEASE_DATE { get; set; }

    [StringLength(250)]
    
    public string? VALUE_PRICE { get; set; }

    [StringLength(250)]
    
    public string? PRICE_UNIT { get; set; }

    [StringLength(250)]
    
    public string? CURRENCY_KEY { get; set; }

    [StringLength(250)]
    
    public string? RELEASE_STATUS { get; set; }

    [StringLength(250)]
    
    public string? TOTAL_VALUE { get; set; }

    [StringLength(250)]
    
    public string? COST_CENTER { get; set; }

    public string? ITEM_TXT { get; set; }

    public string? MATERIAL_PO_TXT { get; set; }

    public long? rfqid { get; set; }

    public long? rfq_itemID { get; set; }

    public bool isSelect { get; set; }

    [StringLength(250)]
    
    public string? requestor_empid { get; set; }

    [ForeignKey("pc_pr_id")]
    [InverseProperty("pc_pr_items")]
    public virtual pc_pr? pc_pr { get; set; }
}
