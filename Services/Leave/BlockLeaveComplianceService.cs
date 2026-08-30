namespace HRM.Services.Leave;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Computes each employee's longest single continuous approved-leave stretch
// in a given year, for the "Block Leave" mandatory-consecutive-leave internal
// control (see Lve_BlockLeavePolicy). Report-only — this never blocks
// anything, it just tells HR/audit who has and hasn't satisfied the policy.
//
// "Continuous" here means leave requests with no gap between them (one
// request's end date immediately followed by the next's start date, or
// overlapping) merged into one block — deliberately NOT bridging two
// separate requests that happen to sit either side of an untaken weekend,
// since that would count leave the employee never actually took. The
// consecutive-length THRESHOLD is measured in working days (via the same
// LeaveDayCalculator used everywhere else in this module), matching how the
// policy is phrased ("N วันทำการ").
public class BlockLeaveComplianceService(IDbContextFactory<HRMContext> dbFactory)
{
    public record ComplianceRow(long HremployeeId, string EmpNo, string EmployeeName, decimal LongestConsecutiveWorkingDays, bool IsCompliant);

    public async Task<List<ComplianceRow>> GetComplianceAsync(string companyId, int year, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var policy = await context.Lve_BlockLeavePolicies.FirstOrDefaultAsync(p => p.CompanyId == companyId, ct);
        var minDays = policy?.MinConsecutiveWorkingDays ?? 5;

        var employees = await context.Hremployee
            .Where(e => e.companyid == companyId && e.ResignDate == null)
            .ToListAsync(ct);
        if (employees.Count == 0) return new();
        var empIds = employees.Select(e => e.id).ToList();

        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);

        var requests = await context.Lve_LeaveRequests
            .Where(r => empIds.Contains(r.HremployeeId) && r.JobMasterId != null
                && r.StartDate <= yearEnd && r.EndDate >= yearStart)
            .ToListAsync(ct);

        var jobIds = requests.Select(r => r.JobMasterId!.Value).ToList();
        var completedJobIds = await context.job_masters
            .Where(j => jobIds.Contains(j.jobmasterid) && j.status == HRM.Services.Workflow.WorkflowEngineService.StatusCompleted)
            .Select(j => j.jobmasterid)
            .ToHashSetAsync(ct);

        var approved = requests.Where(r => completedJobIds.Contains(r.JobMasterId!.Value)).ToList();

        var holidayDates = await context.Lve_CompanyHolidays
            .Where(h => h.CompanyId == companyId && h.IsActive && h.HolidayDate.Year == year)
            .Select(h => h.HolidayDate)
            .ToHashSetAsync(ct);
        var workDaysMask = (await context.Lve_CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct))?.WorkDaysMask;

        var rows = new List<ComplianceRow>();
        foreach (var emp in employees)
        {
            var empRequests = approved.Where(r => r.HremployeeId == emp.id).OrderBy(r => r.StartDate).ToList();
            var longest = 0m;

            DateOnly? blockStart = null;
            DateOnly? blockEnd = null;
            foreach (var r in empRequests)
            {
                if (blockStart is null)
                {
                    blockStart = r.StartDate;
                    blockEnd = r.EndDate;
                }
                else if (r.StartDate <= blockEnd!.Value.AddDays(1))
                {
                    if (r.EndDate > blockEnd.Value) blockEnd = r.EndDate;
                }
                else
                {
                    longest = Math.Max(longest, LeaveDayCalculator.CalculateWorkingDays(blockStart.Value, blockEnd!.Value, holidayDates, workDaysMask));
                    blockStart = r.StartDate;
                    blockEnd = r.EndDate;
                }
            }
            if (blockStart is not null)
                longest = Math.Max(longest, LeaveDayCalculator.CalculateWorkingDays(blockStart.Value, blockEnd!.Value, holidayDates, workDaysMask));

            rows.Add(new ComplianceRow(emp.id, emp.EmpNo, $"{emp.EmpName} {emp.EmpSurname}", longest, longest >= minDays));
        }

        return rows.OrderBy(r => r.IsCompliant).ThenBy(r => r.EmpNo).ToList();
    }
}
