namespace HRM.Services.Dev;

using HRM.Data;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Dev-only scaffolding for the CONFIG-DRIVEN workflow-button work (owner request
// 2026-09-03). Companion to Services/Workflow/WorkflowButtonService.cs.
//
// The action buttons on the approval panel are being moved OUT of hardcoded Razor and
// INTO the legacy epms button tables (wf_button_master = definitions, wf_button = the
// per-workflow/level mapping). This seeder plants the core BUTTON DEFINITIONS so a fresh
// dev DB has something for the service/admin to work with, mirroring the epms seed:
//
//     Approve      → Success / btn btn-success   → ActionKind "approve"
//     Send Back    → Warning / btn btn-warning   → ActionKind "sendback"   (reject / return)
//     Decline      → Error   / btn btn-danger    → ActionKind "decline"
//
// It deliberately seeds ONLY wf_button_master. It does NOT seed wf_button mapping rows —
// which button applies at which workflow/level is an admin decision, and
// WorkflowButtonService.GetButtonsForLevelAsync already returns an EMPTY list (safe
// fallback to the built-in buttons) while nothing is mapped. So this seeder alone changes
// no runtime behaviour; it just makes the definitions available to configure.
//
// Idempotent: does nothing if wf_button_master already has rows. Never runs outside
// Development (gated by IsDevelopment() at the Program.cs call site), same as
// WorkflowAutoApproveSeeder. Writes go through EF so they are audit-logged (no raw SQL).
public static class WorkflowButtonSeeder
{
    // Seed definition: (code, label, cssClass, actiontypecode, orderth). actiontypecode
    // is what WorkflowButtonService.MapActionKind() reads to bucket the button into
    // approve / sendback / decline — keep these codes aligned with that mapping.
    private static readonly (string Code, string Label, string ClassStyle, string ActionType, int Order)[] CoreButtons =
    {
        ("approve", "Approve",   "btn btn-success", "approve", 1),
        ("reject",  "Send Back", "btn btn-warning", "reject",  2), // reject == send-back / return to requester
        ("decline", "Decline",   "btn btn-danger",  "decline", 3),
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var ctx = await dbFactory.CreateDbContextAsync();

        // Idempotent guard: only seed when the master table is completely empty, so we
        // never fight an admin who has since curated the definitions.
        if (await ctx.wf_button_masters.AnyAsync()) return;

        var now = DateTime.Now;
        foreach (var b in CoreButtons)
        {
            ctx.wf_button_masters.Add(new wf_button_master
            {
                name = b.Label,
                code = b.Code,
                value = b.Label,           // value == the display label the UI shows
                class_style = b.ClassStyle,
                actiontypecode = b.ActionType,
                btnType = "submit",
                orderth = b.Order,
                moddate = now,
                modby = "WorkflowButtonSeeder",
            });
        }

        await ctx.SaveChangesAsync();
    }
}
