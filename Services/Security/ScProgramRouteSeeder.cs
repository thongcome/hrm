namespace HRM.Services.Security;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// CEO order (31 ส.ค. 2569, step-by-step CRUDManage build-out): every page
// URL in the app gets a row in the legacy `sc_program` registry — the same
// "reuse the legacy table, don't invent a new one" principle sc_menu
// followed. Runs at every startup off the same route scan
// ProgramRoleService uses, so a new page registers itself on the next
// start; duplicate-checked per row (by progcode = the route path), and
// existing rows — including the two hand-seeded JOBFAMILY_* proof-of-
// concept rows, whose progcodes don't collide with path-style codes — are
// never touched.
public static class ScProgramRouteSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        var paths = ProgramRoleService.ScanRoutedPaths();

        // เช็คซ้ำ: progcode is the identity of a route row. Compared
        // case-insensitively (SQL Server default collation is CI anyway) so
        // a casing difference can't slip a duplicate in.
        var existingCodes = (await context.sc_programs.Select(p => p.progcode).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var path in paths)
        {
            // progcode is StringLength(50); longest current route is 39.
            // Guard anyway — a future long route lands in filename intact
            // and its code is truncated deterministically rather than
            // throwing at startup.
            var code = path.Length <= 50 ? path : path[..50];
            if (existingCodes.Contains(code)) continue;

            context.sc_programs.Add(new sc_program
            {
                progcode = code,
                progname = path,
                filename = path, // the URL — legacy JSP used this column to point at the page file
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
