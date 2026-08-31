namespace HRM.Services.Security;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// CEO order (31 ส.ค. 2569, access-menu step): the complete drawer
// navigation lives as DATA in the legacy `sc_menu` table — one row per nav
// link (url per row; several rows may share a gate menucode, exactly the
// shape the existing rows already use) plus one row per group (synthetic
// GRP_* code, no grant needed — a group is visible when any child is).
// Seeded from ScMenuNavCatalog (extracted from the old hardcoded
// MainLayout nav) at every startup, duplicate-checked per row.
//
// เช็คซ้ำ rules:
//   - LINK identity = url (case-insensitive). A url that already exists in
//     sc_menu — including the ~60 rows seeded by earlier migrations — is
//     NEVER inserted again. For those pre-existing rows we DO align the
//     tree-placement fields (uppermenucode/menuorder/icon/menuname_en)
//     with the catalog so the drawer groups them properly, but never touch
//     menucode or isactive: grants in sc_role_menu key off the existing
//     row and stay exactly as the humans configured them.
//   - GROUP identity = menucode (GRP_*).
public static class ScMenuNavSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        var all = await context.sc_menus.ToListAsync();
        var byUrl = new Dictionary<string, sc_menu>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in all)
        {
            if (!string.IsNullOrWhiteSpace(m.url) && !byUrl.ContainsKey(m.url!))
                byUrl[m.url!] = m;
        }
        var byCode = all.Where(m => !string.IsNullOrWhiteSpace(m.menucode))
            .GroupBy(m => m.menucode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // 1) Groups — level 1, no url, isfinal=false.
        foreach (var g in ScMenuNavCatalog.Groups)
        {
            if (byCode.TryGetValue(g.GroupCode, out var existing))
            {
                existing.menuorder = g.Order;
                existing.icon = g.Icon;
                existing.menuname_en = g.NameEn;
                continue;
            }
            var row = new sc_menu
            {
                menucode = g.GroupCode,
                menuname = g.NameTh,
                menuname_en = g.NameEn,
                menulevel = 1,
                uppermenucode = null,
                url = null,
                icon = g.Icon,
                menuorder = g.Order,
                menugroupid = 1, // the single legacy menu group every existing row uses
                isshow = true,
                isfinal = false,
                isactive = true,
                modby = "ScMenuNavSeeder",
                moddate = DateTime.Now,
            };
            context.sc_menus.Add(row);
            byCode[g.GroupCode] = row;
        }

        // 2) Links — level 2 under a group, level 1 when top-level.
        foreach (var l in ScMenuNavCatalog.Links)
        {
            if (byUrl.TryGetValue(l.Url, out var existing))
            {
                // Pre-existing row (old migration seeds): align placement
                // only — menucode/isactive/grants untouched (see header).
                existing.uppermenucode = l.GroupCode;
                existing.menulevel = l.GroupCode is null ? 1 : 2;
                existing.menuorder = l.Order;
                existing.icon = l.Icon;
                if (string.IsNullOrWhiteSpace(existing.menuname_en))
                    existing.menuname_en = l.NameEn;
                continue;
            }

            var row = new sc_menu
            {
                menucode = l.Code, // null = visible to any logged-in user (rendered fail-closed by DbNavMenu)
                menuname = l.NameTh,
                menuname_en = l.NameEn,
                menulevel = l.GroupCode is null ? 1 : 2,
                uppermenucode = l.GroupCode,
                url = l.Url,
                icon = l.Icon,
                menuorder = l.Order,
                menugroupid = 1, // the single legacy menu group every existing row uses
                isshow = true,
                isfinal = true,
                isactive = true,
                modby = "ScMenuNavSeeder",
                moddate = DateTime.Now,
            };
            context.sc_menus.Add(row);
            byUrl[l.Url] = row;
        }

        await context.SaveChangesAsync();
    }
}
