using System.Globalization;
using HRM.Models;
using HRM.Services;
using HRM.Services.Audit;
using HRM.Services.Pay;
using HRM.Services.Pay.Calculators;
using HRM.Services.Workflow;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Xunit.Abstractions;

namespace HRM.Tests.Integration;

// End-to-end: the whole payroll month the way a small customer runs it (CEO, 18 ก.ย. 2569) —
// one payroll officer + one approver, approval through the workflow engine's inbox.
//
//   A. happy path   pre-flight → calculate → send for approval → approver approves in the
//                   workflow → run locks → approver posts → bank file → confirm paid
//   B. decline      approver declines → run back to Calculated with the reason → resend (new job)
//   C. SoD          officer who also holds the approver role may approve in the workflow (rule 16) but payroll refuses it
//   D. cancel       cancelling a run that waits for approval closes its workflow job
//
// Paid runs are FINAL and can never be removed, so this only ever runs against a throw-away
// copy: HRM_E2E_CONNECTION must point at a database whose name contains "e2e"
// (e.g. hrm_e2e, a restored copy of hrm). Otherwise the test is skipped.
public class PayrollApprovalE2ETests(ITestOutputHelper output)
{
    private const string Company = "ADVD";
    private const string OfficerEmpNo = "AD3756";   // เจ้าหน้าที่เงินเดือน (แผนกค่าตอบแทนและสวัสดิการ)
    private const string ApproverEmpNo = "AD0024";  // ผู้อนุมัติเงินเดือน (ผู้จัดการแผนกเดียวกัน)

    [E2eDbFact]
    public async Task Small_company_payroll_month_end_to_end()
    {
        var root = Path.Combine(Path.GetTempPath(), "hrm-payroll-e2e");
        Directory.CreateDirectory(root);
        await using var sp = E2eDatabase.BuildServices(root);
        var factory = sp.GetRequiredService<IDbContextFactory<HRMContext>>();

        var (officer, approver) = await SetUpPeopleAsync(factory);
        Log($"officer userid={officer}, approver userid={approver}");

        // ── A. happy path ────────────────────────────────────────────────────────
        var runA = await NextOpenRunAsync(factory);
        Log($"A: run #{runA}");
        await using (var scope = sp.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            var wf = s.GetRequiredService<PayrollWorkflowService>();
            var engine = s.GetRequiredService<WorkflowEngineService>();

            var pre = await s.GetRequiredService<PayrollPreflightService>().CheckAsync(runA);
            Assert.Empty(pre.ConfigErrors);
            foreach (var i in pre.Issues.Where(i => i.Severity == PayrollPreflightService.Severity.Error)) Log($"A: pre-flight ERROR {i.EmpNo} {i.Code} {i.Message}");
            Log($"A: pre-flight ok — {pre.EligibleCount} eligible");

            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => wf.CalculateAsync(runA, approver)); // approver may not calculate
            var calc = await wf.CalculateAsync(runA, officer);
            await using (var db = await factory.CreateDbContextAsync())
            {
                var rows = await db.Pay_PayrollEmployees.Where(e => e.PayrollRunId == runA).ToListAsync();
                Assert.Equal(pre.EligibleCount, rows.Count);
                Assert.DoesNotContain(rows, r => r.EmpNo == "KB0001");
                Assert.All(rows, r => Assert.True(r.NetPay >= 0, $"{r.EmpNo} net {r.NetPay}"));
                Log($"A: calculated {rows.Count} employees, net total {rows.Sum(r => r.NetPay):N2}");
            }

            await wf.SubmitForReviewAsync(runA, officer);
            var sent = await LoadRunAsync(factory, runA);
            Assert.Equal(PayrollRunStatus.Reviewed, sent.Status);
            Assert.NotNull(sent.JobMasterId);
            Assert.DoesNotContain(PayrollAction.Approve, PayrollWorkflowService.GetAllowedActions(sent));
            await Assert.ThrowsAnyAsync<Exception>(() => wf.ApproveAsync(runA, approver, null)); // approve only via the inbox

            var inbox = await engine.GetMyInboxAsync(approver);
            Assert.Contains(inbox, r => r.jobmasterid == sent.JobMasterId);
            Log($"A: job #{sent.JobMasterId} is in the approver's inbox");

            await engine.ActAsync(sent.JobMasterId!.Value, 0, approver, WorkflowButtonService.ActionApprove, "อนุมัติ (e2e)");
            var approved = await LoadRunAsync(factory, runA);
            Assert.Equal(PayrollRunStatus.Approved, approved.Status);
            Assert.Equal(approver, approved.ApprovedByUserId);
            Log("A: approved in the workflow → run locked as Approved");

            await wf.PostAsync(runA, approver);   // HRM: post/pay need the approver role (PayrollStepPermission.RoleCodeFor)
            // ไฟล์ธนาคารไม่อยู่ในการทดสอบนี้: ข้อมูลสาธิต AUTOX ไม่มีเลขบัญชี (BankFileExportService ปฏิเสธถูกต้อง)
            await wf.MarkPaidAsync(runA, approver);
            Assert.Equal(PayrollRunStatus.Paid, (await LoadRunAsync(factory, runA)).Status);
            Log("A: posted, paid — done");
        }

