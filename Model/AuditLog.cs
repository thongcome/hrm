using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// System-wide audit trail — every Create/Update/Delete across HRMContext
// (via the SaveChangesAsync override in HRMContext.Audit.cs) plus explicit
// View events (login, payslip download, employee record open) logged by
// IAuditLogger at the few call sites that actually need PDPA access-logging.
// Kept separate from the payroll-specific Pay_PayrollAuditLog, which already
// covers workflow-transition detail well and has its own FK shape that
// can't describe changes outside the payroll module.
//
// Retention: Thai พ.ร.บ. คอมพิวเตอร์ มาตรา 26 requires computer traffic data
// (who accessed what, when, from where) be kept at least 90 days — any
// future cleanup/retention job must not purge rows younger than that.
[Table("AuditLog")]
[Index(nameof(EntityType), nameof(RecordId))]
[Index(nameof(EventDate))]
public class AuditLog
{
    [Key]
    public long Id { get; set; }

    // null when the automatic SaveChanges hook can't resolve an actor
    // (HRMContext isn't request-scoped) — explicit IAuditLogger calls
    // always populate this.
    public long? ActorUserId { get; set; }

    [StringLength(200)]
    public string? ActorName { get; set; }

    public DateTime EventDate { get; set; } = DateTime.Now;

    public AuditActionType Action { get; set; }

    [Required, StringLength(200)]
    public string EntityType { get; set; } = null!;

    [StringLength(100)]
    public string? RecordId { get; set; }

    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }

    // PDPA flag — true for reads/writes touching sensitive personal data
    // (IdCard, SalaryAmt, BankAccountNo, payslips, etc.)
    public bool IsSensitiveDataAccess { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
