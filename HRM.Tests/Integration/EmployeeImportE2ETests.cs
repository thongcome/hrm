using ClosedXML.Excel;
using HRM.Models;
using HRM.Services.Hr.EmployeeImport;
using HRM.Tests.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Tests.Integration;

// Employee import against a real database (throw-away copy — same HRM_E2E_CONNECTION rule as
// PayrollApprovalE2ETests): template from the live lookups -> fill -> preview -> import -> import the
// SAME file again. The second run must update, never duplicate (CEO, 19 ก.ย. 2569). ESS logins are
// switched off here (Identity is not part of this harness).
public class EmployeeImportE2ETests
{
    private const string Company = "ADVD";

    [E2eDbFact]
    public async Task Import_creates_then_updates_without_duplicates()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<HRMContext>(o => o.UseSqlServer(E2eDatabase.ConnectionString!));
        services.AddScoped<EmployeeImportTemplateService>();
        services.AddScoped<EmployeeImportService>();
        await using var sp = services.BuildServiceProvider();
        await using var scope = sp.CreateAsyncScope();
        var template = scope.ServiceProvider.GetRequiredService<EmployeeImportTemplateService>();
        var import = scope.ServiceProvider.GetRequiredService<EmployeeImportService>();
        var factory = sp.GetRequiredService<IDbContextFactory<HRMContext>>();

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Hremployee.RemoveRange(db.Hremployee.Where(e => e.companyid == Company && e.EmpNo.StartsWith("E2EI")));
            await db.SaveChangesAsync();
        }

        var lookups = await template.LoadLookupsAsync(Company);
        var bank = lookups.Banks.First();
        var position = lookups.Positions.First();
        using var wb = new XLWorkbook(new MemoryStream(await template.BuildAsync(Company)));
        // the demo data has units whose approver code matches no employee; the importer rightly refuses those, so blank them for this test
        var orgWs = wb.Worksheet(EmployeeImportSchema.Org.Name);
        var approverCol = EmployeeImportSchema.Org.Columns.ToList().FindIndex(c => c.Key == "ApproverEmpNo") + 1;
        for (var r = 2; r <= (orgWs.LastRowUsed()?.RowNumber() ?? 1); r++) orgWs.Cell(r, approverCol).Clear(XLClearOptions.Contents);
        var orgRow = new Dictionary<string, object> { ["OrgCode"] = "E2EIORG", ["OrgName"] = "หน่วยทดสอบนำเข้า" };
        var people = new[] { ("E2EI001", "110170020111"), ("E2EI002", "110170020222") }.Select(p =>
        {
            var e = EmployeeImportParserTests.Employee(p.Item1, p.Item2, "E2EIORG");
            e["Position"] = $"{position.Code} - {position.Name}";
            e["Bank"] = $"{bank.Code} - {bank.Name}";
            e["CreateLogin"] = EmployeeImportSchema.No;
            return e;
        }).ToList();
        EmployeeImportParserTests.Fill(wb.Worksheet(EmployeeImportSchema.Org.Name), EmployeeImportSchema.Org, [orgRow]);
        EmployeeImportParserTests.Fill(wb.Worksheet(EmployeeImportSchema.Employee.Name), EmployeeImportSchema.Employee, people);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();

        var preview = await import.PreviewAsync(new MemoryStream(bytes), "e2e.xlsx", Company);
        Assert.True(preview.Issues.Count == 0, string.Join("; ", preview.Issues.Select(i => $"{i.Sheet}#{i.Row} {i.Column}: {i.Message}")));
        Assert.Equal(2, preview.NewEmployees);

        var first = await import.ImportAsync(preview, Company, actorUserId: 0);
        Assert.Equal((2, 0), (first.EmployeesAdded, first.EmployeesUpdated));

        var again = await import.PreviewAsync(new MemoryStream(bytes), "e2e.xlsx", Company);
        Assert.NotNull(again.SameFileImportedBefore);
        Assert.Equal(0, again.NewEmployees);
        var second = await import.ImportAsync(again, Company, actorUserId: 0);
        Assert.Equal((0, 2), (second.EmployeesAdded, second.EmployeesUpdated));

        await using var check = await factory.CreateDbContextAsync();
        Assert.Equal(2, await check.Hremployee.CountAsync(e => e.companyid == Company && e.EmpNo.StartsWith("E2EI")));
        Assert.Equal(1, await check.com_organizations.CountAsync(o => o.code == "E2EIORG"));
    }
}
