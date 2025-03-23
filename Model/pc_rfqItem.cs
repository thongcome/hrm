using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pc_rfqItem")]
public partial class pc_rfqItem
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string rfq_no { get; set; } = null!;

    [StringLength(50)]
    
    public string? rfq_itemno { get; set; }

    [StringLength(50)]
    
    public string PRNo { get; set; } = null!;

    public int pr_itemno { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal amount { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
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

    public long? unit_type { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? unit_amount { get; set; }

    public string? remark_special { get; set; }

    [StringLength(50)]
    
    public string? ticketNo { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? qty { get; set; }

    public bool? isActive { get; set; }

    public long? rfqid { get; set; }

    public long? prid { get; set; }

    [StringLength(250)]
    
    public string? uom { get; set; }

    public bool? isSelect { get; set; }

    [StringLength(250)]
    
    public string? CURRENCY_KEY { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? VALUE_PRICE { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? PRICE_UNIT { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? TOTAL_VALUE { get; set; }

    [StringLength(250)]
    
    public string? requestor_empid { get; set; }

    [ForeignKey("rfqid")]
    [InverseProperty("pc_rfqItems")]
    public virtual pc_rfq? rfq { get; set; }
}
