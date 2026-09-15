using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ผลงาน/โครงการอ้างอิงของบริษัทลูกค้า — mirror จาก vd_portfolio. ดู [[advance-customer-account]].
[Table("com_cust_portfolio")]
public class Com_CustPortfolio
{
    [Key]
    public long Id { get; set; }

    public long CompanyId { get; set; }   // FK -> com_company.id

    [Required, StringLength(500)]
    public string Name { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [StringLength(1000)]
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
