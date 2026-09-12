namespace Advance.SecurityCore.Services;

using Advance.SecurityCore.Data;
using Advance.SecurityCore.Domain;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Copied from HRM's Services/Security/SecurityAlertService.cs — fully
// generic already (config-driven thresholds, writes to the generic
// AuditLog table, emails via the generic IEmailSender interface). Only
// change: HRMContext -> SecurityDbContext.
//
// RequireMfaForAdmin (used by RequireMfaMiddleware) checks for a "menu"
// claim of "SYS_ADMIN" specifically — that string is an HRM convention, not
// a SecurityCore concept. Left as-is for now (see EXTRACTION-PLAN.md); a
// later version could make the gating menucode configurable instead of
// hardcoded.
public sealed class SecurityAlertOptions
{
    public const string SectionName = "SecurityAlerts";
    public bool Enabled { get; set; } = true;
    public string[] Recipients { get; set; } = Array.Empty<string>();
    public int FailedLoginThreshold { get; set; } = 3;
    public int FailedLoginWindowMinutes { get; set; } = 15;
    public int AccessDeniedThreshold { get; set; } = 5;
    public int AccessDeniedWindowMinutes { get; set; } = 10;
    public int ThrottleMinutes { get; set; } = 30;
    public bool RequireMfaForAdmin { get; set; } = false;
}

public sealed class SecurityAlertCounter(IMemoryCache cache)
{
    private readonly object _gate = new();

    public int Hit(string key, TimeSpan window)
    {
        lock (_gate)
        {
            var entry = cache.GetOrCreate(key, e => { e.AbsoluteExpirationRelativeToNow = window; return new Counter(); })!;
            return ++entry.Value;
        }
    }

    public bool ReachedNow(string key, TimeSpan window, int threshold) => threshold > 0 && Hit(key, window) == threshold;

    public bool Throttled(string key, TimeSpan period)
    {
        lock (_gate)
        {
            if (cache.TryGetValue(key, out _)) return true;
            cache.Set(key, true, period);
            return false;
        }
    }

    private sealed class Counter { public int Value; }
}

public sealed class SecurityAlertService(
    SecurityAlertCounter counter,
    IOptionsMonitor<SecurityAlertOptions> options,
    IServiceScopeFactory scopes,
    ILogger<SecurityAlertService> logger)
{
    public const string EntityType = "SecurityAlert";

    private SecurityAlertOptions O => options.CurrentValue;

    public async Task OnLoginFailedAsync(string username, string? ip, bool lockedOut)
    {
        if (!O.Enabled) return;
        var window = TimeSpan.FromMinutes(Math.Max(1, O.FailedLoginWindowMinutes));
        var user = (username ?? "").Trim().ToLowerInvariant();
        if (lockedOut)
        {
            await RaiseAsync("lockout", user, $"Account {user} locked out from too many failed password attempts (IP {ip ?? "?"})", ip, user);
            return;
        }
        if (user.Length > 0 && counter.ReachedNow($"sec:lf:user:{user}", window, O.FailedLoginThreshold))
            await RaiseAsync("failed-logins", user, $"Account {user} failed password {O.FailedLoginThreshold} times within {O.FailedLoginWindowMinutes} minutes (IP {ip ?? "?"})", ip, user);
        if (!string.IsNullOrWhiteSpace(ip) && counter.ReachedNow($"sec:lf:ip:{ip}", window, O.FailedLoginThreshold * 3))
            await RaiseAsync("failed-logins-ip", ip!, $"IP {ip} failed password {O.FailedLoginThreshold * 3} times within {O.FailedLoginWindowMinutes} minutes (multiple accounts) — possible password guessing", ip, null);
    }

    public async Task OnAccessDeniedAsync(string? username, long? userId, string path)
    {
        if (!O.Enabled) return;
        var user = string.IsNullOrWhiteSpace(username) ? (userId?.ToString() ?? "anonymous") : username.Trim();
        var window = TimeSpan.FromMinutes(Math.Max(1, O.AccessDeniedWindowMinutes));
        if (counter.ReachedNow($"sec:ad:user:{user}", window, O.AccessDeniedThreshold))
            await RaiseAsync("access-denied", user, $"User {user} tried to open a page they don't have rights to {O.AccessDeniedThreshold} times within {O.AccessDeniedWindowMinutes} minutes (latest {path})", null, user, userId);
    }

    private async Task RaiseAsync(string kind, string target, string message, string? ip, string? actorName, long? actorUserId = null)
    {
        try
        {
            logger.LogWarning("SecurityAlert {Kind} {Target}: {Message}", kind, target, message);
            using var scope = scopes.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecurityDbContext>>();
            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                db.AuditLogs.Add(new AuditLog
                {
                    ActorUserId = actorUserId,
                    ActorName = actorName,
                    Action = AuditActionType.View,
                    EntityType = EntityType,
                    RecordId = target,
                    IsSensitiveDataAccess = false,
                    IpAddress = ip,
                    Note = $"{kind}: {message}",
                });
                await db.SaveChangesAsync();
            }

            if (O.Recipients.Length == 0) return;
            if (counter.Throttled($"sec:throttle:{kind}:{target}", TimeSpan.FromMinutes(Math.Max(1, O.ThrottleMinutes)))) return;
            var mail = scope.ServiceProvider.GetService<IEmailSender>();
            if (mail is null) return;
            var subject = $"[Advance.SecurityCore] {kind} — {target}";
            var body = $"<p>{System.Net.WebUtility.HtmlEncode(message)}</p><p>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</p><p>See AuditLog (EntityType SecurityAlert) for details.</p>";
            foreach (var to in O.Recipients.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                try { await mail.SendEmailAsync(to.Trim(), subject, body); }
                catch (Exception ex) { logger.LogError(ex, "SecurityAlert mail to {To} failed", to); }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SecurityAlert {Kind} could not be recorded", kind);
        }
    }
}