        // ── B/C/D on the following month ─────────────────────────────────────────
        var runB = await NextOpenRunAsync(factory);
        Log($"B: run #{runB}");
        await using (var scope = sp.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            var wf = s.GetRequiredService<PayrollWorkflowService>();
            var engine = s.GetRequiredService<WorkflowEngineService>();

            await wf.CalculateAsync(runB, officer);
            await wf.SubmitForReviewAsync(runB, officer);
            var firstJob = (await LoadRunAsync(factory, runB)).JobMasterId!.Value;
            await engine.ActAsync(firstJob, 0, approver, WorkflowButtonService.ActionDecline, "ยอด OT ไม่ตรง (e2e)");
            var back = await LoadRunAsync(factory, runB);
            Assert.Equal(PayrollRunStatus.Calculated, back.Status);
            Assert.Null(back.JobMasterId);
            Assert.True(await HasLogAsync(factory, runB, "ตีกลับ"));
            Log("B: declined → back to Calculated with the reason in the run's history");

            // C: the officer also holds the approver role. The workflow lets a requester approve
            //    (ad-workflow rule 16), so the job does reach the officer — but payroll's own
            //    separation-of-duties check refuses the result and sends the run back.
            await SetRoleAsync(factory, officer, PayrollStepPermission.ApproverRoleCode, true);
            try
            {
                await wf.SubmitForReviewAsync(runB, officer);
                var secondJob = (await LoadRunAsync(factory, runB)).JobMasterId!.Value;
                Assert.NotEqual(firstJob, secondJob);
                Assert.Contains(await engine.GetMyInboxAsync(officer), r => r.jobmasterid == secondJob);
                await engine.ActAsync(secondJob, 0, officer, WorkflowButtonService.ActionApprove, "อนุมัติเอง (e2e)");
                await wf.SyncStatusFromJobAsync(runB);
                var refused = await LoadRunAsync(factory, runB);
                Assert.Equal(PayrollRunStatus.Calculated, refused.Status);
                Assert.Null(refused.ApprovedByUserId);
                Assert.True(await HasLogAsync(factory, runB, "แยกหน้าที่"));
                Log("C: preparer with the approver role approved in the workflow → payroll refused it (แยกหน้าที่), run back to Calculated");
            }
            finally
            {
                await SetRoleAsync(factory, officer, PayrollStepPermission.ApproverRoleCode, false);
            }

            // D: cancel while waiting for approval closes the job
            await wf.SubmitForReviewAsync(runB, officer);
            var thirdJob = (await LoadRunAsync(factory, runB)).JobMasterId!.Value;
            await wf.CancelAsync(runB, officer, "ทดสอบยกเลิก (e2e)");
            Assert.Equal(PayrollRunStatus.Cancelled, (await LoadRunAsync(factory, runB)).Status);
            await using (var db = await factory.CreateDbContextAsync())
                Assert.True((await db.job_masters.FirstAsync(j => j.jobmasterid == thirdJob)).isJobClosed == true);
            Assert.DoesNotContain(await engine.GetMyInboxAsync(approver), r => r.jobmasterid == thirdJob);
            Log("D: cancelled while waiting → workflow job closed, gone from the inbox");
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────────
    private void Log(string s) => output.WriteLine(s);

    private static async Task<(long Officer, long Approver)> SetUpPeopleAsync(IDbContextFactory<HRMContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        var officer = await db.sc_users.Where(u => u.empid == OfficerEmpNo).Select(u => u.userid).FirstAsync();
        var approver = await db.sc_users.Where(u => u.empid == ApproverEmpNo).Select(u => u.userid).FirstAsync();
        await SetRoleAsync(factory, officer, PayrollStepPermission.OfficerRoleCode, true);
        await SetRoleAsync(factory, officer, PayrollStepPermission.ApproverRoleCode, false);
        await SetRoleAsync(factory, approver, PayrollStepPermission.ApproverRoleCode, true);
        await SetRoleAsync(factory, approver, PayrollStepPermission.OfficerRoleCode, false);
        Assert.True(await db.wf_workflows.AnyAsync(w => w.workflowcode == PayrollWorkflowService.ApprovalWorkflowCode && w.isactive == true),
            "workflow PAYROLL_RUN_APPROVAL missing — apply migration 20260920210000 to the e2e database");
        return (officer, approver);
    }

    private static async Task SetRoleAsync(IDbContextFactory<HRMContext> factory, long userId, string roleCode, bool active)
    {
        await using var db = await factory.CreateDbContextAsync();
        var roleId = await db.sc_roles.Where(r => r.rolecode == roleCode).Select(r => r.roleid).FirstAsync();
        var row = await db.sc_user_roles.FirstOrDefaultAsync(r => r.userid == userId && r.roleid == roleId);
        if (row is null)
        {
            if (!active) return;
            db.sc_user_roles.Add(new sc_user_role { userid = userId, roleid = roleId, isactive = true, modate = DateTime.Now, modby = "e2e" });
        }
        else row.isactive = active;
        await db.SaveChangesAsync();
    }

    // The earliest run of the company that is not finished yet, or a new Draft run for the
    // month after the last finished one — so the test can be re-run on the same e2e copy.
    private static async Task<long> NextOpenRunAsync(IDbContextFactory<HRMContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        var open = await db.Pay_PayrollRuns
            .Where(r => r.CompanyId == Company && r.RunType == PayrollRunType.Regular
                        && (r.Status == PayrollRunStatus.Draft || r.Status == PayrollRunStatus.Calculated))
            .OrderBy(r => r.PeriodStart).FirstOrDefaultAsync();
        if (open is not null) return open.Id;

        var last = await db.Pay_PayrollRuns
            .Where(r => r.CompanyId == Company && r.RunType == PayrollRunType.Regular && r.Status != PayrollRunStatus.Cancelled)
            .OrderByDescending(r => r.PeriodStart).Select(r => (DateOnly?)r.PeriodStart).FirstOrDefaultAsync();
        var start = last is DateOnly d ? d.AddMonths(1) : new DateOnly(2026, 1, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var run = new Pay_PayrollRun
        {
            CompanyId = Company, PayrollPeriod = start.ToString("yyyyMM", CultureInfo.InvariantCulture), TermNo = 1,
            PeriodStart = start, PeriodEnd = end, PayDate = end.AddDays(-1),
            RunType = PayrollRunType.Regular, Status = PayrollRunStatus.Draft, CreatedByUserId = 0,
        };
        db.Pay_PayrollRuns.Add(run);
        await db.SaveChangesAsync();
        return run.Id;
    }

    private static async Task<Pay_PayrollRun> LoadRunAsync(IDbContextFactory<HRMContext> factory, long runId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Pay_PayrollRuns.AsNoTracking().FirstAsync(r => r.Id == runId);
    }

    private static async Task<bool> HasLogAsync(IDbContextFactory<HRMContext> factory, long runId, string text)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Pay_PayrollAuditLogs.AnyAsync(l => l.PayrollRunId == runId && l.Comment != null && l.Comment.Contains(text));
    }
}

public static class E2eDatabase
{
    public static string? ConnectionString
    {
        get
        {
            var cs = Environment.GetEnvironmentVariable("HRM_E2E_CONNECTION");
            if (string.IsNullOrWhiteSpace(cs)) return null;
            var db = new SqlConnectionStringBuilder(cs).InitialCatalog ?? "";
            return db.Contains("e2e", StringComparison.OrdinalIgnoreCase) ? cs : null; // never the real database
        }
    }

