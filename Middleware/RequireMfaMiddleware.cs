namespace HRM.Middleware;

using HRM.Data;
using HRM.Services.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

// บังคับ 2FA กับผู้ดูแลระบบ (12 ก.ย. 2569, ค้างจาก OWASP review) — เปิดด้วย SecurityAlerts:RequireMfaForAdmin = true
// รูปแบบเดียวกับ ForcePasswordChangeMiddleware: ผู้ใช้ login อยู่แล้ว แต่ถูกตรึงไว้ที่หน้าตั้งค่าแอปยืนยันตัวตน
// จนกว่าจะเปิด 2FA เสร็จ — "ผู้ดูแลระบบ" = ผู้ที่มีเมนู SYS_ADMIN (คนที่แก้สิทธิ์/เมนูของทุกคนได้)
// ตรวจสถานะ 2FA จากฐานข้อมูล (cache 60 วินาทีต่อคน) ไม่ใช่จาก claim เพื่อให้พ้นทันทีหลังตั้งค่าเสร็จโดยไม่ต้อง login ใหม่
public sealed class RequireMfaMiddleware(RequestDelegate next, IOptionsMonitor<SecurityAlertOptions> options, IMemoryCache cache)
{
    public const string SetupPath = "/Account/Manage/EnableAuthenticator";
    private static readonly string[] AlwaysAllowed =
    {
        "/Account/", "/logout", "/login", "/_blazor", "/_framework", "/_content", "/Error", "/force-change-password",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (options.CurrentValue.RequireMfaForAdmin
            && context.User?.Identity?.IsAuthenticated == true
            && context.User.HasClaim("menu", "SYS_ADMIN")
            && !IsAllowed(context.Request.Path))
        {
            var userId = context.User.FindFirst("sc_userid")?.Value ?? context.User.Identity.Name ?? "";
            var key = $"mfa:enabled:{userId}";
            if (!cache.TryGetValue(key, out bool enabled))
            {
                var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.GetUserAsync(context.User);
                enabled = user is not null && await userManager.GetTwoFactorEnabledAsync(user);
                cache.Set(key, enabled, TimeSpan.FromSeconds(enabled ? 300 : 20));
            }
            if (!enabled)
            {
                context.Response.Redirect($"{SetupPath}?reason=admin");
                return;
            }
        }
        await next(context);
    }

    private static bool IsAllowed(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value)) return false;
        foreach (var allowed in AlwaysAllowed)
            if (value.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) return true;
        var last = value.LastIndexOf('/');
        return last >= 0 && value.IndexOf('.', last) > last;   // static asset
    }
}
