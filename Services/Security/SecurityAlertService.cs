namespace HRM.Services.Security;

using HRM.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

// การแจ้งเตือนเหตุการณ์ด้านความปลอดภัย (OWASP A09 — ค้างจากรอบทบทวน 1 ก.ย. 2569; CEO 12 ก.ย. 2569 "ทำให้ครบเลย")
//   เดิมระบบ "บันทึก" login ล้มเหลว/ล็อกบัญชี/สิทธิ์ถูกปฏิเสธ ลง AuditLog แต่ไม่มีใครรู้จนกว่าจะเปิดดูเอง
//   ตอนนี้: นับเหตุการณ์ในกรอบเวลา เกินเกณฑ์ → เขียนแถว AuditLog ชนิด SecurityAlert (ดูได้ในหน้า audit เดิม)
//   + ส่งอีเมลผู้ดูแลตาม SecurityAlerts:Recipients (กันซ้ำต่อชนิด+เป้าหมายตาม ThrottleMinutes)
//   ทุกอย่างเป็น config ใน appsettings ส่วน "SecurityAlerts" — ปิดได้ ปรับเกณฑ์ได้ ไม่ต้อง build
public sealed class SecurityAlertOptions
{
    public const string SectionName = "SecurityAlerts";
    public bool Enabled { get; set; } = true;
    public string[] Recipients { get; set; } = Array.Empty<string>();
    public int FailedLoginThreshold { get; set; } = 3;      // ครั้ง/บัญชี หรือ ×3 ต่อ IP
    public int FailedLoginWindowMinutes { get; set; } = 15;
    public int AccessDeniedThreshold { get; set; } = 5;     // ครั้ง/ผู้ใช้
    public int AccessDeniedWindowMinutes { get; set; } = 10;
    public int ThrottleMinutes { get; set; } = 30;          // อีเมลซ้ำชนิดเดิม เป้าหมายเดิม ไม่เกินหนึ่งฉบับต่อช่วงนี้
    // บังคับ 2FA (แอปยืนยันตัวตน) กับผู้ดูแลระบบ (ผู้ที่มีเมนู SYS_ADMIN) — ค่าเริ่มต้นปิด: เปิดเมื่อลูกค้าพร้อมตั้งค่าแอป
    public bool RequireMfaForAdmin { get; set; } = false;
}

// ตัวนับล้วน ๆ ทดสอบได้: คืน true เมื่อครั้งนี้ทำให้ถึงเกณฑ์พอดี (แจ้งครั้งเดียวต่อกรอบเวลา ไม่แจ้งทุกครั้งที่เกิน)
public sealed class SecurityAlertCounter(IMemoryCache cache)
{
    public int Hit(string key, TimeSpan window)
    {
        var entry = cache.GetOrCreate(key, e => { e.AbsoluteExpirationRelativeToNow = window; return new Counter(); })!;
        return Interlocked.Increment(ref entry.Value);
    }

    public bool ReachedNow(string key, TimeSpan window, int threshold) => threshold > 0 && Hit(key, window) == threshold;

    public bool Throttled(string key, TimeSpan period)
    {
        if (cache.TryGetValue(key, out _)) return true;
        cache.Set(key, true, period);
        return false;
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
            await RaiseAsync("lockout", user, $"บัญชี {user} ถูกล็อกจากการใส่รหัสผ่านผิดเกินกำหนด (IP {ip ?? "?"})", ip, user);
            return;
        }
        if (user.Length > 0 && counter.ReachedNow($"sec:lf:user:{user}", window, O.FailedLoginThreshold))
            await RaiseAsync("failed-logins", user, $"บัญชี {user} ใส่รหัสผ่านผิด {O.FailedLoginThreshold} ครั้งภายใน {O.FailedLoginWindowMinutes} นาที (IP {ip ?? "?"})", ip, user);
        if (!string.IsNullOrWhiteSpace(ip) && counter.ReachedNow($"sec:lf:ip:{ip}", window, O.FailedLoginThreshold * 3))
            await RaiseAsync("failed-logins-ip", ip!, $"IP {ip} ใส่รหัสผ่านผิด {O.FailedLoginThreshold * 3} ครั้งภายใน {O.FailedLoginWindowMinutes} นาที (หลายบัญชี) — อาจเป็นการเดารหัส", ip, null);
    }

    public async Task OnAccessDeniedAsync(string? username, long? userId, string path)
    {
        if (!O.Enabled) return;
        var user = string.IsNullOrWhiteSpace(username) ? (userId?.ToString() ?? "anonymous") : username.Trim();
        var window = TimeSpan.FromMinutes(Math.Max(1, O.AccessDeniedWindowMinutes));
        if (counter.ReachedNow($"sec:ad:user:{user}", window, O.AccessDeniedThreshold))
            await RaiseAsync("access-denied", user, $"ผู้ใช้ {user} พยายามเปิดหน้าที่ไม่มีสิทธิ์ {O.AccessDeniedThreshold} ครั้งภายใน {O.AccessDeniedWindowMinutes} นาที (ล่าสุด {path})", null, user, userId);
    }

    // เขียน AuditLog เสมอ (หลักฐานตาม พ.ร.บ.คอมพิวเตอร์) · อีเมลเฉพาะเมื่อไม่ถูกกันซ้ำ · ห้ามโยน exception ออกไปกระทบ login
    private async Task RaiseAsync(string kind, string target, string message, string? ip, string? actorName, long? actorUserId = null)
    {
        try
        {
            logger.LogWarning("SecurityAlert {Kind} {Target}: {Message}", kind, target, message);
            using var scope = scopes.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
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
            var subject = $"[HumanOk Security] {kind} — {target}";
            var body = $"<p>{System.Net.WebUtility.HtmlEncode(message)}</p><p>เวลา {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p><p>ดูรายละเอียดในบันทึกการใช้งาน (AuditLog ประเภท SecurityAlert)</p>";
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
