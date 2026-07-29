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

            if (assignment is not null)
            {
                var expectedIn = group.Key.WorkDate.ToDateTime(assignment.ShiftDefinition.StartTime);
                var expectedOut = assignment.ShiftDefinition.EndTime < assignment.ShiftDefinition.StartTime
                    ? group.Key.WorkDate.AddDays(1).ToDateTime(assignment.ShiftDefinition.EndTime)
                    : group.Key.WorkDate.ToDateTime(assignment.ShiftDefinition.EndTime);

                isLate = firstIn > expectedIn;
                isEarlyLeave = lastOut < expectedOut;
            }
            else if (settings.TrackingMode == AttTrackingMode.SimpleInOut && settings.DefaultWorkStart.HasValue && settings.DefaultWorkEnd.HasValue)
            {
                var expectedIn = group.Key.WorkDate.ToDateTime(settings.DefaultWorkStart.Value);
                var expectedOut = group.Key.WorkDate.ToDateTime(settings.DefaultWorkEnd.Value);

                isLate = firstIn > expectedIn;
                isEarlyLeave = lastOut < expectedOut;
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
                IsAbsent = false,
                WorkLocation = workLocation,
            });
        }

        await context.SaveChangesAsync(ct);

        return new AttendanceAggregationResult(groups.Count);
    }
}
