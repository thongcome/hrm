namespace HRM.Services.Audit;

public interface IAuditLogger
{
    // For View events — a read never touches EF's ChangeTracker, so this is
    // the only way a "someone looked at this" event ever gets recorded.
    Task LogAccessAsync(string entityType, string? recordId, bool isSensitive, string? note = null, CancellationToken ct = default);

    // For explicit Create/Update/Delete logging at a specific call site,
    // when the automatic HRMContext.Audit.cs hook's lack of actor info
    // isn't good enough (e.g. this action needs old/new value capture with
    // a known actor attached).
    Task LogChangeAsync(HRM.Models.AuditActionType action, string entityType, string? recordId, object? oldValues, object? newValues, bool isSensitive, CancellationToken ct = default);
}
