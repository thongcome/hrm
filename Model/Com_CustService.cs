using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// หมวดธุรกิจ/อุตสาหกรรมของบริษัทลูกค้า แบบลำดับชั้น 4 ระดับ — mirror จาก vd_service
// (ของ vendor ใช้แท็กว่า "ขายอะไรให้เราได้บ้าง"; ของฝั่งลูกค้าใช้แท็กว่า "ลูกค้าอยู่ในธุรกิจ/
// อุตสาหกรรมอะไร" เพื่อวิเคราะห์กลุ่มลูกค้า — คนละความหมายกับ Entitlement
// "ลูกค้าซื้อ package ไหนของเราไปบ้าง" ซึ่งยังไม่มีระบบรองรับ ดู [[advance-customer-account]]).
// ยังไม่มีตาราง master สำหรับหมวดเหล่านี้ — เก็บเป็น code/name อิสระไปก่อนตามธรรมเนียม
// "Table config when unclear" จนกว่าจะรู้ชัดว่าต้องการ master list แบบไหน.
[Table("com_cust_service")]
public class Com_CustService
{
    [Key]
    public long Id { get; set; }

    public long CompanyId { get; set; }   // FK -> com_company.id

    [StringLength(100)]
    public string? Level1Code { get; set; }

    [StringLength(500)]
    public string? Level1Name { get; set; }

    [StringLength(100)]
    public string? Level2Code { get; set; }

    [StringLength(500)]
    public string? Level2Name { get; set; }

    [StringLength(100)]
    public string? Level3Code { get; set; }

    [StringLength(500)]
    public string? Level3Name { get; set; }

    [StringLength(100)]
    public string? Level4Code { get; set; }

    [StringLength(500)]
    public string? Level4Name { get; set; }

    [StringLength(2000)]
    public string? Remark { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(CompanyId))]
    public virtual com_company Company { get; set; } = null!;
}
