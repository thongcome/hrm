using HRM.Models;
using HRM.Services.Pay;
using HRM.Services.Pay.Calculators;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HRM.Tests.Integration;

// Integration tests run against the developer's real SQL Server (the same
// connection string the app reads from user secrets) — the legacy schema is
// not SQLite/in-memory friendly, so there is no cheaper way to exercise the
// payroll engine end-to-end. When no database is reachable the tests are
// SKIPPED, not failed, so the pure unit suite stays green on any machine.
//
// Connection string resolution order:
//   1. HRM_TEST_CONNECTION environment variable
//   2. ConnectionStrings:DefaultConnection from the HRM project's user secrets
public static class DevDatabase
{
    private static readonly Lazy<string?> ConnectionStringLazy = new(Resolve);
    private static readonly Lazy<bool> AvailableLazy = new(Probe);

    public static string? ConnectionString => ConnectionStringLazy.Value;
    public static bool IsAvailable => AvailableLazy.Value;

    private static string? Resolve()
    {
        var env = Environment.GetEnvironmentVariable("HRM_TEST_CONNECTION");
        if (!string.IsNullOrWhiteSpace(env)) return env;
        try
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets(typeof(HRMContext).Assembly, optional: true)
                .Build();
            return config.GetConnectionString("DefaultConnection");
        }
        catch
        {
            return null;
        }
    }

    private static bool Probe()
    {
        var cs = ConnectionString;
        if (string.IsNullOrWhiteSpace(cs)) return false;
        try
        {
            var builder = new SqlConnectionStringBuilder(cs) { ConnectTimeout = 3 };
            using var conn = new SqlConnection(builder.ConnectionString);
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // The same service graph Program.cs wires for the Pay_* module, minus the
    // web host: a DbContext factory on the dev database, the SSO rate provider,
    // the calculators, the workflow, and the three file exporters writing under
    // a throw-away content root.
    public static ServiceProvider BuildPayrollServices(string contentRoot)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<HRMContext>(o => o.UseSqlServer(ConnectionString!));
        services.AddSingleton<IWebHostEnvironment>(new TestHostEnvironment(contentRoot));
        services.AddScoped<PrivateFileStorage>();
        services.AddScoped<ISocialSecurityRateProvider, HrucfsecurityRateProvider>();
        services.AddScoped<OvertimeEarningsCalculator>();
        services.AddScoped<LoanDeductionCalculator>();
        services.AddScoped<PayrollAnomalyDetectionService>();
        services.AddScoped<PayrollCalculationService>();
        services.AddScoped<PayrollWorkflowService>();
        services.AddScoped<PayslipGenerationService>();
        services.AddScoped<BankFileExportService>();
        services.AddScoped<GLExportService>();
        return services.BuildServiceProvider();
    }

    private sealed class TestHostEnvironment(string contentRoot) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "HRM.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(contentRoot, "wwwroot");
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

// [DevDbFact] = a Fact that is skipped (with a reason) when the dev database
// cannot be reached, instead of failing.
public sealed class DevDbFactAttribute : FactAttribute
{
    public DevDbFactAttribute()
    {
        if (!DevDatabase.IsAvailable)
            Skip = "Dev SQL Server not reachable (set HRM_TEST_CONNECTION or the HRM user secret ConnectionStrings:DefaultConnection)";
    }
}
