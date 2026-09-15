using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ผู้มีอำนาจลงนามผูกพันบริษัทลูกค้า (สัญญาซื้อขาย/ใบอนุญาตใช้งานซอฟต์แวร์) — สำคัญทาง
// กฎหมาย ต้องรู้ว่าใครเซ็นสัญญาแทนบริษัทได้บ้าง mirror จาก vd_signed.
// ดู [[advance-customer-account]].
[Table("com_cust_signed")]
public class Com_CustSigned
{
    [Key]
    public long Id { get; set; }

    public long CompanyId { get; set; }   // FK -> com_company.id

    [StringLength(500)]
    public string? SignedName { get; set; }

    [StringLength(500)]
    public string? SignedPosition { get; set; }

    public DateTime? SignedDate { get; set; }

    public long? RecordedByUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(CompanyId))]
    public virtual com_company Company { get; set; } = null!;
}
