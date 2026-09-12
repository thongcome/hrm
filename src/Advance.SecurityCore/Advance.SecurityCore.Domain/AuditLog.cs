using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Advance.SecurityCore.Domain;

// Generic, system-wide audit action. Copied from HRM's Model/AuditEnums.cs.
// Kept separate from any product-specific audit enum (e.g. HRM's
// PayAuditEventType for Pay_PayrollAuditLog) — SecurityCore only knows about
// this generic one.
public enum AuditActionType
{
    Create,
    Update,
    Delete,
    View,
}

// Copied from HRM's Model/AuditLog.cs (D:\GitWorkspace\HRM\Model\AuditLog.cs) verbatim —
// this table has no HRM-specific columns at all, so no adaptation was needed.
//
// Retention: Thai พ.ร.บ. คอมพิวเตอร์ มาตรา 26 requires at least 90 days of
// actor+IP+timestamp for sensitive access — do not add any purge/cleanup job
// that deletes rows younger than that (see CLAUDE.md's audit-logging rule,
// which applies here unchanged).
[Table("AuditLog")]
[Index(nameof(EntityType), nameof(RecordId))]
[Index(nameof(EventDate))]
public class AuditLog
{
    [Key]
    public long Id { get; set; }

    // null when the automatic SaveChanges hook can't resolve an actor;
    // explicit IAuditLogger calls always populate this.
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

    // PDPA flag — true for reads/writes touching sensitive personal data.
    public bool IsSensitiveDataAccess { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
