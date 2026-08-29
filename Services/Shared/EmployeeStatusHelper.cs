namespace HRM.Services.Shared;

using HRM.Models;

// Hremployee.ResignDate can be a FUTURE date (advance notice — see
// Services/Hr/SeparationRequestService.cs.SubmitAsync, which validates
// nothing about the effective date). That means "ResignDate is not null" is
// NOT the same question as "has this person actually left yet" — most of
// the ~16 places in this codebase that check `ResignDate == null` actually
// want the stricter "no resignation on file at all" meaning (forward-looking
// talent/planning programs like Talent Pool/Succession/Leadership
// Development correctly exclude someone the moment they've given notice,
// even before their last day — that's intentional, not a bug). These two
// helpers exist for the places that need the OTHER, date-aware meaning:
// "can this person transact today" and "have they genuinely departed as of
// today." Do not use these to replace every `ResignDate == null` check in
// the codebase — only where the date-aware distinction is actually needed.
public static class EmployeeStatusHelper
{
    // IsActive is an independent HR toggle unrelated to resignation
    // (suspended pending investigation, extended unpaid leave, seconded
    // elsewhere) — never touched by the resignation/rehire flow itself.
    public static bool CanTransact(Hremployee emp) =>
        emp.IsActive && (emp.ResignDate is null || DateOnly.FromDateTime(DateTime.Today) <= DateOnly.FromDateTime(emp.ResignDate.Value));

    // "Genuinely gone as of today" — used to find rehire candidates. Someone
    // who has merely given advance notice (ResignDate in the future) must
    // NOT match this, or the rehire flow could reset a still-working
    // employee's records by mistake.
    public static bool HasDeparted(Hremployee emp) =>
        emp.ResignDate is not null && emp.ResignDate.Value <= DateTime.Today;
}
