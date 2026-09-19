using HRM.Models;
using HRM.Models.Reporting;
using HRM.Services.Audit;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Full employee roster. Personal data (name + employee code), so viewing is audit-logged as
// sensitive access. Deliberately excludes national ID, birthdate and any salary column.
public class EmployeeRosterReport(IDbContextFactory<HRMContext> dbFactory, IAuditLogger audit) : IReportDefinition
{
    public string Code => "employee-roster";
    public string Category => "กำลังพล (Headcount)";
    public string Name => "ทะเบียนรายชื่อพนักงาน";
    public string? Description => "รายชื่อพนักงานทั้งหมด พร้อมตำแหน่ง หน่วยงาน ประเภทการจ้าง วันเริ่มงาน และสถานะ (ไม่รวมข้อมูลเงินเดือน/เลขบัตรประชาชน) — ข้อมูลส่วนบุคคล PDPA";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("q", "ค้นหา (ชื่อ / นามสกุล / รหัสพนักงาน)", ReportParamType.Text, HelperText: "เว้นว่าง = ทุกคน"),
    }.Concat(ReportCriteria.Standard(statusDefault: ReportCriteria.StatusWorking)).ToList();

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        await audit.LogAccessAsync("Report:employee-roster", ctx.CompanyId, isSensitive: true, note: "PDPA: employee roster", ct: ct);

        var query = context.Hremployee.Where(e => e.companyid == ctx.CompanyId);
        query = await ReportCriteria.ApplyEmployeeAsync(context, query, args, ct);
        query = EntitySearchHelper.ApplyTextSearch(query, ReportCriteria.Arg(args, "q"),
            nameof(Hremployee.EmpNo), nameof(Hremployee.EmpName), nameof(Hremployee.EmpSurname));

        var emps = await query
            .OrderBy(e => e.EmpNo)
            .Select(e => new { e.EmpNo, e.EmpName, e.EmpSurname, e.PosCode, e.orgcode, e.EmptypeCode, e.WorkDate, e.ResignDate, e.IsActive })
            .ToListAsync(ct);

        var posNames = (await context.pos_positions.Select(p => new { p.pos_code, p.name }).ToListAsync(ct))
            .Where(p => p.pos_code != null)
            .GroupBy(p => p.pos_code!).ToDictionary(g => g.Key, g => g.First().name ?? g.Key, StringComparer.OrdinalIgnoreCase);
        var orgNames = (await context.com_organizations.Where(o => o.code != null).Select(o => new { o.code, o.name }).ToListAsync(ct))
            .GroupBy(o => o.code!).ToDictionary(g => g.Key, g => g.First().name ?? g.Key, StringComparer.OrdinalIgnoreCase);
        var typeNames = (await context.Pos_EmployeeTypes.Where(x => x.CompanyId == ctx.CompanyId && x.Code != null)
                .Select(x => new { x.Code, x.Name }).ToListAsync(ct))
            .GroupBy(x => x.Code!).ToDictionary(g => g.Key, g => g.First().Name ?? g.Key, StringComparer.OrdinalIgnoreCase);

        var today = DateTime.Today;
        var rows = emps.Select(e => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["empno"] = e.EmpNo,
            ["name"] = $"{e.EmpName} {e.EmpSurname}".Trim(),
            ["position"] = e.PosCode != null && posNames.TryGetValue(e.PosCode, out var p) ? p : e.PosCode,
            ["dept"] = e.orgcode != null && orgNames.TryGetValue(e.orgcode, out var o) ? o : e.orgcode,
            ["type"] = e.EmptypeCode != null && typeNames.TryGetValue(e.EmptypeCode, out var ty) ? ty : e.EmptypeCode,
            ["hired"] = e.WorkDate,
            ["status"] = e.ResignDate is { } rd && rd.Date <= today ? "ลาออก/พ้นสภาพ"
                : !e.IsActive ? "ระงับชั่วคราว"
                : "ทำงานอยู่",
        }).ToList();

        var crit = await ReportCriteria.DescribeAsync(context, ctx.CompanyId, args, ct);
        var q = ReportCriteria.Arg(args, "q");
        return new ReportResult(
            "ทะเบียนรายชื่อพนักงาน",
            new[]
            {
                new ReportColumn("empno", "รหัสพนักงาน"),
                new ReportColumn("name", "ชื่อ-สกุล"),
                new ReportColumn("position", "ตำแหน่ง"),
                new ReportColumn("dept", "หน่วยงาน"),
                new ReportColumn("type", "ประเภทการจ้าง"),
                new ReportColumn("hired", "วันเริ่มงาน", ReportColumnType.Date),
                new ReportColumn("status", "สถานะ"),
            },
            rows,
            new Dictionary<string, object?> { ["empno"] = "รวม", ["name"] = $"{rows.Count:N0} คน" },
            $"บริษัท {ctx.CompanyId} · PDPA ข้อมูลส่วนบุคคล" + (q is null ? "" : $" · ค้นหา \"{q}\"") + (crit is null ? "" : " · " + crit));
    }
}
