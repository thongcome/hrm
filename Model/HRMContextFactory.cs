namespace HRM.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

// dotnet ef's design-time tooling enumerates every DbContext type in the
// assembly (even when --context picks one explicitly), and ApplicationDbContext
// being registered two ways (AddDbContextFactory + plain AddDbContext, for
// OpenIddict's EF Core store) breaks that enumeration's whole-app-host
// bootstrap ("Cannot resolve scoped service ... from root provider") for
// every context, not just ApplicationDbContext. Implementing this interface
// bypasses that bootstrap for HRMContext too — used only by the CLI, never
// by the running app. See Data/ApplicationDbContextFactory.cs for the
// sibling factory that was added at the same time, for the same reason.
public class HRMContextFactory : IDesignTimeDbContextFactory<HRMContext>
{
    public HRMContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets("aspnet-LeaderDevelop-2fc179dd-3f85-4eb6-ba75-b7b719f0c039")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found (checked appsettings.json, user secrets, and environment variables).");

        var optionsBuilder = new DbContextOptionsBuilder<HRMContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new HRMContext(optionsBuilder.Options);
    }
}
