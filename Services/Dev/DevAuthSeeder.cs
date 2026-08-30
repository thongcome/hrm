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

    public static async Task EnsureKnownDevPasswordAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(DevAdminEmail);
        if (user is null)
            return; // Account itself (and its sc_user/role/menu linkage) is expected to already exist — this only resets the password, never creates the account.

        if (await userManager.HasPasswordAsync(user))
            await userManager.RemovePasswordAsync(user);
        await userManager.AddPasswordAsync(user, DevAdminPassword);
    }
}
