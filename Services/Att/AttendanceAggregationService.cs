namespace HRM.Services.Att;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

public record AttendanceAggregationResult(int DaysProcessed);

// Computes Att_DailyAttendance from raw Att_PunchLog rows (+ Att_ShiftAssignment
// if the company uses shifts). Always safe to re-run for a date range — it
// wipes and rebuilds the summary rows rather than accumulating.
public class AttendanceAggregationService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public AttendanceAggregationService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<AttendanceAggregationResult> RunAsync(string companyId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var settings = await context.Att_CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        if (settings is null || settings.TrackingMode == AttTrackingMode.None)
            return new AttendanceAggregationResult(0);

        var fromDt = fromDate.ToDateTime(TimeOnly.MinValue);
        var toDt = toDate.ToDateTime(TimeOnly.MaxValue);

        var punches = await context.Att_PunchLogs
            .Where(p => p.CompanyId == companyId && p.PunchTime >= fromDt && p.PunchTime <= toDt)
            .ToListAsync(ct);

        var shiftAssignments = settings.TrackingMode == AttTrackingMode.ShiftBased
            ? await context.Att_ShiftAssignments
                .Include(a => a.ShiftDefinition)
                .Where(a => a.WorkDate >= fromDate && a.WorkDate <= toDate)
                .ToListAsync(ct)
            : new List<Att_ShiftAssignment>();

        // existing summary rows in range get replaced wholesale
        var existing = await context.Att_DailyAttendances
            .Where(a => a.CompanyId == companyId && a.WorkDate >= fromDate && a.WorkDate <= toDate)
            .ToListAsync(ct);
        context.Att_DailyAttendances.RemoveRange(existing);

        var groups = punches
            .GroupBy(p => (p.HremployeeId, WorkDate: DateOnly.FromDateTime(p.PunchTime)))
            .ToList();

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(p => p.PunchTime).ToList();
            var firstIn = ordered.First().PunchTime;
            var lastOut = ordered.Last().PunchTime;
            var workedMinutes = (int)(lastOut - firstIn).TotalMinutes;

            var assignment = shiftAssignments.FirstOrDefault(a => a.HremployeeId == group.Key.HremployeeId && a.WorkDate == group.Key.WorkDate);

            bool isLate = false;
            bool isEarlyLeave = false;
            int lateMinutes = 0, earlyLeaveMinutes = 0;

            if (assignment is not null)
            {
                // Flexible shift: only the core hours are enforced (REQ-066).
                var shift = assignment.ShiftDefinition;
                var startRef = shift.IsFlexible && shift.CoreStartTime is TimeOnly cs ? cs : shift.StartTime;
                var endRef = shift.IsFlexible && shift.CoreEndTime is TimeOnly ce ? ce : shift.EndTime;
                var expectedIn = group.Key.WorkDate.ToDateTime(startRef);
                var expectedOut = endRef < startRef
                    ? group.Key.WorkDate.AddDays(1).ToDateTime(endRef)
                    : group.Key.WorkDate.ToDateTime(endRef);

                isLate = firstIn > expectedIn;
                isEarlyLeave = lastOut < expectedOut;
                lateMinutes = isLate ? (int)Math.Ceiling((firstIn - expectedIn).TotalMinutes) : 0;
                earlyLeaveMinutes = isEarlyLeave ? (int)Math.Ceiling((expectedOut - lastOut).TotalMinutes) : 0;
            }
            else if (settings.TrackingMode == AttTrackingMode.SimpleInOut && settings.DefaultWorkStart.HasValue && settings.DefaultWorkEnd.HasValue)
            {
                var expectedIn = group.Key.WorkDate.ToDateTime(settings.DefaultWorkStart.Value);
                var expectedOut = group.Key.WorkDate.ToDateTime(settings.DefaultWorkEnd.Value);

                isLate = firstIn > expectedIn;
                isEarlyLeave = lastOut < expectedOut;
                lateMinutes = isLate ? (int)Math.Ceiling((firstIn - expectedIn).TotalMinutes) : 0;
                earlyLeaveMinutes = isEarlyLeave ? (int)Math.Ceiling((expectedOut - lastOut).TotalMinutes) : 0;
            }

            var allWfh = ordered.All(p => p.Source == AttPunchSource.WfhSelfCheckin || p.Source == AttPunchSource.ManualEntry);
            var anyWfh = ordered.Any(p => p.Source == AttPunchSource.WfhSelfCheckin);
            var workLocation = allWfh && anyWfh ? AttWorkLocation.Wfh
                : anyWfh ? AttWorkLocation.Mixed
                : AttWorkLocation.Office;

            context.Att_DailyAttendances.Add(new Att_DailyAttendance
            {
                HremployeeId = group.Key.HremployeeId,
                WorkDate = group.Key.WorkDate,
                CompanyId = companyId,
                ShiftDefinitionId = assignment?.ShiftDefinitionId,
                FirstIn = firstIn,
                LastOut = lastOut,
                WorkedMinutes = workedMinutes,
                IsLate = isLate,
                IsEarlyLeave = isEarlyLeave,
                LateMinutes = lateMinutes,
                EarlyLeaveMinutes = earlyLeaveMinutes,
                IsAbsent = false,
                WorkLocation = workLocation,
            });
        }

        // Absence detection (opt-in per company, Att_CompanySetting.DetectAbsence):
        // a scheduled working day with no punch, no approved leave and no company
        // holiday becomes an IsAbsent row, which payroll may deduct per
        // Pay_AttendanceDeductionPolicy. "Scheduled" = has a shift assignment
        // (ShiftBased) or is a company working day per the leave work-days mask
        // (SimpleInOut). Employees count only between start date and resignation.
        var absentRows = 0;
        if (settings.DetectAbsence)
        {
            var punched = groups.Select(g => (g.Key.HremployeeId, g.Key.WorkDate)).ToHashSet();
            var holidays = (await context.Lve_CompanyHolidays
                    .Where(h => h.CompanyId == companyId && h.IsActive && h.HolidayDate >= fromDate && h.HolidayDate <= toDate)
                    .Select(h => h.HolidayDate).ToListAsync(ct)).ToHashSet();
            var workDaysMask = HRM.Services.Leave.LeaveDayCalculator.ResolveWorkDaysMask(
                await context.Lve_CompanySettings.Where(s => s.CompanyId == companyId).Select(s => s.WorkDaysMask).FirstOrDefaultAsync(ct));
            var completed = HRM.Services.Workflow.WorkflowEngineService.StatusCompleted;
            var approvedLeaves = await context.Lve_LeaveRequests
                .Where(l => l.JobMasterId != null && l.StartDate <= toDate && l.EndDate >= fromDate
                            && context.job_masters.Any(j => j.jobmasterid == l.JobMasterId && j.status == completed))
                .Select(l => new { l.HremployeeId, l.StartDate, l.EndDate })
                .ToListAsync(ct);
            var employees = await context.Hremployee
                .Where(e => e.companyid == companyId && e.WorkDate != null && e.WorkDate <= toDt
                            && (e.ResignDate == null || e.ResignDate >= fromDt))
                .Select(e => new { e.id, e.WorkDate, e.ResignDate })
                .ToListAsync(ct);

            for (var d = fromDate; d <= toDate; d = d.AddDays(1))
            {
                if (holidays.Contains(d)) continue;
                var scheduledToday = settings.TrackingMode == AttTrackingMode.ShiftBased
                    ? shiftAssignments.Where(a => a.WorkDate == d).Select(a => a.HremployeeId).ToHashSet()
                    : null; // SimpleInOut: everyone, on company working days
                if (scheduledToday is null && (workDaysMask & (1 << (int)d.DayOfWeek)) == 0) continue;

                foreach (var e in employees)
                {
                    if (scheduledToday is not null && !scheduledToday.Contains(e.id)) continue;
                    if (e.WorkDate is DateTime wd && DateOnly.FromDateTime(wd) > d) continue;
                    if (e.ResignDate is DateTime rd && DateOnly.FromDateTime(rd) < d) continue;
                    if (punched.Contains((e.id, d))) continue;
                    if (approvedLeaves.Any(l => l.HremployeeId == e.id && l.StartDate <= d && l.EndDate >= d)) continue;
                    context.Att_DailyAttendances.Add(new Att_DailyAttendance
                    {
                        HremployeeId = e.id, WorkDate = d, CompanyId = companyId,
                        ShiftDefinitionId = shiftAssignments.FirstOrDefault(a => a.HremployeeId == e.id && a.WorkDate == d)?.ShiftDefinitionId,
                        IsAbsent = true, WorkLocation = AttWorkLocation.Office,
                        Remark = "ไม่มีการลงเวลา ไม่มีใบลาที่อนุมัติ (ระบบตรวจพบอัตโนมัติ)",
                    });
                    absentRows++;
                }
            }
        }

        await context.SaveChangesAsync(ct);

        return new AttendanceAggregationResult(groups.Count + absentRows);
    }
}
