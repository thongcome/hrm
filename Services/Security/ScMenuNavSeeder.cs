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

        // Rows this seeder authored are its own to keep current — including
        // the display NAME (demo polish 1 ก.ย. 2569: catalog renames must
        // reach the drawer). Rows authored by anyone else (old migrations,
        // humans) keep their names forever.
        static bool SeederOwns(sc_menu m) => string.Equals(m.modby, "ScMenuNavSeeder", StringComparison.Ordinal);

        // ชั้นของกลุ่ม = ความลึกจริงในต้นไม้แคตตาล็อก (พ่อ + 1) — เมนูไม่จำกัดระดับ (CEO, 26 ก.ย. 2569) ไม่ใช่เพดาน 3/4 ชั้น
        // กันแคตตาล็อกที่พ่อวนกลับมาหาตัวเอง: เจอซ้ำเมื่อไรหยุดนับ
        static int GroupLevel(string groupCode)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var level = 0;
            for (var code = groupCode; code is not null && seen.Add(code); level++)
                code = ScMenuNavCatalog.Groups.FirstOrDefault(x => string.Equals(x.GroupCode, code, StringComparison.OrdinalIgnoreCase))?.ParentGroupCode;
            return Math.Max(1, level);
        }

        // 1) Groups — no url, isfinal=false; level = depth in the catalog tree.
        foreach (var g in ScMenuNavCatalog.Groups)
        {
            if (byCode.TryGetValue(g.GroupCode, out var existing))
            {
                existing.menuorder = g.Order;
                existing.icon = g.Icon;
                existing.menuname_en = g.NameEn;
                // ที่อยู่ของกลุ่มมาจากแคตตาล็อกเหมือนลิงก์ (กลุ่มย่อยในกลุ่ม)
                existing.uppermenucode = g.ParentGroupCode;
                existing.menulevel = GroupLevel(g.GroupCode);
                existing.isshow = true;
                if (SeederOwns(existing)) existing.menuname = g.NameTh;
                continue;
            }
            var row = new sc_menu
            {
                menucode = g.GroupCode,
                menuname = g.NameTh,
                menuname_en = g.NameEn,
                menulevel = GroupLevel(g.GroupCode),
                uppermenucode = g.ParentGroupCode,
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

        // กลุ่มที่แคตตาล็อกเลิกใช้ (ยุบเข้าโมดูลอื่นแล้ว) ซ่อนไว้ ไม่ลบ — สิทธิ์ผูกกับ menucode ของลิงก์ ไม่ใช่กลุ่ม
        var liveGroups = ScMenuNavCatalog.Groups.Select(g => g.GroupCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var stale in all.Where(m => m.url is null && m.menucode is not null
                     && m.menucode.StartsWith("GRP_", StringComparison.OrdinalIgnoreCase) && !liveGroups.Contains(m.menucode)))
            stale.isshow = false;

        // ลิงก์อยู่ชั้นไหน: ไม่มีกลุ่ม = 1 · อยู่ในกลุ่ม = ชั้นของกลุ่ม + 1
        static int LevelFor(string? groupCode)
        {
            return groupCode is null ? 1 : GroupLevel(groupCode) + 1;
        }

        // 2) Links — one level below their group (any depth), level 1 when top-level.
        foreach (var l in ScMenuNavCatalog.Links)
        {
            var candidates = byUrl.TryGetValue(l.Url, out var list) ? list : null;

            // เช็คซ้ำ: this (group, url) pair already has a row — align
            // placement only; menucode/isactive/grants untouched.
            var existing = candidates?.FirstOrDefault(m => SameGroup(m.uppermenucode, l.GroupCode));
            if (existing is not null)
            {
                existing.menulevel = LevelFor(l.GroupCode);
                existing.menuorder = l.Order;
                existing.icon = l.Icon;
                if (string.IsNullOrWhiteSpace(existing.menuname_en))
                    existing.menuname_en = l.NameEn;
                if (SeederOwns(existing)) { existing.menuname = l.NameTh; existing.menuname_en = l.NameEn; }
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
                orphan.menulevel = LevelFor(l.GroupCode);
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
                menulevel = LevelFor(l.GroupCode),
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
