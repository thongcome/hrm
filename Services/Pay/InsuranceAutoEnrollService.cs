using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// Bulk-enrolls eligible employees into a Pay_InsurancePlan in one shot, driven
// by HR clicking "ซิงค์สิทธิ์ตอนนี้" — no scheduler, no event-driven trigger.
// Builds every new Pay_EmployeeInsuranceEnrollment in memory and calls
// SaveChangesAsync once at the end, mirroring PayrollCalculationService.CalculateAsync
// so this scales to "tens of thousands of employees" without per-row saves.
public class InsuranceAutoEnrollService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<int> PreviewEligibleCountAsync(long planId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var eligibleIds = await ResolveEligibleEmployeeIdsAsync(context, planId, ct);
        return eligibleIds.Count;
    }

    public async Task<int> SyncNowAsync(long planId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var plan = await context.Pay_InsurancePlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) throw new InvalidOperationException("ไม่พบแผนประกันที่ต้องการซิงค์");
        if (!plan.AutoEnrollEnabled) throw new InvalidOperationException("แผนนี้ยังไม่ได้เปิดใช้งาน Auto-enroll");

        var eligibleIds = await ResolveEligibleEmployeeIdsAsync(context, planId, ct);
        if (eligibleIds.Count == 0) return 0;

        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var employeeId in eligibleIds)
        {
            context.Pay_EmployeeInsuranceEnrollments.Add(new Pay_EmployeeInsuranceEnrollment
            {
                HremployeeId = employeeId,
                PlanId = plan.Id,
                EmployeeAmount = plan.DefaultEmployeeAmount,
                CompanyAmount = plan.DefaultCompanyAmount,
                EffectiveFrom = today,
                IsActive = true,
                NeedsReview = true,
                EnrolledByUserId = actorUserId,
            });
        }

        await context.SaveChangesAsync(ct);
        return eligibleIds.Count;
    }

    // Shared by preview + sync so the count HR sees before confirming always
    // matches what actually gets written — same eligibility rules, same
    // mutual-exclusion check that EmployeeInsuranceAdmin.razor's manual
    // EnrollAsync uses (HremployeeId+PlanId active already => skip).
    private static async Task<List<long>> ResolveEligibleEmployeeIdsAsync(HRMContext context, long planId, CancellationToken ct)
    {
        var plan = await context.Pay_InsurancePlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return new List<long>();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var eligibleTypes = string.IsNullOrWhiteSpace(plan.EligibleEmploymentTypes)
            ? null
            : plan.EligibleEmploymentTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

        var candidates = await context.Hremployee
            .Where(e => e.companyid == plan.CompanyId && e.ResignDate == null)
            .Select(e => new { e.id, e.EmptypeCode, e.WorkDate, e.BirthDate })
            .ToListAsync(ct);

        var alreadyEnrolledIds = await context.Pay_EmployeeInsuranceEnrollments
            .Where(en => en.PlanId == planId && en.IsActive)
            .Select(en => en.HremployeeId)
            .ToListAsync(ct);
        var alreadyEnrolledSet = alreadyEnrolledIds.ToHashSet();

        var eligibleIds = new List<long>();
        foreach (var emp in candidates)
        {
            if (alreadyEnrolledSet.Contains(emp.id)) continue;

            if (eligibleTypes is not null && (emp.EmptypeCode is null || !eligibleTypes.Contains(emp.EmptypeCode)))
                continue;

            if (plan.MinTenureDays is int minTenureDays)
            {
                if (emp.WorkDate is null) continue;
                var tenureDays = today.DayNumber - DateOnly.FromDateTime(emp.WorkDate.Value).DayNumber;
                if (tenureDays < minTenureDays) continue;
            }

            if (plan.MinAge is not null || plan.MaxAge is not null)
            {
                if (emp.BirthDate is null) continue;
                var birthDate = DateOnly.FromDateTime(emp.BirthDate.Value);
                var age = today.Year - birthDate.Year;
                if (birthDate > today.AddYears(-age)) age--;

                if (plan.MinAge is int min && age < min) continue;
                if (plan.MaxAge is int max && age > max) continue;
            }

            eligibleIds.Add(emp.id);
        }

        return eligibleIds;
    }
}
