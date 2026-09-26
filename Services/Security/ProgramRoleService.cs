namespace HRM.Services.Security;

using System.Reflection;
using System.Security.Claims;
using HRM.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

// AD.CRUDManage enforcement — see Model/sc_program_role.cs for the model's
// full rationale. This file owns the three runtime pieces:
//
//   1. Route scanning (shared by the seeder and the regression test): every
//      component with an @page route in this assembly, with parameter
//      segments stripped, is a "program path".
//   2. Startup auto-seed: insert missing (role × path) rows so the table is
//      always complete without anyone registering pages by hand. Existing
//      rows are NEVER touched — human permission decisions survive restarts.
//      Activation baseline (deliberate deviation from the skill's
//      all-read-only default, recorded in CLAUDE.md): the "Admin" role
//      seeds with all four flags so turning enforcement on doesn't
//      instantly read-only the whole live system for the people running
//      it; every other role seeds read-only per the skill.
//   3. The cached per-request check: rights are read from the table via a
//      ~60s in-memory cache — never stamped into the login cookie — so an
//      admin's permission change takes effect within a minute with no
//      re-login. Fail-closed: no matching row means no rights.
public class ProgramRoleService(IDbContextFactory<HRMContext> dbFactory, IMemoryCache cache)
{
    public record ProgramRights(bool CanCreate, bool CanRead, bool CanEdit, bool CanDelete)
    {
        public static readonly ProgramRights None = new(false, false, false, false);
    }

    public enum ProgramAction { Create, Read, Edit, Delete }

    private const string CacheKey = "sc_program_role.all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    // Rows and the role-name map live in ONE cache entry so they can never be
    // out of step (they used to be two entries, "…all" and "…all.rolemap").
    // A role name maps to EVERY active role id carrying it: two active roles
    // with the same name used to make ToDictionaryAsync throw, which failed
    // every rights check on every page for everyone. Ported from the ADP.AI
    // Advance.SecurityCore master (2b4abb5), CEO 26 ก.ย. 2569.
    private sealed record RightsSnapshot(
        Dictionary<long, List<sc_program_role>> RowsByRole,
        Dictionary<string, long[]> RoleIdsByName);

    // ---- 1) Route scanning ------------------------------------------------