    // Payroll services + the workflow engine and its write-back, as Program.cs wires them.
    // Empty configuration: no SMTP, so approver notifications fail quietly (the engine logs and
    // carries on) and no e-mail ever leaves the test.
    public static ServiceProvider BuildServices(string contentRoot, string? connectionString = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var cs = connectionString ?? ConnectionString!;
        services.AddDbContextFactory<HRMContext>(o => o.UseSqlServer(cs));
        services.AddSingleton<IWebHostEnvironment>(new E2eHostEnvironment(contentRoot));
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<EmailSender>();
        services.AddScoped<PrivateFileStorage>();
        services.AddScoped<ISocialSecurityRateProvider, HrucfsecurityRateProvider>();
        services.AddScoped<OvertimeEarningsCalculator>();
        services.AddScoped<LoanDeductionCalculator>();
        services.AddScoped<PayrollAnomalyDetectionService>();
        services.AddScoped<PayrollCalculationService>();
        services.AddScoped<PayrollWorkflowService>();
        services.AddScoped<PayrollPreflightService>();
        services.AddScoped<BankFileExportService>();
        services.AddScoped<GLExportService>();
        services.AddScoped<WorkflowEngineService>();
        services.AddScoped<WorkflowService>();
        services.AddScoped<WorkflowButtonService>();
        WorkflowDocumentHandlers.AddWorkflowDocumentHandlers(services);
        return services.BuildServiceProvider();
    }

    private sealed class E2eHostEnvironment(string contentRoot) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "HRM.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(contentRoot, "wwwroot");
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public sealed class E2eDbFactAttribute : FactAttribute
{
    public E2eDbFactAttribute()
    {
        if (E2eDatabase.ConnectionString is null)
            Skip = "Set HRM_E2E_CONNECTION to a throw-away copy of the database whose name contains \"e2e\"";
    }
}
