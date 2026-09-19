using System.Linq.Expressions;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// The one definition of "who is paid in a period" (CEO, 18 ก.ย. 2569: ตอนคำนวณเงินเดือน ต้องตรวจ
// วันที่เข้า วันที่ออก สถานะ และ IsActive — และต้องแยกได้ว่าคนที่ไม่ได้เงินเพราะเงินเดือน 0, ลาออก
// หรือไม่ใช่พนักงานที่รับเงินเดือน เพราะฐานมีพนักงานหลายประเภท). PayrollCalculationService and
// PayrollPreflightService both use InPeriod, so the engine and the pre-flight never disagree.
//
//  · Hire date   WorkDate must exist and be on/before the period end (no date = never paid).
//  · Leave date  ResignDate empty, or on/after the period start (the leaver's last period is paid).
//  · Dates sane  ResignDate on/after WorkDate — a leave date before the hire date is bad data.
//  · Status      IsActive — HR's own switch for people who must not be paid right now
//                (suspension, unpaid leave, secondment). It is never flipped by resignation.
//  · Type        EMPTYPE_CODE whose Pos_EmployeeType row says IsPaidByPayroll = false (e.g.
//                directors) is not a payroll person at all. An unknown/blank type IS paid —
//                dropping someone because a lookup is missing would be the worse mistake.
//
// Hremployee.EMP_STATUS is NOT used: a legacy column that nothing writes, empty on every row,
// and typed decimal(2,2) so it could not hold a status code anyway.
public static class PayrollEligibility
{
    public enum Reason { NoHireDate, ResignBeforeHire, Inactive, NotPayrollType }

    public static async Task<List<string>> LoadNonPayrollTypeCodesAsync(HRMContext context, string companyId, CancellationToken ct = default) =>
        await context.Pos_EmployeeTypes
            .Where(t => t.CompanyId == companyId && t.IsActive && !t.IsPaidByPayroll && t.Code != null)
            .Select(t => t.Code!)
            .ToListAsync(ct);

    public static Expression<Func<Hremployee, bool>> InPeriod(string companyId, DateTime periodStart, DateTime periodEnd,
        IReadOnlyCollection<string> nonPayrollTypeCodes) =>
        e => e.companyid == companyId
             && e.IsActive
             && (e.EmptypeCode == null || !nonPayrollTypeCodes.Contains(e.EmptypeCode))
             && e.WorkDate != null && e.WorkDate <= periodEnd
             && (e.ResignDate == null || (e.ResignDate >= periodStart && e.ResignDate >= e.WorkDate));

    // Company employees whose dates touch the period but who are left out — the pre-flight
    // lists every one of them with its reason, so nobody is dropped from a run silently.
    public static Expression<Func<Hremployee, bool>> ExcludedButRelevant(string companyId, DateTime periodStart, DateTime periodEnd,
        IReadOnlyCollection<string> nonPayrollTypeCodes) =>
        e => e.companyid == companyId
             && (e.ResignDate == null || e.ResignDate >= periodStart)
             && (e.WorkDate == null || e.WorkDate <= periodEnd)
             && (!e.IsActive || e.WorkDate == null || (e.ResignDate != null && e.ResignDate < e.WorkDate)
                 || (e.EmptypeCode != null && nonPayrollTypeCodes.Contains(e.EmptypeCode)));

    public static Reason? WhyExcluded(bool isActive, DateTime? workDate, DateTime? resignDate, string? empTypeCode,
        IReadOnlyCollection<string> nonPayrollTypeCodes)
    {
        if (empTypeCode is not null && nonPayrollTypeCodes.Contains(empTypeCode)) return Reason.NotPayrollType;
        if (workDate is null) return Reason.NoHireDate;
        if (resignDate is not null && resignDate < workDate) return Reason.ResignBeforeHire;
        if (!isActive) return Reason.Inactive;
        return null;
    }
}
