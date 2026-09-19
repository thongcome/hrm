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
        new ReportParameter("actor", "ผู้กระทำ (ชื่อ หรือ รหัสผู้ใช้)", ReportParamType.Text,
            HelperText: "พิมพ์บางส่วนของชื่อ หรือเลขรหัสผู้ใช้ — เว้นว่าง = ทุกคน"),
        new ReportParameter("entity", "ประเภทข้อมูล", ReportParamType.Text,
            HelperText: "พิมพ์บางส่วนของชื่อ entity เช่น Hremployee — เว้นว่าง = ทุกประเภท"),
        new ReportParameter("action", "การกระทำ", ReportParamType.Select,
            HelperText: "เว้นว่าง = ทุกการกระทำ", Options: new[]
            {
                new ReportParamOption("", "ทั้งหมด"),
                new ReportParamOption(nameof(AuditActionType.View), ActionLabel(AuditActionType.View)),
                new ReportParamOption(nameof(AuditActionType.Create), ActionLabel(AuditActionType.Create)),
                new ReportParamOption(nameof(AuditActionType.Update), ActionLabel(AuditActionType.Update)),
                new ReportParamOption(nameof(AuditActionType.Delete), ActionLabel(AuditActionType.Delete)),
            }),
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

        var query = context.AuditLogs
            .Where(l => l.IsSensitiveDataAccess && l.EventDate >= from && l.EventDate <= to);
        var actorText = ReportCriteria.Arg(args, "actor");
        if (actorText is not null)
        {
            var actorId = long.TryParse(actorText, out var aid) ? (long?)aid : null;
            query = query.Where(l => (l.ActorName != null && l.ActorName.Contains(actorText))
                                     || (actorId != null && l.ActorUserId == actorId));
        }
        var entityText = ReportCriteria.Arg(args, "entity");
        if (entityText is not null)
            query = query.Where(l => l.EntityType.Contains(entityText));
        if (Enum.TryParse<AuditActionType>(ReportCriteria.Arg(args, "action"), out var actionFilter))
            query = query.Where(l => l.Action == actionFilter);

        var logs = await query
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
            Subtitle: $"ช่วง {from:dd/MM/yyyy} - {to:dd/MM/yyyy}"
                + (actorText is null ? "" : $" · ผู้กระทำ \"{actorText}\"")
                + (entityText is null ? "" : $" · ประเภทข้อมูล \"{entityText}\"")
                + (Enum.TryParse<AuditActionType>(ReportCriteria.Arg(args, "action"), out var act) ? $" · {ActionLabel(act)}" : "")
                + $" · พบ {rows.Count} รายการ{(rows.Count >= MaxRows ? $" (แสดงสูงสุด {MaxRows})" : "")}");
    }
}
