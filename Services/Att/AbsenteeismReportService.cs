namespace HRM.Services.Att;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Computes genuine absenteeism (expected-to-work day with no attendance
// record) — deliberately NOT the same thing as Att_DailyAttendance.IsAbsent,
// which the aggregation service never actually sets true (it only knows
// "punches that exist", never "should have come but didn't"). This service
// cross-references company holidays and approved leave to answer the real
// question. Confirmed with the user: no per-company work-week config exists
// anywhere in the schema, so under SimpleInOut tracking mode "expected work
// day" is hardcoded to Mon-Fri — the caller/UI must disclose this.
public class AbsenteeismReportService(IDbContextFactory<HRMContext> dbFactory)
{
    public record AbsentEmployeeDay(long HremployeeId, string EmpNo, string EmpName, DateOnly Date);

    public record DailyPoint(DateOnly Date, int ExpectedCount, int AbsentCount);

    public record TopAbsentee(long HremployeeId, string EmpNo, string EmpName, int AbsentDays);

    public record AbsenteeismSummary(
        AttTrackingMode TrackingMode,
        int ExpectedWorkDayCount,
        int AbsentDayCount,
        double AbsenteeismRatePercent,
        List<DailyPoint> DailyTrend,
        List<TopAbsentee> TopAbsentees);

    // Returns null when the company hasn't enabled attendance tracking at
    // all — there is no data to compute from, not a zero result.
    public async Task<AbsenteeismSummary?> GetSummaryAsync(string companyId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var setting = await context.Att_CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        if (setting is null || setting.TrackingMode == AttTrackingMode.None)
            return null;

        var employees = await context.Hremployee
            .Where(e => e.companyid == companyId)
            .Select(e => new { e.id, e.EmpNo, e.EmpName, e.EmpSurname, e.WorkDate, e.ResignDate })
            .ToListAsync(ct);

        var holidays = (await context.Lve_CompanyHolidays
            .Where(h => h.CompanyId == companyId && h.IsActive && h.HolidayDate >= fromDate && h.HolidayDate <= toDate)
            .Select(h => h.HolidayDate)
            .ToListAsync(ct))
            .ToHashSet();

        // Approved leave = the leave request's workflow job is fully
        // completed (job_master.status == "COMPLETED" is the whole-document
        // completion sentinel — matches the convention LeaveRequestList.razor
        // already uses for the same join).
        var leaveRows = await (
            from lr in context.Lve_LeaveRequests
            join jm in context.job_masters on lr.JobMasterId equals jm.jobmasterid into jmg
            from jm in jmg.DefaultIfEmpty()
            where lr.EndDate >= fromDate && lr.StartDate <= toDate && jm != null && jm.status == "COMPLETED"
            select new { lr.HremployeeId, lr.StartDate, lr.EndDate })
            .ToListAsync(ct);
        var approvedLeaveByEmployee = leaveRows
            .GroupBy(l => l.HremployeeId)
            .ToDictionary(g => g.Key, g => g.Select(l => (l.StartDate, l.EndDate)).ToList());

        var punchDays = (await context.Att_PunchLogs
            .Where(p => p.CompanyId == companyId && p.PunchTime >= fromDate.ToDateTime(TimeOnly.MinValue) && p.PunchTime < toDate.AddDays(1).ToDateTime(TimeOnly.MinValue))
            .Select(p => new { p.HremployeeId, p.PunchTime })
            .ToListAsync(ct))
            .Select(p => (p.HremployeeId, Date: DateOnly.FromDateTime(p.PunchTime)))
            .ToHashSet();

        HashSet<(long HremployeeId, DateOnly WorkDate)>? scheduledDays = null;
        if (setting.TrackingMode == AttTrackingMode.ShiftBased)
        {
            scheduledDays = (await context.Att_ShiftAssignments
                .Where(s => s.WorkDate >= fromDate && s.WorkDate <= toDate)
                .Select(s => new { s.HremployeeId, s.WorkDate })
                .ToListAsync(ct))
                .Select(s => (s.HremployeeId, s.WorkDate))
                .ToHashSet();
        }

        var absentDays = new List<AbsentEmployeeDay>();
        var dailyExpected = new Dictionary<DateOnly, int>();
        var dailyAbsent = new Dictionary<DateOnly, int>();

        for (var day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday) continue;
            if (holidays.Contains(day)) continue;

            foreach (var emp in employees)
            {
                var hireDate = emp.WorkDate.HasValue ? DateOnly.FromDateTime(emp.WorkDate.Value) : (DateOnly?)null;
                if (hireDate is null || hireDate > day) continue;
                if (emp.ResignDate.HasValue && DateOnly.FromDateTime(emp.ResignDate.Value) < day) continue;

                if (setting.TrackingMode == AttTrackingMode.ShiftBased
                    && (scheduledDays is null || !scheduledDays.Contains((emp.id, day))))
                    continue; // not scheduled to work this day at all

                if (approvedLeaveByEmployee.TryGetValue(emp.id, out var leaves)
                    && leaves.Any(l => l.StartDate <= day && l.EndDate >= day))
                    continue; // on approved leave, not an absence

                dailyExpected[day] = dailyExpected.GetValueOrDefault(day) + 1;

                if (!punchDays.Contains((emp.id, day)))
                {
                    absentDays.Add(new AbsentEmployeeDay(emp.id, emp.EmpNo, $"{emp.EmpName} {emp.EmpSurname}".Trim(), day));
                    dailyAbsent[day] = dailyAbsent.GetValueOrDefault(day) + 1;
                }
            }
        }

        var expectedTotal = dailyExpected.Values.Sum();
        var absentTotal = absentDays.Count;
        var rate = expectedTotal == 0 ? 0 : Math.Round(absentTotal * 100.0 / expectedTotal, 2);

        var trend = dailyExpected.Keys
            .OrderBy(d => d)
            .Select(d => new DailyPoint(d, dailyExpected[d], dailyAbsent.GetValueOrDefault(d)))
            .ToList();

        var topAbsentees = absentDays
            .GroupBy(a => (a.HremployeeId, a.EmpNo, a.EmpName))
            .Select(g => new TopAbsentee(g.Key.HremployeeId, g.Key.EmpNo, g.Key.EmpName, g.Count()))
            .OrderByDescending(t => t.AbsentDays)
            .Take(10)
            .ToList();

        return new AbsenteeismSummary(setting.TrackingMode, expectedTotal, absentTotal, rate, trend, topAbsentees);
    }
}
