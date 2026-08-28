using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// รายชื่อธนาคาร — master กลางของทั้งระบบ (ไม่ผูก CompanyId เพราะรายชื่อธนาคารเหมือนกันทุกบริษัท)
// ported from legacy PIS "bankname" table concept (bankcode/bankname1) — ใช้เป็น
// dropdown ให้ Hrd_BankAccount เลือก แทนการพิมพ์ชื่อธนาคารเป็น free text ทุกครั้ง
[Table("Com_Bank")]
public class Com_Bank
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(10)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? NameEn { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
