using HRM.Models;
using HRM.Services.Att.Calculators;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Att;

// OT checks shared by every place an OT request is created or submitted, so the rules live in one spot:
//  · minimum rate  — the request's ฿/hour may not be below hourly wage × the company's Att_OtRule multiplier
//                    for that day type (the multiplier is the legal floor; paying less is not allowed)
//  · weekly cap    — OT hours in the Monday–Sunday week, counting requests (draft/pending/approved) and payroll
//                    OT rows that did not come from a request, may not pass Att_OtPolicy.WeeklyCapHours
public class OtPolicyService(IDbContextFactory<HRMContext> dbFactory)
{
    public record RateSuggestion(decimal Rate, OtDayType DayType, decimal Multiplier, decimal HourlyWage);

    public record CapCheck(bool Configured, bool Block, OtWeeklyCap.Result? Result);

    // null = no wage on file or no multiplier configured for that day type (nothing to suggest or enforce)
    public async Task<RateSuggestion?> GetMinimumRateAsync(HRMContext context, Hremployee employee, DateOnly date, CancellationToken ct = default)
    {
        if (employee.SalaryAmt is not decimal salary || salary <= 0) return null;

        var isHoliday = await context.Lve_CompanyHolidays.AnyAsync(
            h => h.CompanyId == employee.companyid && h.IsActive && h.HolidayDate == date, ct);
        var dayType = OtRateCalculator.ClassifyDayType(date, isHoliday);
        var rule = await context.Att_OtRules.FirstOrDefaultAsync(
            r => r.CompanyId == employee.companyid && r.DayType == dayType && r.IsActive, ct);
        if (rule is null) return null;

        var hourly = OtRateCalculator.CalculateHourlyWage(salary);
        return new RateSuggestion(Math.Round(hourly * rule.Multiplier, 2), dayType, rule.Multiplier, hourly);
    }

    public async Task<CapCheck> CheckWeeklyCapAsync(string companyId, string empNo, DateTime start, decimal newHours,
        long? excludeRequestId = null, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var day = DateOnly.FromDateTime(start);
        var policy = await context.Att_OtPolicies.AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.IsActive && p.EffectiveFrom <= day)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
        if (policy is null) return new CapCheck(false, false, null);

        var weekStart = OtWeeklyCap.WeekStart(day);
        var weekFrom = weekStart.ToDateTime(TimeOnly.MinValue);
        var weekTo = weekStart.AddDays(7).ToDateTime(TimeOnly.MinValue);

        // requests in the week, minus the ones a manager has already rejected / the requester withdrew
        var requests = await (
            from r in context.emp_overtime_requests
            join j in context.job_masters on r.jobmasterid equals (long?)j.jobmasterid into jj
            from j in jj.DefaultIfEmpty()
            where r.companyid == companyId && r.empid == empNo && r.starttime >= weekFrom && r.starttime < weekTo
                  && (excludeRequestId == null || r.id != excludeRequestId)
                  && (j == null || (j.status != "REJECTED" && j.status != "CANCELLED"))
            select new { r.id, r.workhour }).ToListAsync(ct);
        var requestDocnos = requests.Select(r => $"OT{r.id:D8}").ToHashSet();

        // payroll OT rows in the week that are not the write-back of one of those requests (imported / HR-keyed)
        var rows = await context.HrwOts
            .Where(o => o.companyid == companyId && o.EmpNo == empNo && o.DateWork != null && o.DateWork >= weekFrom && o.DateWork < weekTo)
            .Select(o => new { o.OtDocno, o.OtPMinute })
            .ToListAsync(ct);

        var existing = requests.Sum(r => r.workhour ?? 0m)
                     + rows.Where(o => !requestDocnos.Contains(o.OtDocno)).Sum(o => (o.OtPMinute ?? 0m) / 60m);
        return new CapCheck(true, policy.BlockOnExceed, OtWeeklyCap.Evaluate(policy.WeeklyCapHours, Math.Round(existing, 2), newHours));
    }
}
