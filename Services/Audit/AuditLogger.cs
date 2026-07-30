using System.Text.Json;
using HRM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Audit;

public class AuditLogger : IAuditLogger
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogger(IDbContextFactory<HRMContext> dbFactory, IHttpContextAccessor httpContextAccessor)
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
