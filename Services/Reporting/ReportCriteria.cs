using HRM.Models;
using HRM.Models.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Reporting;

// Shared search criteria for the Report Center, so every employee-based report
// filters the same way instead of each one inventing its own parameters.
//
//   dept    — a unit in the org tree; includes every unit below it
//             (com_organization.orgcodefull prefix, same rule the Leave team
//             calendar uses via OrgEmployeeResolverHelper)
//   emptype — employment type (Hremployee.EmptypeCode → Pos_EmployeeTypes)
//   status  — working / resigned / all, judged by ResignDate the way the
//             headcount reports already do
//
// A report opts in by concatenating Standard(...) onto its own parameters and
// passing its Hremployee query through ApplyEmployeeAsync. The viewer fills the
// dropdowns from OptionsAsync when a report does not supply its own, so a
// report needs no IReportDynamicOptions just to offer these three.
public static class ReportCriteria
{
    public const string DeptKey = "dept";
    public const string EmpTypeKey = "emptype";
    public const string StatusKey = "status";

    public const string StatusWorking = "working";
    public const string StatusResigned = "resigned";
    public const string StatusAll = "all";

    public static IReadOnlyList<ReportParameter> Standard(bool dept = true, bool empType = true, string? statusDefault = null)
    {
        var list = new List<ReportParameter>();
        if (dept)
            list.Add(new ReportParameter(DeptKey, "หน่วยงาน", ReportParamType.Organization,
                HelperText: "เลือกหน่วยงานแล้วรวมหน่วยงานย่อยทั้งหมด — เว้นว่าง = ทุกหน่วยงาน"));
        if (empType)
            list.Add(new ReportParameter(EmpTypeKey, "ประเภทการจ้าง", ReportParamType.Select,
                HelperText: "เว้นว่าง = ทุกประเภท"));
        if (statusDefault is not null)
            list.Add(new ReportParameter(StatusKey, "สถานะพนักงาน", ReportParamType.Select,
                DefaultValue: statusDefault, Options: StatusOptions));
        return list;
    }

    public static readonly IReadOnlyList<ReportParamOption> StatusOptions = new[]
    {
        new ReportParamOption(StatusWorking, "ยังทำงานอยู่"),
        new ReportParamOption(StatusResigned, "ลาออก/พ้นสภาพแล้ว"),
        new ReportParamOption(StatusAll, "ทั้งหมด"),
    };

    public static string? Arg(IReadOnlyDictionary<string, string?> args, string key)
        => args.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v!.Trim() : null;

    // Applies whichever of dept / emptype / status the caller supplied. company
    // scope is NOT added here — every report already filters companyid itself.
    public static async Task<IQueryable<Hremployee>> ApplyEmployeeAsync(
        HRMContext db, IQueryable<Hremployee> query, IReadOnlyDictionary<string, string?> args, CancellationToken ct = default)
    {
        if (Arg(args, DeptKey) is { } dept)
        {
            var prefix = await db.com_organizations
                .Where(o => o.code == dept)
                .Select(o => o.orgcodefull)
                .FirstOrDefaultAsync(ct);
            // an unknown unit returns nothing rather than silently widening to everyone
            query = string.IsNullOrWhiteSpace(prefix)
                ? query.Where(_ => false)
                : query.Where(e => e.orgcodefull != null && e.orgcodefull.StartsWith(prefix));
        }

        if (Arg(args, EmpTypeKey) is { } type)
            query = query.Where(e => e.EmptypeCode == type);

        switch (Arg(args, StatusKey))
        {
            case StatusWorking:
                query = query.Where(e => e.ResignDate == null);
                break;
            case StatusResigned:
                query = query.Where(e => e.ResignDate != null);
                break;
        }
        return query;
    }

    // One-line description of the active criteria for the report subtitle, so a
    // printed/exported report says what it was filtered by.
    public static async Task<string?> DescribeAsync(
        HRMContext db, string companyId, IReadOnlyDictionary<string, string?> args, CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (Arg(args, DeptKey) is { } dept)
        {
            var name = await db.com_organizations.Where(o => o.code == dept).Select(o => o.name).FirstOrDefaultAsync(ct);
            parts.Add($"หน่วยงาน {name ?? dept}");
        }
        if (Arg(args, EmpTypeKey) is { } type)
        {
            var name = await db.Pos_EmployeeTypes
                .Where(t => t.CompanyId == companyId && t.Code == type).Select(t => t.Name).FirstOrDefaultAsync(ct);
            parts.Add($"ประเภทการจ้าง {name ?? type}");
        }
        var status = Arg(args, StatusKey);
        if (status is not null)
            parts.Add(StatusOptions.FirstOrDefault(o => o.Value == status)?.Label ?? status);
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    // Dropdown contents for the standard keys; empty for any key it does not own.
    // The leading "ทั้งหมด" row lets the user clear a selection (MudSelect has no clear).
    public static async Task<IReadOnlyList<ReportParamOption>> OptionsAsync(
        HRMContext db, string key, ReportContext ctx, CancellationToken ct = default)
    {
        var all = new ReportParamOption("", "ทั้งหมด");
        switch (key)
        {
            case DeptKey:
            {
                var orgs = await db.com_organizations
                    .Where(o => o.code != null && o.isActive && o.orgcodefull != null)
                    .OrderBy(o => o.orgcodefull)
                    .Select(o => new { o.code, o.name, o.orgcodefull })
                    .ToListAsync(ct);
                // indent by depth so the tree shape is visible in a flat dropdown
                var opts = orgs.Select(o => new ReportParamOption(
                    o.code!, new string(' ', Math.Max(0, (o.orgcodefull!.Length / 2 - 1)) * 2) + (o.name ?? o.code!)));
                return new[] { all }.Concat(opts).ToList();
            }
            case EmpTypeKey:
            {
                var types = await db.Pos_EmployeeTypes
                    .Where(t => t.CompanyId == ctx.CompanyId && t.Code != null)
                    .OrderBy(t => t.Code)
                    .Select(t => new ReportParamOption(t.Code!, t.Code + " — " + (t.Name ?? t.Code)))
                    .ToListAsync(ct);
                return new[] { all }.Concat(types).ToList();
            }
            default:
                return Array.Empty<ReportParamOption>();
        }
    }
}
