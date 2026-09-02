namespace HRM.Services.Leave;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Leave round 4 (deepen existing module, 2 ก.ย. 2569): the Leave module had
// every operational surface (request/approve, policy, types, holidays, team
// calendar, block-leave) but NO reporting — HR could not see, in one place,
// how much leave the company is actually taking, by whom, or where the
// patterns are. This service is the aggregation layer behind
// LeaveAnalyticsDashboard.razor.
//
// Every figure it returns is drill-down-ready per the data-discipline rule
// "no number is a dead end": the dashboard's org rows expand to the employees
// behind them (Employees, pre-loaded here so the drill needs no round-trip),
// and each employee expands to their own request lines
// (GetEmployeeRequestsAsync), each of which links to the source request page.
//
// Company-scoped (payroll_company claim). "Taken" always means an
// APPROVED/COMPLETED request (same job_master status join every other Leave
// aggregate uses); pending requests are counted separately, never as taken.
// Employees are those active for ANY part of the year (same active-in-year
// filter as BlockLeaveComplianceService — a resignee who took leave that year
// still belongs in that year's numbers), so head-count denominators match what
// HR expects for the period.
public class LeaveAnalyticsService(IDbContextFactory<HRMContext> dbFactory)
{
    public record TypeStat(int LeaveTypeId, string Code, string Name, int RequestCount, decimal DaysTaken);
    public record OrgStat(long OrganizationId, string OrgName, int Headcount, int EmployeesWhoTook, decimal DaysTaken, decimal AvgDaysPerHead);
    public record EmployeeStat(long HremployeeId, string EmpNo, string EmployeeName, long? OrganizationId, string OrgName, int RequestCount, decimal DaysTaken);
    public record RequestLine(long RequestId, string? RequestNo, string TypeName, DateOnly StartDate, DateOnly EndDate, decimal TotalDays, string Status);

    public record DashboardData(
        int Headcount, int EmployeesWhoTook, decimal TotalDaysTaken, decimal AvgDaysPerHead,
        int CompletedRequests, int PendingRequests,
        List<TypeStat> ByType, List<OrgStat> ByOrg, List<EmployeeStat> Employees,
        List<EmployeeStat> ZeroLeave, List<EmployeeStat> FrequentAbsence);

    private const string Completed = HRM.Services.Workflow.WorkflowEngineService.StatusCompleted;
    private const string Pending = HRM.Services.Workflow.WorkflowEngineService.StatusPending;

    // An employee with this many separate approved absences in the year is
    // surfaced as a frequency pattern worth a look (the "many short spells"
    // signal absence-management uses), independent of total days.
    private const int FrequentAbsenceSpells = 4;

    public async Task<DashboardData> GetDashboardAsync(string companyId, int year, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);
        var yearStartDt = yearStart.ToDateTime(TimeOnly.MinValue);
        var yearEndDt = yearEnd.ToDateTime(TimeOnly.MaxValue);

        var employees = await context.Hremployee
            .Where(e => e.companyid == companyId
                && (e.WorkDate == null || e.WorkDate <= yearEndDt)
                && (e.ResignDate == null || e.ResignDate >= yearStartDt))
            .Select(e => new { e.id, e.EmpNo, e.EmpName, e.EmpSurname, e.OrganizationId })
            .ToListAsync(ct);

        if (employees.Count == 0)
            return new(0, 0, 0m, 0m, 0, 0, new(), new(), new(), new(), new());

        var empIds = employees.Select(e => e.id).ToList();

        var requests = await context.Lve_LeaveRequests
            .Where(r => empIds.Contains(r.HremployeeId) && r.JobMasterId != null
                && r.StartDate <= yearEnd && r.EndDate >= yearStart)
            .Select(r => new { r.Id, r.HremployeeId, r.LeaveTypeId, r.TotalDays, JobId = r.JobMasterId!.Value })
            .ToListAsync(ct);

        var jobIds = requests.Select(r => r.JobId).Distinct().ToList();
        var jobStatus = await context.job_masters
            .Where(j => jobIds.Contains(j.jobmasterid))
            .Select(j => new { j.jobmasterid, j.status })
            .ToDictionaryAsync(x => x.jobmasterid, x => x.status, ct);

        string? StatusOf(long jobId) => jobStatus.TryGetValue(jobId, out var s) ? s : null;

        var completed = requests.Where(r => StatusOf(r.JobId) == Completed).ToList();
        var pendingCount = requests.Count(r => StatusOf(r.JobId) == Pending);

