using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Employee documents (ID card, contract, resignation letter, etc.) attached
// from the simplified Payroll employee admin page. Files are stored on disk
// via PrivateFileStorage (category "employee-docs"), same pattern as
// payslips/bank/GL exports — this table just tracks the metadata.
[Table("Pay_EmployeeDocument")]
public class Pay_EmployeeDocument
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    public Pay_EmployeeDocumentType DocumentType { get; set; }

    [Required, StringLength(260)]
    public string FileName { get; set; } = null!;

    [Required, StringLength(500)]
    public string StoragePath { get; set; } = null!;

    [Required, StringLength(64)]
    public string Sha256 { get; set; } = null!;

    public long UploadedByUserId { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.Now;

    // Only meaningful for document types that actually expire (work permit,
    // visa, driving license, professional license) — left null for types
    // like education certificates that never expire. HR can set it on any
    // type though; nothing enforces which types require it.
    public DateOnly? ExpiryDate { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public virtual Hremployee Hremployee { get; set; } = null!;
}
