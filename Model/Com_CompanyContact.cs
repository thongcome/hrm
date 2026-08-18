using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// บุคคลติดต่อของบริษัท (com_company) — 1 บริษัทมีได้หลายคน แยกตามประเภท
// (ทั่วไป/การเงิน/เทคนิค/ผู้มีอำนาจตัดสินใจ) เพื่อให้รู้ว่าเรื่องเก็บเงินหรือเสนอขายเพิ่ม
// ต้องติดต่อใคร. ข้อมูลนี้เป็นข้อมูลส่วนบุคคล (ชื่อ/เบอร์โทร/อีเมล) — ต้องมีป้าย PDPA
// และ audit access log ตอนเปิดดู.
[Table("Com_CompanyContact")]
public class Com_CompanyContact
{
    [Key]
    public long Id { get; set; }

    public long CompanyId { get; set; }   // FK -> com_company.id

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(150)]
    public string? Position { get; set; }     // ตำแหน่ง

    [StringLength(150)]
    public string? Department { get; set; }   // แผนก

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(50)]
    public string? Mobile { get; set; }

    [StringLength(250)]
    public string? Email { get; set; }

    public CompanyContactType ContactType { get; set; } = CompanyContactType.General;

    public bool IsPrimary { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(CompanyId))]
    public virtual com_company Company { get; set; } = null!;
}
