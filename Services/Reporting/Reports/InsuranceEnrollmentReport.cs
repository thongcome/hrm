using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Active group-insurance enrollment counts per plan. Enrollment rows carry no
// CompanyId, so the report is scoped through the plan (Pay_InsurancePlan.CompanyId).
public class InsuranceEnrollmentReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "insurance-enrollment";
    public string Category => "สวัสดิการ (Welfare)";
    public string Name => "จำนวนผู้เข้าร่วมประกันกลุ่มตามแผน";
    public string? Description => "จำนวนพนักงานที่มีสิทธิ์ประกันกลุ่ม (สถานะใช้งาน) แยกตามแผนประกัน";

    public IReadOnlyList<ReportParameter> Parameters => Array.Empty<ReportParameter>();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plans = await context.Pay_InsurancePlans
            .Where(p => p.CompanyId == ctx.CompanyId)
            .Select(p => new { p.Id, p.PlanName })
            .ToListAsync(ct);
        var planName = plans.ToDictionary(p => p.Id, p => p.PlanName);
        var planIds = plans.Select(p => p.Id).ToList();

        var enrollments = await context.Pay_EmployeeInsuranceEnrollments
            .Where(e => e.IsActive && planIds.Contains(e.PlanId))
            .Select(e => e.PlanId)
            .ToListAsync(ct);

        var counts = enrollments.GroupBy(p => p).ToDictionary(g => g.Key, g => g.Count());

        var rows = plans
            .OrderByDescending(p => counts.TryGetValue(p.Id, out var c) ? c : 0)
            .Select(p => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["plan"] = p.PlanName,
                ["count"] = counts.TryGetValue(p.Id, out var c) ? c : 0,
            }).ToList();

        var totals = new Dictionary<string, object?> { ["plan"] = "รวมทั้งหมด", ["count"] = enrollments.Count };

        return new ReportResult(
            "จำนวนผู้เข้าร่วมประกันกลุ่มตามแผน",
            new[]
            {
                new ReportColumn("plan", "แผนประกัน"),
                new ReportColumn("count", "จำนวนผู้เข้าร่วม (คน)", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"บริษัท {ctx.CompanyId} · เฉพาะสถานะใช้งาน · ณ {DateTime.Now:dd/MM/yyyy}");
    }
}