        var typeMap = await context.Lve_LeaveTypes
            .Select(t => new { t.Id, t.Code, t.NameTh })
            .ToDictionaryAsync(x => x.Id, ct);
        string TypeName(int id) => typeMap.TryGetValue(id, out var t) ? t.NameTh : $"#{id}";
        string TypeCode(int id) => typeMap.TryGetValue(id, out var t) ? t.Code : $"#{id}";

        var orgIds = employees.Where(e => e.OrganizationId != null).Select(e => e.OrganizationId!.Value).Distinct().ToList();
        var orgMap = await context.com_organizations
            .Where(o => orgIds.Contains(o.id))
            .Select(o => new { o.id, o.name })
            .ToDictionaryAsync(x => x.id, x => x.name ?? "-", ct);
        string OrgName(long? id) => id is long v && orgMap.TryGetValue(v, out var n) ? n : "(ไม่ระบุหน่วยงาน)";

        // ---- by leave type ----
        var byType = completed
            .GroupBy(r => r.LeaveTypeId)
            .Select(g => new TypeStat(g.Key, TypeCode(g.Key), TypeName(g.Key), g.Count(), g.Sum(r => r.TotalDays)))
            .OrderByDescending(t => t.DaysTaken)
            .ToList();

        // ---- per employee (drives org drill-down, zero-leave, frequent) ----
        var takenByEmp = completed
            .GroupBy(r => r.HremployeeId)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Days: g.Sum(r => r.TotalDays)));

        var employeeStats = employees
            .Select(e =>
            {
                takenByEmp.TryGetValue(e.id, out var t);
                return new EmployeeStat(e.id, e.EmpNo, $"{e.EmpName} {e.EmpSurname}".Trim(),
                    e.OrganizationId, OrgName(e.OrganizationId), t.Count, t.Days);
            })
            .ToList();

        // ---- by org ----
        var byOrg = employeeStats
            .GroupBy(s => s.OrganizationId)
            .Select(g =>
            {
                var head = g.Count();
                var took = g.Count(s => s.RequestCount > 0);
                var days = g.Sum(s => s.DaysTaken);
                return new OrgStat(g.Key ?? 0, g.First().OrgName, head, took, days,
                    head == 0 ? 0m : Math.Round(days / head, 1, MidpointRounding.AwayFromZero));
            })
            .OrderByDescending(o => o.DaysTaken)
            .ToList();

        var headcount = employees.Count;
        var totalDays = completed.Sum(r => r.TotalDays);
        var whoTook = takenByEmp.Count;

        var zeroLeave = employeeStats.Where(s => s.RequestCount == 0)
            .OrderBy(s => s.OrgName).ThenBy(s => s.EmpNo).ToList();
        var frequent = employeeStats.Where(s => s.RequestCount >= FrequentAbsenceSpells)
            .OrderByDescending(s => s.RequestCount).ThenByDescending(s => s.DaysTaken).ToList();

        return new DashboardData(
            headcount, whoTook, totalDays,
            headcount == 0 ? 0m : Math.Round(totalDays / headcount, 1, MidpointRounding.AwayFromZero),
            completed.Count, pendingCount,
            byType, byOrg, employeeStats.OrderBy(s => s.EmpNo).ToList(), zeroLeave, frequent);
    }

    // Employee → their own request lines (the source-document drill): every
    // request that overlaps the year, with its live workflow status, newest
    // first. Each RequestId links to /leave-requests/detail/{id}.
    public async Task<List<RequestLine>> GetEmployeeRequestsAsync(long hremployeeId, int year, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);

        var requests = await context.Lve_LeaveRequests
            .Where(r => r.HremployeeId == hremployeeId && r.StartDate <= yearEnd && r.EndDate >= yearStart)
            .Select(r => new { r.Id, r.RequestNo, r.LeaveTypeId, TypeName = r.Lve_LeaveType.NameTh, r.StartDate, r.EndDate, r.TotalDays, r.JobMasterId })
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(ct);

        var jobIds = requests.Where(r => r.JobMasterId != null).Select(r => r.JobMasterId!.Value).Distinct().ToList();
        var jobStatus = await context.job_masters
            .Where(j => jobIds.Contains(j.jobmasterid))
            .Select(j => new { j.jobmasterid, j.status })
            .ToDictionaryAsync(x => x.jobmasterid, x => x.status, ct);

        return requests.Select(r => new RequestLine(
            r.Id, r.RequestNo, r.TypeName, r.StartDate, r.EndDate, r.TotalDays,
            r.JobMasterId is null ? "ร่าง"
                : jobStatus.TryGetValue(r.JobMasterId.Value, out var s) ? s ?? "-" : "-"))
            .ToList();
    }
}
