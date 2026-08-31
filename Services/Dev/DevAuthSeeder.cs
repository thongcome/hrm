namespace HRM.Services.Dev;

using HRM.Data;
using Microsoft.AspNetCore.Identity;

// Development-only convenience so anyone (a new developer, an AI agent
// verifying a change) always has a known-working login without hand-editing
// the database or guessing a forgotten password. Every app startup in
// Development resets DevAdminEmail's password back to DevAdminPassword —
// idempotent by design, same "runs every startup, only actually does
// something the first/next time it's needed" idiom as the erp-web OIDC
// client seed in Program.cs. Never runs outside Development — see the
// IsDevelopment() gate around the call in Program.cs. Do not add an
// equivalent for Production; a hardcoded password is only acceptable
// because this code path is physically unreachable there.
public static class DevAuthSeeder
{
    public const string DevAdminEmail = "admin@local.humanok";
    public const string DevAdminPassword = "Dev@12345";

    // Non-admin counterpart to the admin account above, for verifying
    // ESS-only / non-admin behavior (e.g. that an admin-gated page actually
    // rejects a plain employee) without guessing or raw-SQL-resetting a
    // real person's password. sc_user 26 ("008", empid 008) already exists
    // with only the "Employee" role — no "Admin" — so this account is a
    // true non-admin fixture, not a stand-in that happens to also have
    // elevated access.
    public const string DevEssEmail = "ess.test008@hrm.local";
    public const string DevEssPassword = "Dev@12345";

    // Demo cast (added for live demos, 2026-08-31): every existing
    // test-account email here gets the same known password on startup, so
    // a demo can switch between several employee/supervisor personas
    // without anyone tracking per-account passwords. Only accounts whose
    // Identity email is a synthetic test address are listed — accounts
    // tied to a real person's own email (sawat@, thongcome@...) are
    // deliberately left alone so this never clobbers a password a human
    // actually set. test_payroll@hrm.local is also excluded on purpose:
    // the pre-login home page advertises its password as Test@12345, and
    // resetting it here would silently make that on-screen hint wrong.
    // See docs/HRM_Demo_Guide.md for who each account is in the demo.
    private static readonly string[] DemoEmployeeEmails =
    {
        "test005@hrm.local",     // 005 ปราณี สุขใจ — Employee+Admin (หัวหน้า/HR persona)
        "009@local.humanok",     // 009 นภัสสร วงศ์ษา — Employee only
        "010@local.humanok",     // 010 ประยุทธ ชัยมงคล — Employee only
        "012@local.humanok",     // 012 ชาญวิทย์ บุญมี — Employee+Admin
    };

    public static async Task EnsureKnownDevPasswordAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await ResetPasswordAsync(userManager, DevAdminEmail, DevAdminPassword);
        await ResetPasswordAsync(userManager, DevEssEmail, DevEssPassword);
        foreach (var email in DemoEmployeeEmails)
            await ResetPasswordAsync(userManager, email, DevEssPassword);
    }

    private static async Task ResetPasswordAsync(UserManager<ApplicationUser> userManager, string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return; // Account itself (and its sc_user/role/menu linkage) is expected to already exist — this only resets the password, never creates the account.

        if (await userManager.HasPasswordAsync(user))
            await userManager.RemovePasswordAsync(user);
        await userManager.AddPasswordAsync(user, password);
    }
}
