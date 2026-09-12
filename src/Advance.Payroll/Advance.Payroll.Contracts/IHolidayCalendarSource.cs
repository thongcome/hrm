namespace Advance.Payroll.Contracts;

// Replaces Lve_CompanySettings.WorkDaysMask + Lve_CompanyHolidays reads in
// PayrollCalculationService.cs:246-251 (used only by the daily-wage "WorkingDays" mode
// via HRM.Services.Leave.LeaveDayCalculator.CalculateWorkingDays, PayrollCalculationService.cs:489).
// Advance.Payroll Lite owns its own tiny pay_holiday / pay_workday_setting tables per
// the plan (section 3.1) — no leave module needed for this one calendar fact.
public interface IHolidayCalendarSource
{
    Task<CompanyCalendar> GetCalendarAsync(
        string companyId, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct = default);
}

// WorkDaysMask: bit flags Sun=1..Sat=64, matching Lve_CompanySetting.WorkDaysMask's
// existing convention (see HRM.Services.Leave.LeaveDayCalculator) — carried over
// unchanged so this isn't a second competing convention.
public sealed record CompanyCalendar(int WorkDaysMask, IReadOnlySet<DateOnly> Holidays);
