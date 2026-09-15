using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ใบรับรองมาตรฐาน/คุณภาพของบริษัทลูกค้า (เช่น ISO) — ข้อมูลเสริมที่ลูกค้าอาจยินดีให้
// mirror จาก vd_certificate. ดู [[advance-customer-account]].
[Table("com_cust_certificate")]
public class Com_CustCertificate
{
    [Key]
    public long Id { get; set; }

    public long CompanyId { get; set; }   // FK -> com_company.id

    [Required, StringLength(500)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string? StandCode { get; set; }

    [StringLength(500)]
    public string? IssueBy { get; set; }

    public int? IssueYear { get; set; }

    public int? ExpireYear { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? FileName { get; set; }

    [StringLength(1000)]
    public string? FilePath { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(CompanyId))]
    public virtual com_company Company { get; set; } = null!;
}
