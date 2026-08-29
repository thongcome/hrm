namespace HRM.Services.Pay;

using HRM.Models;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

// Rehires a genuinely-departed employee back onto their ORIGINAL Hremployee
// row — never creates a second row with the same EmpNo (HRMContext.cs's
// HasAlternateKey(companyid, EmpNo) is the FK principal key for legacy
// Hrpayroll/HrwOt, so a duplicate row would break those joins). Callers must
// have already confirmed via EmployeeIdentityHelper.CheckAsync that the
// match is MatchesDepartedEmployee — this service does not re-check ID card
// identity itself.
//
// Tenure treatment controls what happens to Hremployee.WorkDate — every
// tenure calculator in this codebase (severance, PF vesting, probation,
// anomaly detection, ...) reads WorkDate directly with no separate "original
// hire date" field, so this single write is what "continuous vs reset"
// actually means system-wide; no calculator needs to change.
//
// PF fund membership is handled as an independent clock (see
// Pay_ProvidentFundMembershipPeriod's own doc comment) — the rehire form
// lets HR choose Transfer (carry the ORIGINAL JoinDate forward into a new
// open period) or Reset (JoinDate = the rehire date) regardless of which
// tenure treatment was picked for WorkDate, since fund rules and general
// employment tenure are governed by different policies.
public class EmployeeRehireService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task RehireAsync(
        long hremployeeId,
        DateOnly rehireDate,
        TenureTreatment tenureTreatment,
        bool transferPfMembership,
        long actorUserId,
        CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");
        if (!EmployeeStatusHelper.HasDeparted(emp))
            throw new InvalidOperationException("พนักงานคนนี้ยังไม่พ้นสภาพจริง จึงรับกลับเข้างานไม่ได้");

        var priorWorkDate = emp.WorkDate;
        var priorResignDate = emp.ResignDate;
        var rehireDateTime = rehireDate.ToDateTime(TimeOnly.MinValue);

        context.Hrd_EmploymentHistories.Add(new Hrd_EmploymentHistory
        {
            HremployeeId = emp.id,
            OrderType = "กลับเข้าทำงาน",
            EffectiveDate = rehireDate,
            OrderDate = rehireDate,
            PositionStatus = "รับกลับเข้างาน",
            TenureTreatment = tenureTreatment,
            PriorWorkDate = priorWorkDate,
            PriorResignDate = priorResignDate,
            CreatedByUserId = actorUserId,
        });

        emp.ResignDate = null;
        emp.SeparationType = null;
        emp.IsActive = true;
        if (tenureTreatment == TenureTreatment.Reset)
            emp.WorkDate = rehireDateTime;
        // Continuous: WorkDate is left untouched — every tenure calculator
        // downstream keeps counting from the original hire date.

        var pfJoinDate = rehireDate;
        if (transferPfMembership)
        {
            var lastMembership = await context.Pay_ProvidentFundMembershipPeriods
                .Where(p => p.HremployeeId == emp.id)
                .OrderByDescending(p => p.JoinDate)
                .FirstOrDefaultAsync(ct);
            if (lastMembership is not null)
                pfJoinDate = lastMembership.JoinDate;
        }
        context.Pay_ProvidentFundMembershipPeriods.Add(new Pay_ProvidentFundMembershipPeriod
        {
            HremployeeId = emp.id,
            JoinDate = pfJoinDate,
            Note = transferPfMembership ? "โอนอายุสมาชิกกองทุนจากช่วงก่อนหน้า (รับกลับเข้างาน)" : "นับอายุสมาชิกกองทุนใหม่ (รับกลับเข้างาน)",
        });

        await context.SaveChangesAsync(ct);
    }
}
