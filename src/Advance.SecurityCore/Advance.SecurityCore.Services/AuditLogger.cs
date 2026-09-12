namespace Advance.SecurityCore.Services;

using System.Text.Json;
using Advance.SecurityCore.Data;
using Advance.SecurityCore.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

// Copied from HRM's Services/Audit/AuditLogger.cs — fully generic already,
// only change is HRMContext -> SecurityDbContext.
//
// NOTE: HRM's automatic write-audit hook (Model/HRMContext.Audit.cs, which
// overrides SaveChangesAsync to log every Create/Update/Delete without any
// call site needing to do anything) is NOT copied here — that logic lives on
// HRMContext itself, not as a separate service, so "copying IAuditLogger/
// AuditLogger" does not by itself give a host built on SecurityDbContext the
// automatic part of HRM's audit story. See EXTRACTION-PLAN.md's audit
// section: a SecurityDbContext.SaveChangesAsync override doing the same
// ChangeTracker-walk is flagged as follow-up work, not done in this pass.
// AuditMasking.cs (mentioned in the task prompt as "if a background agent
// added it") does not exist anywhere in the HRM repo as of this extraction —
// confirmed by search; nothing to copy.
public class AuditLogger : IAuditLogger
{
    private readonly IDbContextFactory<SecurityDbContext> _dbFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogger(IDbContextFactory<SecurityDbContext> dbFactory, IHttpContextAccessor httpContextAccessor)
    {
        _dbFactory = dbFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAccessAsync(string entityType, string? recordId, bool isSensitive, string? note = null, CancellationToken ct = default)
    {
        var (actorUserId, actorName, ipAddress) = ResolveActor();

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            ActorName = actorName,
            Action = AuditActionType.View,
            EntityType = entityType,
            RecordId = recordId,
            IsSensitiveDataAccess = isSensitive,
            IpAddress = ipAddress,
            Note = note,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task LogChangeAsync(AuditActionType action, string entityType, string? recordId, object? oldValues, object? newValues, bool isSensitive, CancellationToken ct = default)
    {
        var (actorUserId, actorName, ipAddress) = ResolveActor();

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            ActorName = actorName,
            Action = action,
            EntityType = entityType,
            RecordId = recordId,
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues),
            IsSensitiveDataAccess = isSensitive,
            IpAddress = ipAddress,
        });
        await context.SaveChangesAsync(ct);
    }

    private (long? actorUserId, string? actorName, string? ipAddress) ResolveActor()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        long? actorUserId = null;
        var idClaim = user?.FindFirst("sc_userid")?.Value;
        if (long.TryParse(idClaim, out var id)) actorUserId = id;

        var actorName = user?.Identity?.Name;
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

        return (actorUserId, actorName, ipAddress);
    }
}
