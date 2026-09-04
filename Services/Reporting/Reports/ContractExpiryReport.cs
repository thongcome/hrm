using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting.Reports;

// Active contracts whose end date (CT_Contract.expired_date) falls within a
// configurable window ahead — the renewal follow-up list.
//
// NOTE ON THE SOURCE TABLE: CT_Contract is the legacy vendor/procurement
// contract table carried over from the epms/vms scaffold (po_no, vendor_code,
// vendor_name, ProjectAmount, BankGuarantee/InsurancePolicy blocks). It has NO
// employee link (no HremployeeId / EmpNo) and NO CompanyId column, so the
// originally-requested employee-oriented columns (รหัสพนักงาน / ชื่อ-สกุล) do
// not exist here and the report cannot be scoped by ctx.CompanyId. Columns are
// therefore mapped to the real contract-party fields and the data spans all
// companies. If "employment contracts nearing expiry" was the true intent, a
// different (employee-keyed) contract table is needed.
public class ContractExpiryReport(IDbContextFactory<HRMContext> dbFactory) : IReportDefinition
{
    public string Code => "contract-expiry";
    public string Category => "สัญญา (Contracts)";
    public string Name => "สัญญาจัดซื้อ/ผู้ขายที่ใกล้หมดอายุ";
    public string? Description => "สัญญา (จัดซื้อ/ผู้ขาย) ที่จะหมดอายุภายในจำนวนวันที่กำหนด — จาก CT_Contract (ไม่ใช่สัญญาจ้างพนักงาน)";

    public IReadOnlyList<ReportParameter> Parameters => new[]
    {
        new ReportParameter("days", "ภายในกี่วันข้างหน้า", ReportParamType.Number,
            DefaultValue: "90", HelperText: "ค่าเริ่มต้น 90 วัน"),
    };

    public async Task<ReportResult> RunAsync(IReadOnlyDictionary<string, string?> args, ReportContext ctx, CancellationToken ct = default)
    {
        var days = 90;
        if (args.TryGetValue("days", out var daysStr) && int.TryParse(daysStr, out var d) && d > 0)
            days = d;

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var until = today.AddDays(days);

        // CT_Contract has no CompanyId and no employee link, so the query cannot
        // be scoped by ctx.CompanyId — pull active contracts with an end date,
        // then do the date-window math in memory (DateOnly arithmetic).
        var contracts = await context.CT_Contracts
            .Where(c => c.isActive && c.expired_date != null)
            .Select(c => new { c.constract_no, c.po_no, c.vendor_name, c.requesterOrg, c.expired_date })
            .ToListAsync(ct);

        var ordered = contracts
            .Where(c => c.expired_date >= today && c.expired_date <= until)
            .OrderBy(c => c.expired_date)
            .ToList();

        var rows = ordered.Select(c => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["contractNo"] = c.constract_no ?? c.po_no ?? "—",
            ["party"] = string.IsNullOrWhiteSpace(c.vendor_name) ? "—" : c.vendor_name,
            ["org"] = string.IsNullOrWhiteSpace(c.requesterOrg) ? "—" : c.requesterOrg,
            ["expiry"] = c.expired_date.HasValue ? c.expired_date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            ["daysLeft"] = c.expired_date.HasValue ? c.expired_date.Value.DayNumber - today.DayNumber : (int?)null,
        }).ToList();

        var totals = new Dictionary<string, object?> { ["contractNo"] = "รวม", ["party"] = $"{ordered.Count} สัญญา" };

        return new ReportResult(
            "สัญญาจัดซื้อ/ผู้ขายที่ใกล้หมดอายุ",
            new[]
            {
                new ReportColumn("contractNo", "เลขที่สัญญา"),
                new ReportColumn("party", "คู่สัญญา"),
                new ReportColumn("org", "หน่วยงานผู้ร้องขอ"),
                new ReportColumn("expiry", "วันหมดอายุ", ReportColumnType.Date),
                new ReportColumn("daysLeft", "เหลือ (วัน)", ReportColumnType.Number),
            },
            rows, totals,
            Subtitle: $"หมดอายุภายใน {days} วัน · {today:dd/MM/yyyy} – {until:dd/MM/yyyy} (ทุกบริษัท — CT_Contract ไม่มี CompanyId)");
    }
}
