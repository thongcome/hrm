using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// งบการเงินของบริษัทลูกค้า ปีละ 1 แถว — ไว้ใช้ประกอบการพิจารณาเครดิตเทอม
// (com_company.PaymentTermsDays มีอยู่แล้วแต่ไม่มีข้อมูลรองรับการตัดสินใจ)
// mirror จาก vd_financial. ดู [[advance-customer-account]].
[Table("com_cust_financial")]
public class Com_CustFinancial
{
    [Key]
    public long Id { get; set; }

    public long CompanyId { get; set; }   // FK -> com_company.id

    public int FiscalYear { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Income { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Profit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Loss { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal? WorkingCapitalRatio { get; set; }

    [StringLength(500)]
    public string? FileName { get; set; }

    [StringLength(1000)]
    public string? FilePath { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(CompanyId))]
    public virtual com_company Company { get; set; } = null!;
}
