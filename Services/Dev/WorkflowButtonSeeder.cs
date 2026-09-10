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
        ("submit",  "ส่งต่อ",     "btn btn-primary", "submit",  1), // ขั้นกลาง: ส่งไปขั้นถัดไป
        ("approve", "อนุมัติ",    "btn btn-success", "approve", 2), // ขั้นสุดท้าย: อนุมัติแล้วงานจบ
        ("reject",  "ส่งกลับ",    "btn btn-warning", "reject",  3), // ให้กลับไปแก้แล้วส่งใหม่
        ("decline", "ไม่อนุมัติ", "btn btn-danger",  "decline", 4), // ปฏิเสธและปิดเรื่อง
    };

    // ปุ่มไหนขึ้นที่ขั้นแบบไหน — ชุดกลางที่ใช้กับทุก workflow (workflowid = null)
    // ตรงกับที่ epms ทำจริงใน production: ขั้นกลางส่งต่อ/ส่งกลับได้ ส่วนการปฏิเสธ
    // ถาวรมีเฉพาะขั้นสุดท้าย เพราะคนที่ยังไม่ใช่ผู้ตัดสินสุดท้ายไม่ควรปิดเรื่องของคนอื่น
    // ต้องมีทั้ง isAndCondition true/false เพราะ service กรองตรง ๆ ตาม epms
    private static readonly (string Code, bool IsTop, bool IsAnd)[] CoreMappings =
    {
        ("submit", false, false), ("reject", false, false),
        ("submit", false, true),  ("reject", false, true),
        ("approve", true, false), ("reject", true, false), ("decline", true, false),
        ("approve", true, true),  ("reject", true, true),  ("decline", true, true),
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var ctx = await dbFactory.CreateDbContextAsync();

        var now = DateTime.Now;

        // Add-missing by code rather than "only when the table is empty": the earlier
        // guard meant a database that already had the three original definitions could
        // never receive the fourth ("submit"), and the button set stayed incomplete
        // forever. Existing rows are still never overwritten, so an admin who renamed a
        // label keeps it.
        var masters = await ctx.wf_button_masters.Where(m => m.code != null)
            .ToDictionaryAsync(m => m.code!, StringComparer.OrdinalIgnoreCase);

        foreach (var b in CoreButtons)
        {
            if (masters.ContainsKey(b.Code)) continue;
            var row = new wf_button_master
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
            };
            ctx.wf_button_masters.Add(row);
            masters[b.Code] = row;
        }
        await ctx.SaveChangesAsync();

        // The mappings are what actually make buttons appear. Without these rows the
        // approval screen finds nothing configured and silently falls back to the
        // buttons written into the page — which is how wf_button sat empty while the
        // whole config path existed (owner, 2026-09-10: "config ใน table ได้เลย").
        var existing = await ctx.wf_buttons
            .Where(b => b.workflowid == null && b.wlevel == null)
            .Select(b => new { b.button_masterid, b.istop, b.isAndCondition })
            .ToListAsync();

        foreach (var (code, isTop, isAnd) in CoreMappings)
        {
            if (!masters.TryGetValue(code, out var m)) continue;
            if (existing.Any(e => e.button_masterid == m.id
                               && (e.istop ?? false) == isTop && e.isAndCondition == isAnd)) continue;

            ctx.wf_buttons.Add(new wf_button
            {
                btname = m.value,
                bcode = m.code,
                class_style = m.class_style,
                isactive = true,
                isshow = true,
                istop = isTop,
                isStart = false,
                isAndCondition = isAnd,
                button_masterid = m.id,
                workflowid = null,
                wlevel = null,
            });
        }

        await ctx.SaveChangesAsync();
    }
}
