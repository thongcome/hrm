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
//   - LINK identity = (group, url), case-insensitive. The same url may
//     legitimately appear in TWO groups (CEO 31 ส.ค. 2569: /wf/my-inbox in
//     both ESS and Workflow; /hr/announcements in both ESS and Announce) —
//     each (group, url) pair is one row, and the same pair is never
//     inserted twice. A pre-existing row (e.g. the ~60 migration-seeded
//     ones) whose group isn't claimed by any catalog entry for that url is
//     ADOPTED by the first catalog entry — placement fields aligned
//     (uppermenucode/menulevel/menuorder/icon/menuname_en-if-empty) — but
//     its menucode and isactive are never touched: grants in sc_role_menu
//     key off the existing row and stay exactly as the humans configured
//     them.
//   - GROUP identity = menucode (GRP_*).
public static class ScMenuNavSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        var all = await context.sc_menus.ToListAsync();
        var byUrl = new Dictionary<string, List<sc_menu>>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in all)
        {
            if (string.IsNullOrWhiteSpace(m.url)) continue;
            if (!byUrl.TryGetValue(m.url!, out var list)) byUrl[m.url!] = list = new List<sc_menu>();
            list.Add(m);
        }

        // Which groups the catalog claims per url — an existing row in none
        // of them is a legacy row eligible for adoption (see header).
        var catalogGroupsByUrl = ScMenuNavCatalog.Links
            .GroupBy(l => l.Url, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(l => l.GroupCode).ToList(), StringComparer.OrdinalIgnoreCase);

        static bool SameGroup(string? a, string? b) =>
            string.IsNullOrWhiteSpace(a) ? string.IsNullOrWhiteSpace(b)
                : string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
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
            var candidates = byUrl.TryGetValue(l.Url, out var list) ? list : null;

            // เช็คซ้ำ: this (group, url) pair already has a row — align
            // placement only; menucode/isactive/grants untouched.
            var existing = candidates?.FirstOrDefault(m => SameGroup(m.uppermenucode, l.GroupCode));
            if (existing is not null)
            {
                existing.menulevel = l.GroupCode is null ? 1 : 2;
                existing.menuorder = l.Order;
                existing.icon = l.Icon;
                if (string.IsNullOrWhiteSpace(existing.menuname_en))
                    existing.menuname_en = l.NameEn;
                continue;
            }

            // Adoption: a row with this url sitting in a group NO catalog
            // entry for this url claims (typically an old migration-seeded
            // row, or a row placed by the pre-(group,url) version of this
            // seeder) is moved into this entry's group instead of being
            // duplicated. Once adopted its group matches the catalog, so a
            // second entry for the same url can never adopt it again.
            var claimedGroups = catalogGroupsByUrl[l.Url];
            var orphan = candidates?.FirstOrDefault(m => !claimedGroups.Any(g => SameGroup(m.uppermenucode, g)));
            if (orphan is not null)
            {
                orphan.uppermenucode = l.GroupCode;
                orphan.menulevel = l.GroupCode is null ? 1 : 2;
                orphan.menuorder = l.Order;
                orphan.icon = l.Icon;
                if (string.IsNullOrWhiteSpace(orphan.menuname_en))
                    orphan.menuname_en = l.NameEn;
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
            (candidates ?? (byUrl[l.Url] = new List<sc_menu>())).Add(row);
        }

        await context.SaveChangesAsync();
    }
}
