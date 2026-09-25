using HRM.Services.Leave;

namespace HRM.Services.Pay.Calculators;

// Which days of approved leave fall inside the attendance window, and how much of each is paid.
// Payroll uses it two ways:
//   · monthly staff — unpaid leave is wages not earned: deducted at the same daily rate as an
//     absence, before tax and before the SSO/PF base (it used to be pushed by hand as an
//     after-tax one-off item, so the employee paid tax and SSO on money they never got).
//   · daily-wage staff — a paid leave day (sick ม.57, annual ม.30, personal ม.34) is a paid day
//     even with no punch; an unpaid leave day is not.
// Only company working days count, the same rule the absence detector uses; a half-day request
// counts 0.5, and one date never adds up past a whole day.
public static class LeavePayCalculator
{
    public sealed record ApprovedLeave(long HremployeeId, DateOnly Start, DateOnly End, bool IsHalfDay, bool IsPaid);

    public sealed record LeaveDay(DateOnly Date, decimal PaidFraction, decimal UnpaidFraction);

    public static List<LeaveDay> DaysInWindow(IEnumerable<ApprovedLeave> leaves, DateOnly from, DateOnly to,
        IReadOnlySet<DateOnly> holidays, int? workDaysMask)
    {
        var days = new Dictionary<DateOnly, (decimal Paid, decimal Unpaid)>();
        foreach (var leave in leaves)
        {
            var start = leave.Start > from ? leave.Start : from;
            var end = leave.End < to ? leave.End : to;
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                if (LeaveDayCalculator.CalculateWorkingDays(d, d, holidays, workDaysMask) <= 0) continue;
                var cur = days.GetValueOrDefault(d);
                var room = 1m - cur.Paid - cur.Unpaid;
                if (room <= 0) continue;
                var fraction = Math.Min(room, leave.IsHalfDay ? 0.5m : 1m);
                days[d] = leave.IsPaid ? (cur.Paid + fraction, cur.Unpaid) : (cur.Paid, cur.Unpaid + fraction);
            }
        }
        return days.OrderBy(kv => kv.Key).Select(kv => new LeaveDay(kv.Key, kv.Value.Paid, kv.Value.Unpaid)).ToList();
    }
}
