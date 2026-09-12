namespace Advance.SecurityCore.Services;

using System.Reflection;
using Advance.SecurityCore.Data;
using Advance.SecurityCore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Copied from HRM's Services/Security/ScProgramRouteSeeder.cs. Same
// route-assembly parameterization as ProgramRoleService.SeedAsync above —
// see that file's header for why. Registers every route in the legacy
// `sc_program` table (a route registry, distinct from sc_program_role's
// per-role RIGHTS on that route — see sc_program.cs for why this table is
// part of the extraction even though it wasn't in the original list).
public static class ScProgramRouteSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IEnumerable<Assembly> routeAssemblies)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecurityDbContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        var paths = ProgramRoleService.ScanRoutedPaths(routeAssemblies);

        // เช็คซ้ำ: progcode is the identity of a route row. Compared
        // case-insensitively (SQL Server default collation is CI anyway).
        var existingCodes = (await context.sc_programs.Select(p => p.progcode).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var path in paths)
        {
            // progcode is StringLength(50) — guard against a future long
            // route by truncating deterministically rather than throwing.
            var code = path.Length <= 50 ? path : path[..50];
            if (existingCodes.Contains(code)) continue;

            context.sc_programs.Add(new sc_program
            {
                progcode = code,
                progname = path,
                filename = path,
                isactive = true,
                modby = "ScProgramRouteSeeder",
                moddate = DateTime.Now,
            });
            existingCodes.Add(code);
            added++;
        }

        if (added > 0)
            await context.SaveChangesAsync();
    }
}