    // "/leave-requests/detail/{Id:long}" -> "/leave-requests/detail";
    // "/{param}" -> "/" ; templates without parameters pass through as-is.
    public static string? NormalizeRouteTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template)) return null;
        var segments = template.Trim().TrimStart('/').Split('/');
        var kept = segments.TakeWhile(s => !s.StartsWith('{')).ToArray();
        var path = "/" + string.Join('/', kept);
        return path.Length > 1 ? path.TrimEnd('/') : "/";
    }

    public static IReadOnlyList<string> ScanRoutedPaths()
    {
        var componentAssembly = typeof(HRM.Components.App).Assembly;
        return componentAssembly.GetTypes()
            .SelectMany(t => t.GetCustomAttributes<RouteAttribute>())
            .Select(a => NormalizeRouteTemplate(a.Template))
            .Where(p => p is not null)
            .Select(p => p!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ---- 2) Startup auto-seed --------------------------------------------

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        var paths = ScanRoutedPaths();
        var roles = await context.sc_roles.Where(r => r.isactive).Select(r => new { r.roleid, r.name }).ToListAsync();

        // Existence check ignores isactive on purpose: a row an admin
        // deactivated is still a decision, not a gap to refill.
        var existing = await context.sc_program_roles
            .Select(p => new { p.roleid, p.progpath })
            .ToListAsync();
        var existingKeys = existing.Select(e => (e.roleid, e.progpath.ToLowerInvariant())).ToHashSet();

        var added = 0;
        foreach (var role in roles)
        {
            var isAdminBaseline = string.Equals(role.name, "Admin", StringComparison.OrdinalIgnoreCase);
            foreach (var path in paths)
            {
                if (existingKeys.Contains((role.roleid, path.ToLowerInvariant()))) continue;
                context.sc_program_roles.Add(new sc_program_role
                {
                    roleid = role.roleid,
                    progpath = path,
                    cancreate = isAdminBaseline,
                    canread = true,
                    canedit = isAdminBaseline,
                    candelete = isAdminBaseline,
                    modby = "ProgramRoleSeeder",
                    moddate = DateTime.Now,
                });
                added++;
            }
        }

        if (added > 0)
            await context.SaveChangesAsync();
    }

    // ---- 3) Cached checks -------------------------------------------------

    private async Task<RightsSnapshot> GetSnapshotAsync(CancellationToken ct)
    {
        return (await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var rows = await context.sc_program_roles.Where(p => p.isactive).ToListAsync(ct);
            var roles = await context.sc_roles.Where(r => r.isactive && r.name != null)
                .Select(r => new { r.name, r.roleid }).ToListAsync(ct);
            return BuildSnapshot(rows, roles.Select(r => (r.name!, r.roleid)));
        }))!;
    }

    // Pure — ProgramRoleAccessTests reaches it through RightsFromRows below.
    private static RightsSnapshot BuildSnapshot(IEnumerable<sc_program_role> rows, IEnumerable<(string Name, long RoleId)> roles)
        => new(
            rows.GroupBy(r => r.roleid).ToDictionary(g => g.Key, g => g.ToList()),
            roles.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(r => r.RoleId).ToArray(), StringComparer.OrdinalIgnoreCase));

    public async Task<ProgramRights> GetRightsAsync(ClaimsPrincipal user, string path, CancellationToken ct = default)
        => RightsFor(await GetSnapshotAsync(ct), user, path);

    private static ProgramRights RightsFor(RightsSnapshot snapshot, ClaimsPrincipal user, string path)
    {
        var perRoleRows = user.FindAll(ClaimTypes.Role).Select(x => x.Value)
            .SelectMany(roleName => snapshot.RoleIdsByName.TryGetValue(roleName, out var ids) ? ids : [])
            .Distinct()
            .Where(snapshot.RowsByRole.ContainsKey)
            .Select(roleId => (IReadOnlyList<sc_program_role>)snapshot.RowsByRole[roleId]);

        return ResolveRights(perRoleRows, path);
    }

    // Normalize a request path to how program-path rows are keyed: no trailing
    // slash, "/" stays "/". Public + static so it can be unit-tested and reused
    // by GetRightsAsync alongside ResolveRights below.
    public static string NormalizePath(string? path)
    {
        var normalized = (path ?? "/").TrimEnd('/');
        return normalized.Length == 0 ? "/" : normalized;
    }

    // Does a program-path row govern this (already-normalized) request path?
    // Prefix match with a SEGMENT boundary, so "/admin" never covers
    // "/administrators" — the boundary check is the security-critical part.
    public static bool ProgPathCovers(string progpath, string normalizedPath)
        => normalizedPath.StartsWith(progpath, StringComparison.OrdinalIgnoreCase)
            && (normalizedPath.Length == progpath.Length
                || progpath == "/"
                || normalizedPath[progpath.Length] == '/');

    // The pure access-control decision, extracted from GetRightsAsync so it can
    // be unit-tested (ProgramRoleAccessTests) independently of the cache/EF:
    // per role, longest-prefix wins ("/leave-requests/policy" beats
    // "/leave-requests"); rights then OR-accumulate ACROSS roles; a path no row
    // covers yields ProgramRights.None (fail-closed). Per-role-then-OR order is
    // deliberate — flattening all roles' rows before longest-prefix would let a
    // narrower read-only row on one role mask a broader edit grant on another.
    public static ProgramRights ResolveRights(IEnumerable<IReadOnlyList<sc_program_role>> perRoleRows, string? path)
    {
        var normalized = NormalizePath(path);

        bool c = false, r = false, e = false, d = false;
        foreach (var rows in perRoleRows)
        {
            var match = rows
                .Where(row => ProgPathCovers(row.progpath, normalized))
                .OrderByDescending(row => row.progpath.Length)
                .FirstOrDefault();
            if (match is null) continue;

            c |= match.cancreate; r |= match.canread; e |= match.canedit; d |= match.candelete;
        }
        return new ProgramRights(c, r, e, d);
    }

    // Server-side re-check for write handlers — the second of the "two
    // layers, always" rule (a hidden button stops nobody who guesses the
    // URI). Throws with a Thai message the page's existing catch blocks
    // already surface.
    public async Task RequireAsync(ClaimsPrincipal user, string path, ProgramAction action, CancellationToken ct = default)
    {
        var rights = await GetRightsAsync(user, path, ct);
        var allowed = action switch
        {
            ProgramAction.Create => rights.CanCreate,
            ProgramAction.Read => rights.CanRead,
            ProgramAction.Edit => rights.CanEdit,
            ProgramAction.Delete => rights.CanDelete,
            _ => false,
        };
        if (!allowed)
            throw new InvalidOperationException("บทบาทของคุณยังไม่ได้รับสิทธิ์ทำรายการนี้ในหน้านี้ (ติดต่อผู้ดูแลระบบเพื่อเปิดสิทธิ์)");
    }

    // Called by the permission-admin screen after a save so changes apply
    // immediately there instead of waiting out the TTL.
    public void InvalidateCache() => cache.Remove(CacheKey);

    // Test seam for the snapshot logic (duplicate role names, role-id merge) without a database.
    public static ProgramRights RightsFromRows(
        IEnumerable<sc_program_role> rows, IEnumerable<(string Name, long RoleId)> roles, ClaimsPrincipal user, string path)
        => RightsFor(BuildSnapshot(rows, roles), user, path);
}
