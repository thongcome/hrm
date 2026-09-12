namespace Advance.SecurityCore.Services;

using Advance.SecurityCore.Domain;

// Copied verbatim from HRM's Services/Audit/IAuditLogger.cs.
public interface IAuditLogger
{
    // For View events — a read never touches EF's ChangeTracker, so this is
    // the only way a "someone looked at this" event ever gets recorded.
    Task LogAccessAsync(string entityType, string? recordId, bool isSensitive, string? note = null, CancellationToken ct = default);

    // For explicit Create/Update/Delete logging at a specific call site.
    Task LogChangeAsync(AuditActionType action, string entityType, string? recordId, object? oldValues, object? newValues, bool isSensitive, CancellationToken ct = default);
}
