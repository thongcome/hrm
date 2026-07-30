namespace HRM.Models;

// Generic, system-wide audit action — deliberately separate from
// PayAuditEventType (Pay_PayrollAuditLog), which only describes payroll
// workflow-state transitions. This enum covers ordinary data changes plus
// View, needed for PDPA access-logging (a read has no EF ChangeTracker
// entry, so it can never come from the SaveChanges hook — it's always
// logged explicitly via IAuditLogger.LogAccessAsync).
public enum AuditActionType
{
    Create,
    Update,
    Delete,
    View,
}
