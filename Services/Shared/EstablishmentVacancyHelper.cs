namespace HRM.Services.Shared;

// Single source of truth for "is this establishment slot (อัตรา) effectively
// vacant?" — used by every headcount/vacancy surface so the rule can't drift.
//
// Two notions of vacancy:
//   • LIVE (today): a slot with no occupant. Physical release of a departed
//     employee's occupant happens on their effective date via
//     SeparationRequestService.ApplyDueSeparationsAsync (lazy, no scheduler).
//   • AS-OF a future date D (forward planning): a slot is *projected* vacant as
//     of D when it has no occupant OR its occupant's separation effective date
//     (Hremployee.ResignDate) is on/before D. This is a READ-TIME projection —
//     it never mutates the slot; physical release still waits for the real date.
//
// Deterministic date arithmetic — the resign date is a stored, workflow-approved
// fact, so no prediction/AI is involved (AI belongs only to a separate,
// clearly-labelled probabilistic attrition forecast for UNscheduled turnover).
// Boundary: on the effective date itself the seat counts as vacant (ResignDate
// <= D), matching ApplyDueSeparationsAsync's "<= today" physical release.
public static class EstablishmentVacancyHelper
{
    public static bool IsEffectivelyVacantAsOf(long? hremployeeId, DateTime? resignDate, DateOnly asOf)
        => hremployeeId is null
           || (resignDate is not null && DateOnly.FromDateTime(resignDate.Value) <= asOf);

    // Convenience inverse — occupied (counts toward used headcount) as of D.
    public static bool IsEffectivelyOccupiedAsOf(long? hremployeeId, DateTime? resignDate, DateOnly asOf)
        => !IsEffectivelyVacantAsOf(hremployeeId, resignDate, asOf);
}
