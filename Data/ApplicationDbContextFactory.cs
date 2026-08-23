namespace HRM.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

// dotnet ef's design-time tooling normally spins up the whole app's DI
// container to find a DbContext, but ApplicationDbContext is now registered
// two ways (AddDbContextFactory for app code, plain AddDbContext so
// OpenIddict's EF Core store can resolve it directly — see Program.cs) and
// that combination makes the tooling try to resolve a scoped service from
// the root provider and fail ("Cannot resolve scoped service ... from root
// provider"). Implementing this interface bypasses the whole-app DI
// bootstrap for `dotnet ef` commands entirely — used only by the CLI,
// never by the running app.
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets("aspnet-LeaderDevelop-2fc179dd-3f85-4eb6-ba75-b7b719f0c039")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found (checked appsettings.json, user secrets, and environment variables).");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
