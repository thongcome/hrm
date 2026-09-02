namespace HRM.Services.Welfare;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Computes an employee's remaining welfare entitlement for a benefit in a year:
// the per-person effective limit (WelfareEntitlementResolver — company default →
// position → individual) minus what they have already had APPROVED. "Approved"
// means a claim whose workflow job is COMPLETED (same job_master status join the
// rest of the app uses); drafts and in-flight claims don't consume the balance.
public class WelfareBalanceService(IDbContextFactory<HRMContext> dbFactory, WelfareEntitlementResolver resolver)
{
    public record Balance(
        WelfareEntitlementMode Mode,
        decimal? Limit, decimal UsedAmount, decimal? RemainingAmount,
        int? MaxClaimsPerYear, int UsedClaims, int? RemainingClaims,
        WelfareEntitlementScope SourceScope, string? SourceNote);

    public async Task<Balance> GetAsync(string companyId, long benefitTypeId, long hremployeeId, int year, CancellationToken ct = default)
    {
        var eff = await resolver.ResolveAsync(companyId, benefitTypeId, hremployeeId, ct);

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var benefit = await context.Wel_BenefitTypes.FirstOrDefaultAsync(b => b.Id == benefitTypeId, ct);
        var mode = benefit?.EntitlementMode ?? WelfareEntitlementMode.AnnualAmount;

        var approved = await (
            from c in context.Wel_Claims
            join j in context.job_masters on c.JobMasterId equals j.jobmasterid
            where c.HremployeeId == hremployeeId && c.BenefitTypeId == benefitTypeId
                  && c.EventDate.Year == year && j.status == "COMPLETED"
            select c.Amount).ToListAsync(ct);

        var usedAmount = approved.Sum();
        var usedClaims = approved.Count;

        // For AnnualAmount the limit is a running yearly pool; for PerEvent the
        // "Amount" is a per-claim cap (not a pool), so RemainingAmount is null
        // there and the cap is enforced per claim instead.
        decimal? remainingAmount = mode == WelfareEntitlementMode.AnnualAmount && eff.Amount is decimal lim
            ? lim - usedAmount
            : null;
        int? remainingClaims = eff.MaxClaimsPerYear is int mc ? Math.Max(0, mc - usedClaims) : null;

        return new Balance(mode, eff.Amount, usedAmount, remainingAmount,
            eff.MaxClaimsPerYear, usedClaims, remainingClaims, eff.SourceScope, eff.SourceNote);
    }

    // Validate a proposed claim against the balance. Returns a Thai error, or
    // null if OK. Shared by the ESS submit path and the service.
    public async Task<string?> ValidateClaimAsync(string companyId, long benefitTypeId, long hremployeeId, decimal amount, int year, CancellationToken ct = default)
    {
        if (amount <= 0) return "จำนวนเงินต้องมากกว่า 0";

        var bal = await GetAsync(companyId, benefitTypeId, hremployeeId, year, ct);

        if (bal.RemainingClaims is 0)
            return "ใช้สิทธิ์ครบจำนวนครั้งที่กำหนดต่อปีแล้ว";

        return bal.Mode switch
        {
            WelfareEntitlementMode.AnnualAmount when bal.RemainingAmount is decimal rem && amount > rem
                => $"เกินวงเงินคงเหลือ (คงเหลือ {rem:N0} บาท)",
            WelfareEntitlementMode.PerEventAmount when bal.Limit is decimal cap && amount > cap
                => $"เกินวงเงินต่อครั้ง (สูงสุด {cap:N0} บาท/ครั้ง)",
            _ => null,
        };
    }
}
