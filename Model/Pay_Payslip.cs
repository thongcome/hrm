using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// One generated payslip per Pay_PayrollEmployee. PdfStoragePath points into
// private storage (App_Data, NOT wwwroot) — files are only ever served
// through an authorized minimal-API endpoint, never as static files.
[Table("Pay_Payslip")]
[Index(nameof(PayrollEmployeeId), IsUnique = true)]
public class Pay_Payslip
{
    [Key]
    public long Id { get; set; }

    public long PayrollEmployeeId { get; set; }

    public DateTime GeneratedDate { get; set; } = DateTime.Now;

    [Required, StringLength(500)]
    public string PdfStoragePath { get; set; } = null!;

    [StringLength(64)]
    public string? PdfSha256 { get; set; }

    public bool IsPublishedToEmployee { get; set; }
    public DateTime? PublishedDate { get; set; }

    public virtual Pay_PayrollEmployee Pay_PayrollEmployee { get; set; } = null!;
}
