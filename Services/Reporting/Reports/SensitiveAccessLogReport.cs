using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// The PDPA / พ.ร.บ.คอมพิวเตอร์ compliance view: every sensitive-data access or
// change in a date range, with actor + action + entity + IP + timestamp — the
// audit trail the law requires be retained. AuditLog is system-wide (no
// CompanyId), so this report is not company-scoped; that is intentional.
public class SensitiveAccessLogReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    private const int MaxRows = 1000;

    public string Code => "sensitive-access-log";
    public string Category => "Audit / PDPA";
    public string Name => "บันทึกการเข้าถึงข้อมูลอ่อนไหว (PDPA)";
    public string? Description => "รายการเข้าถึง/แก้ไขข้อมูลส่วนบุคคลที่อ่อนไหว ตามช่วงวันที่ (พ.ร.บ.คอมพิวเตอร์) — ผู้กระทำ/การกระทำ/IP/เวลา";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("from", "ตั้งแต่วันที่", ReportParamType.Date, Required: true,
            DefaultValue: new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd")),
        new ReportParameter("to", "ถึงวันที่", ReportParamType.Date, Required: true,
            DefaultValue: DateTime.Today.ToString("yyyy-MM-dd")),
    };

    private static string ActionLabel(AuditActionType a) => a switch
    {
        AuditActionType.Create => "เพิ่ม",
        AuditActionType.Update => "แก้ไข",
        AuditActionType.Delete => "ลบ",
        AuditActionType.View => "เข้าดู",
        _ => a.ToString(),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var from = args.TryGetValue("from", out var f) && DateTime.TryParse(f, out var ff)
            ? ff.Date : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var to = args.TryGetValue("to", out var t) && DateTime.TryParse(t, out var tt)
            ? tt.Date.AddDays(1).AddTicks(-1) : DateTime.Today.AddDays(1).AddTicks(-1);

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var logs = await context.AuditLogs
            .Where(l => l.IsSensitiveDataAccess && l.EventDate >= from && l.EventDate <= to)
            .OrderByDescending(l => l.EventDate)
            .Take(MaxRows)
            .Select(l => new { l.EventDate, l.ActorName, l.ActorUserId, l.Action, l.EntityType, l.RecordId, l.IpAddress })
            .ToListAsync(ct);

        var rows = logs.Select(l => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["when"] = l.EventDate.ToString("dd/MM/yyyy HH:mm"),
            ["actor"] = string.IsNullOrWhiteSpace(l.ActorName) ? (l.ActorUserId?.ToString() ?? "-") : l.ActorName,
            ["action"] = ActionLabel(l.Action),
            ["entity"] = l.EntityType,
            ["record"] = l.RecordId ?? "-",
            ["ip"] = l.IpAddress ?? "-",
        }).ToList();

        return new ReportResult(
            "บันทึกการเข้าถึงข้อมูลอ่อนไหว (PDPA)",
            new[]
            {
                new ReportColumn("when", "วันเวลา"),
                new ReportColumn("actor", "ผู้กระทำ"),
                new ReportColumn("action", "การกระทำ"),
                new ReportColumn("entity", "ประเภทข้อมูล"),
                new ReportColumn("record", "รหัสระเบียน"),
                new ReportColumn("ip", "IP"),
            },
            rows, Totals: null,
            Subtitle: $"ช่วง {from:dd/MM/yyyy} - {to:dd/MM/yyyy} · พบ {rows.Count} รายการ{(rows.Count >= MaxRows ? $" (แสดงสูงสุด {MaxRows})" : "")}");
    }
}
