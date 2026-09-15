using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ที่อยู่ของบริษัทลูกค้า (com_company) — 1 บริษัทมีได้หลายที่อยู่ แยกตามประเภท
// (สำนักงานใหญ่/ออกใบกำกับภาษี/จัดส่ง/สาขา) เพราะ com_company.address_HQ เป็นช่องเดียว
// ไม่พอเมื่อที่อยู่ออกใบกำกับภาษีต่างจาก HQ. ทรงตาราง mirror จาก vd_address
// (ระบบผู้ขายที่มีอยู่แล้ว, ดู hrm_vd_vendor_tables memory) — ดู [[advance-customer-account]].
[Table("com_cust_address")]
public class Com_CustAddress
{
    [Key]
    public long Id { get; set; }

    public long CompanyId { get; set; }   // FK -> com_company.id

    public CompanyAddressType AddressType { get; set; } = CompanyAddressType.HeadOffice;

    [StringLength(500)]
    public string? AddressNo { get; set; }

    [StringLength(200)]
    public string? Building { get; set; }

    [StringLength(200)]
    public string? Road { get; set; }

    [StringLength(200)]
    public string? SubDistrict { get; set; }

    [StringLength(200)]
    public string? District { get; set; }

    [StringLength(200)]
    public string? Province { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(CompanyId))]
    public virtual com_company Company { get; set; } = null!;
}
