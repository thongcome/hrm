using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Engagement.Activities;

// Awards points when an employee completes an LMS course. One event per
// completed enrollment (keyed by the enrollment id), scoped to the company via
// the employee (Lms_Enrollment has no CompanyId of its own).
public class LmsCompletionActivity(IDbContextFactory<HRMContext> dbFactory) : IPointEarningActivity
{
    public string Code => "LMS_COMPLETION";
    public string Name => "จบหลักสูตรอบรม (LMS)";
    public string HowEarned => "มอบอัตโนมัติเมื่อพนักงานมีสถานะ \"จบแล้ว\" ในหลักสูตร (1 ครั้งต่อ 1 หลักสูตร)";

    public async Task<IReadOnlyList<PointEarnEvent>> DetectAsync(HRMContext context, string companyId, CancellationToken ct = default)
    {
        var rows = await (
            from e in context.Lms_Enrollments
            join emp in context.Hremployee on e.HremployeeId equals emp.id
            where e.Status == EnrollmentStatus.Completed && emp.companyid == companyId
            select new { e.Id, e.HremployeeId }).ToListAsync(ct);

        return rows.Select(r => new PointEarnEvent(r.HremployeeId, "Lms_Enrollment", r.Id.ToString(), "จบหลักสูตรอบรม")).ToList();
    }
}
