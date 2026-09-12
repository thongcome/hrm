namespace Advance.SecurityCore.Services;

using Advance.SecurityCore.Data;
using Advance.SecurityCore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Adapted from HRM's Services/Security/ScMenuNavSeeder.cs. Logic is
// unchanged; the one structural change is the data source: HRM's version
// read a single static `ScMenuNavCatalog.Groups`/`.Links`. This version
// takes `IEnumerable<IMenuNavContributor>` (DI-resolved — every module that
// owns menu entries registers one) and concatenates all of them, so seeding
// stays correct however many products/modules are wired into a given host.
public static class ScMenuNavSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IEnumerable<IMenuNavContributor> contributors)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecurityDbContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        var allGroups = contributors.SelectMany(c => c.Groups).ToList();
        var allLinks = contributors.SelectMany(c => c.Links).ToList();

        var all = await context.sc_menus.ToListAsync();
        var byUrl = new Dictionary<string, List<sc_menu>>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in all)
        {
            if (string.IsNullOrWhiteSpace(m.url)) continue;
            if (!byUrl.TryGetValue(m.url!, out var list)) byUrl[m.url!] = list = new List<sc_menu>();
            list.Add(m);
        }

        // Which groups the catalog claims per url — an existing row in none
        // of them is a legacy row eligible for adoption (see HRM's original
        // header comment for the full เช็คซ้ำ rules this preserves).
        var catalogGroupsByUrl = allLinks
            .GroupBy(l => l.Url, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(l => l.GroupCode).ToList(), StringComparer.OrdinalIgnoreCase);

        static bool SameGroup(string? a, string? b) =>
            string.IsNullOrWhiteSpace(a) ? string.IsNullOrWhiteSpace(b)
                : string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        var byCode = all.Where(m => !string.IsNullOrWhiteSpace(m.menucode))
            .GroupBy(m => m.menucode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        static bool SeederOwns(sc_menu m) => string.Equals(m.modby, "ScMenuNavSeeder", StringComparison.Ordinal);

        // 1) Groups — level 1, no url, isfinal=false.
        foreach (var g in allGroups)
        {
            if (byCode.TryGetValue(g.GroupCode, out var existing))
            {
                existing.menuorder = g.Order;
                existing.icon = g.Icon;
                existing.menuname_en = g.NameEn;
                if (SeederOwns(existing)) existing.menuname = g.NameTh;
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
        foreach (var l in allLinks)
        {
            var candidates = byUrl.TryGetValue(l.Url, out var list) ? list : null;

            var existing = candidates?.FirstOrDefault(m => SameGroup(m.uppermenucode, l.GroupCode));
            if (existing is not null)
            {
                existing.menulevel = l.GroupCode is null ? 1 : 2;
                existing.menuorder = l.Order;
                existing.icon = l.Icon;
                if (string.IsNullOrWhiteSpace(existing.menuname_en))
                    existing.menuname_en = l.NameEn;
                if (SeederOwns(existing)) { existing.menuname = l.NameTh; existing.menuname_en = l.NameEn; }
                continue;
            }

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
                menucode = l.Code, // null = visible to any logged-in user (rendered fail-closed by the host's own nav component)
                menuname = l.NameTh,
                menuname_en = l.NameEn,
                menulevel = l.GroupCode is null ? 1 : 2,
                uppermenucode = l.GroupCode,
                url = l.Url,
                icon = l.Icon,
                menuorder = l.Order,
                menugroupid = 1,
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
