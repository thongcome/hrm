using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// A reward employees can redeem points for ("วันหยุดพิเศษ 1 วัน", "บัตรกำนัล
// 500 บาท"). Config catalog maintained per company; PointsCost is what a
// redeem deducts from the employee's balance. Optional StockQty caps how many
// can be redeemed (null = unlimited). Company-scoped, soft-deleted.
[Table("Eng_RedeemItem")]
[Index(nameof(CompanyId))]
public class Eng_RedeemItem
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(20)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(30)]
    public string Code { get; set; } = null!;

    [Required, StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public int PointsCost { get; set; }

    // null = unlimited; otherwise how many remain available.
    public int? StockQty { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
