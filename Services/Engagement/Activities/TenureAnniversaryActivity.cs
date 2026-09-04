using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Engagement.Activities;

// Awards points on each completed work anniversary. One event per whole year
// reached (keyed by "<empId>:<years>"), so an employee earns once per year of
// service as each anniversary passes.
public class TenureAnniversaryActivity(IDbContextFactory<HRMContext> dbFactory) : IPointEarningActivity
{
    public string Code => "TENURE_ANNIVERSARY";
    public string Name => "ครบรอบปีการทำงาน";
    public string HowEarned => "มอบอัตโนมัติเมื่อพนักงานทำงานครบรอบปี (1 ครั้งต่อ 1 ปีที่ครบ นับจากวันเริ่มงาน)";

    public async Task<IReadOnlyList<PointEarnEvent>> DetectAsync(HRMContext context, string companyId, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var emps = await context.Hremployee
            .Where(e => e.companyid == companyId && e.ResignDate == null && e.WorkDate != null)
            .Select(e => new { e.id, e.WorkDate })
            .ToListAsync(ct);

        var events = new List<PointEarnEvent>();
        foreach (var e in emps)
        {
            var wd = e.WorkDate!.Value.Date;
            var years = today.Year - wd.Year;
            if (wd.AddYears(years) > today) years--;
            if (years < 1) continue;
            // one event per completed year mark reached so far (backfills prior years too)
            for (var y = 1; y <= years; y++)
                events.Add(new PointEarnEvent(e.id, "TenureAnniversary", $"{e.id}:{y}", $"ครบ {y} ปีการทำงาน"));
        }
        return events;
    }
}
