using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// เอกสารประกอบของบริษัทลูกค้า (หนังสือรับรองบริษัท, ภ.พ.20, บอจ.5, สำเนาบัตรผู้มีอำนาจ
// ลงนาม ฯลฯ) — จำเป็นสำหรับออกใบกำกับภาษี/ใบเสร็จให้ถูกต้อง mirror จาก vd_doc.
// ดู [[advance-customer-account]].
[Table("com_cust_doc")]
public class Com_CustDoc
{
    [Key]
    public long Id { get; set; }

    public long CompanyId { get; set; }   // FK -> com_company.id

    [StringLength(200)]
    public string? DocType { get; set; }

    [StringLength(500)]
    public string? DocName { get; set; }

    public DateOnly? DocDate { get; set; }

    public DateOnly? DocExpireDate { get; set; }

    [StringLength(500)]
    public string? FileName { get; set; }

    [StringLength(1000)]
    public string? FilePath { get; set; }

    public bool IsMandatory { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(2000)]
    public string? Remark { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(CompanyId))]
    public virtual com_company Company { get; set; } = null!;
}
