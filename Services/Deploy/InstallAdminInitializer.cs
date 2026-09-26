using System.Security.Cryptography;
using HRM.Data;
using HRM.Models;
using HRM.Services.Login;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Deploy;

// `dotnet HRM.dll --init-admin` — the installer's step that gives the system account
// `advadmin` a password on a freshly installed customer database (advadmin is always kept in the
// system for installing; the clean template carries no password we know). A random password is
// generated, printed ONCE to the console, and the account is flagged to change it at first
// sign-in (ForcePasswordChangeMiddleware). Nothing about it is logged or stored in plain text.
// The web server does not start in this mode — see tools/deploy/README.md for the full install flow.
public static class InstallAdminInitializer
{
    public const string AdminLogin = "advadmin";
    public const string CommandLineSwitch = "--init-admin";

    public static async Task<int> RunAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var dbFactory = sp.GetRequiredService<IDbContextFactory<HRMContext>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var scUser = await db.sc_users.FirstOrDefaultAsync(u => u.loginname == AdminLogin);
        if (scUser is null)
        {
            Console.Error.WriteLine($"ไม่พบบัญชี {AdminLogin} ในฐานข้อมูลนี้ — ฐานต้องสร้างจากฐานแม่แบบ (tools/deploy)");
            return 1;
        }

        var password = NewPassword();
        var appUser = await userManager.Users.FirstOrDefaultAsync(u => u.userid == scUser.userid);
        if (appUser is null)
        {
            var result = await sp.GetRequiredService<UserProvisioningService>().EnsureIdentityLinkedAsync(scUser, password);
            appUser = await userManager.Users.FirstOrDefaultAsync(u => u.userid == scUser.userid);
            if (appUser is null)
            {
                Console.Error.WriteLine($"สร้างบัญชีเข้าระบบของ {AdminLogin} ไม่สำเร็จ: {result}");
                return 1;
            }
        }
        else
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(appUser);
            var reset = await userManager.ResetPasswordAsync(appUser, token, password);
            if (!reset.Succeeded)
            {
                Console.Error.WriteLine("ตั้งรหัสผ่านไม่สำเร็จ: " + string.Join("; ", reset.Errors.Select(e => e.Description)));
                return 1;
            }
        }

        await userManager.SetLockoutEndDateAsync(appUser, null);
        await userManager.ResetAccessFailedCountAsync(appUser);

        scUser.isforcechanged = true;   // must be replaced at first sign-in
        scUser.isdisable = false;
        scUser.iscancel = false;
        scUser.invalidpwcount = 0;
        await db.SaveChangesAsync();

        Console.WriteLine();
        Console.WriteLine("==============================================================");
        Console.WriteLine($" บัญชีผู้ดูแลระบบ : {AdminLogin}");
        Console.WriteLine($" รหัสผ่านชั่วคราว  : {password}");
        Console.WriteLine(" แสดงครั้งเดียวเท่านั้น — ระบบจะบังคับให้เปลี่ยนเมื่อเข้าระบบครั้งแรก");
        Console.WriteLine("==============================================================");
        return 0;
    }

    // 16 characters, always containing upper, lower, digit and symbol (the Identity policy).
    private static string NewPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", lower = "abcdefghijkmnpqrstuvwxyz", digits = "23456789", symbols = "!@#$%*?";
        var all = upper + lower + digits + symbols;
        var chars = new List<char>
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            symbols[RandomNumberGenerator.GetInt32(symbols.Length)],
        };
        while (chars.Count < 16) chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }
}
