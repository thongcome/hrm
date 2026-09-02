namespace HRM.Services.Welfare;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Resolves the effective welfare entitlement for one employee against a
// benefit type — the amount and claim-count cap that actually apply to THEM,
// after layering the Wel_Entitlement override rules (Employee > Position > All)
// on top of the Wel_BenefitType default. This is what phase-2 claims/balance
// will call so each person's limit is their own, not a flat company number.
//
// The layering itself (Pick) is a pure static so it can be unit-tested without
// a database — same "expose the pure decision" convention as
// ProgramRoleService.ResolveRights and WorkflowEngineService.EvaluateLevel.
public class WelfareEntitlementResolver(IDbContextFactory<HRMContext> dbFactory)
{
    // The effective entitlement for an employee. Amount is interpreted per the
    // benefit's EntitlementMode (annual limit or per-event limit). SourceScope
    // says which level won, for display ("จากตำแหน่ง" / "เฉพาะบุคคล" / "ค่าเริ่มต้น").
    public record Effective(decimal? Amount, int? MaxClaimsPerYear, WelfareEntitlementScope SourceScope, string? SourceNote);

    public async Task<Effective> ResolveAsync(string companyId, long benefitTypeId, long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var benefit = await context.Wel_BenefitTypes.FirstOrDefaultAsync(b => b.Id == benefitTypeId && b.CompanyId == companyId, ct);
        if (benefit is null) return new(null, null, WelfareEntitlementScope.All, null);

        // Employee's current position — same source EmployeeDetail / LMS use:
        // Pos_PositionSlot.HremployeeId, never a direct field on Hremployee.
        var posExecTypeId = await context.Pos_PositionSlots
            .Where(s => s.HremployeeId == hremployeeId && s.IsActive)
            .Select(s => (long?)s.PosExecTypeId)
            .FirstOrDefaultAsync(ct);

        var rules = await context.Wel_Entitlements
            .Where(r => r.CompanyId == companyId && r.BenefitTypeId == benefitTypeId && r.IsActive)
            .ToListAsync(ct);

        return Pick(benefit, rules, posExecTypeId, hremployeeId);
    }

    // Pure most-specific-wins resolution. Each override field independently
    // inherits from the less-specific level when null, so a Position rule can
    // set only the amount and still take the benefit-type's default cap.
    public static Effective Pick(Wel_BenefitType benefit, IEnumerable<Wel_Entitlement> rules, long? posExecTypeId, long hremployeeId)
    {
        // Benefit-type default = the least-specific baseline.
        var baseAmount = benefit.EntitlementMode switch
        {
            WelfareEntitlementMode.AnnualAmount => benefit.AnnualLimitAmount,
            WelfareEntitlementMode.PerEventAmount => benefit.PerEventLimitAmount,
            _ => (decimal?)null,
        };
        decimal? amount = baseAmount;
        int? maxClaims = benefit.MaxClaimsPerYear;
        var scope = WelfareEntitlementScope.All;
        string? note = null;

        // Apply least-specific → most-specific so later (more specific) non-null
        // overrides win, while nulls leave the inherited value intact.
        var all = rules.FirstOrDefault(r => r.Scope == WelfareEntitlementScope.All);
        var position = posExecTypeId is long pid
            ? rules.FirstOrDefault(r => r.Scope == WelfareEntitlementScope.Position && r.PosExecTypeId == pid)
            : null;
        var employee = rules.FirstOrDefault(r => r.Scope == WelfareEntitlementScope.Employee && r.HremployeeId == hremployeeId);

        foreach (var rule in new[] { all, position, employee })
        {
            if (rule is null) continue;
            if (rule.OverrideAmount is not null) amount = rule.OverrideAmount;
            if (rule.OverrideMaxClaimsPerYear is not null) maxClaims = rule.OverrideMaxClaimsPerYear;
            scope = rule.Scope;   // the most specific rule seen so far
            note = rule.Note;
        }

        return new Effective(amount, maxClaims, scope, note);
    }
}
