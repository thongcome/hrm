namespace Advance.Payroll.Contracts;

// Replaces the Wel_BenefitType / Wel_Entitlement / Pos_PositionSlot block in
// PayrollCalculationService.cs:368-383 and 565-594 (monthly recurring allowances, e.g.
// ค่ารถ, resolved company-default -> position-override -> individual-override via
// HRM.Services.Welfare.WelfareEntitlementResolver.Pick). The per-employee-per-benefit
// resolution logic (position lookup, override precedence) stays in HRM's implementation
// of this interface — Advance.Payroll never sees Wel_*/Pos_PositionSlot, only the
// already-resolved amount per employee, which is exactly what it needs to emit an
// earning line.
// Advance.Payroll Lite: plan section 5 calls this "รายการจ่ายประจำต่อคน" — a flat
// per-employee recurring-pay-items screen with no position/benefit-type layer, which
// implements this interface trivially (no resolution, just a lookup table).
public interface IAllowanceFeed
{
    // asOfDate: normally the payroll run's PeriodStart — an allowance is "as configured
    // for the pay period", matching the original's un-dated Wel_Entitlement.IsActive read.
    Task<IReadOnlyDictionary<long, IReadOnlyList<AllowanceLine>>> GetMonthlyAllowancesAsync(
        string companyId, DateOnly asOfDate, CancellationToken ct = default);
}

// Mirrors the Pay Element catalog flags (Pay_PayItemType) that PayrollCalculationService
// applies to each allowance line: IsTaxable, IsSsoWageBase, IsProvidentFundWageBase,
// IsProrated (scaled by the same working-day factor as base salary for a mid-month joiner).
// PayItemTypeCode must match an existing Pay_PayItemType.Code (falls back to "ALLOWANCE"
// if unmatched, same as the original's `payItemTypesById.TryGetValue(...) ?? payItemTypes["ALLOWANCE"]`).
public sealed record AllowanceLine(
    string? PayItemTypeCode,
    string Name,
    decimal Amount,
    bool IsTaxable,
    bool IsSsoWageBase,
    bool IsProvidentFundWageBase,
    bool IsProrated,
    string SourceRefTable,
    long SourceRefId);
