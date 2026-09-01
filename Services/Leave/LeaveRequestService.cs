namespace HRM.Services.Leave;

using System.Globalization;
using HRM.Models;
using HRM.Services.Pay;
using HRM.Services.Shared;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Leave request lifecycle: create draft (day-count + overlap-check) -> submit
// into LEAVE_APPROVAL workflow -> (optional) attach medical certificate ->
// once approved and the leave type is unpaid, push a real payroll deduction.
// Centralizes logic that used to live inline in LeaveRequestList.razor, same
// extract-once-it-grows precedent as Services/Pay/EmployeeRehireService.cs.
public class LeaveRequestService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine, PrivateFileStorage fileStorage)
{
    private const string WorkflowCode = "LEAVE_APPROVAL";
    private const string UnpaidLeavePayItemCode = "LEAVE_UNPAID";
    // Historic code kept verbatim (rows + Endpoints/LeaveFileEndpoints.cs
    // depend on it) even though it now stores ANY required leave attachment
    // (หมายเรียก, ใบฎีกา, ...) — the human-facing name lives in
    // Lve_LeaveType.AttachmentDocName.
    public const string AttachmentDocTypeCode = "LEAVE_MEDCERT";
    private static readonly string[] NonBlockingStatuses = ["REJECTED", "RETURNED", WorkflowEngineService.StatusCancelled];

    public async Task<long> CreateDraftAsync(long hremployeeId, int leaveTypeId, DateOnly start, DateOnly end,
        bool isHalfDay, HalfDayPeriod? halfDayPeriod, string? reason, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        var leaveType = await context.Lve_LeaveTypes.FirstOrDefaultAsync(t => t.Id == leaveTypeId && t.IsActive, ct)
            ?? throw new InvalidOperationException("ไม่พบประเภทการลานี้ หรือประเภทการลาถูกปิดใช้งานแล้ว");

        // --- Catalog-rule enforcement (service layer — never trust the UI alone) ---

        // ApplicableGender: "M"/"F"/null, same single-char convention as
        // Hremployee.Sex. An employee with NO recorded sex passes — missing
        // legacy data must not strip entitlements (matches the form filter).
        if (!string.IsNullOrWhiteSpace(leaveType.ApplicableGender)
            && !string.IsNullOrWhiteSpace(emp.Sex)
            && !string.Equals(emp.Sex, leaveType.ApplicableGender, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(leaveType.ApplicableGender == "F"
                ? $"{leaveType.NameTh}ใช้ได้เฉพาะพนักงานหญิงเท่านั้น"
                : $"{leaveType.NameTh}ใช้ได้เฉพาะพนักงานชายเท่านั้น");
        }

        // Half-day: blocked both when the type disallows it outright and when
        // the type must be taken as one consecutive block (MustBeConsecutive
        // types are all AllowHalfDay=false by seed data, but guard both flags
        // so a misconfigured row still fails safe).
        if (isHalfDay && (!leaveType.AllowHalfDay || leaveType.MustBeConsecutive))
            throw new InvalidOperationException($"{leaveType.NameTh}ไม่สามารถลาครึ่งวันได้ ต้องลาเต็มวัน");

        var today = DateOnly.FromDateTime(DateTime.Today);

        if (!leaveType.AllowRetroactive && start < today)
            throw new InvalidOperationException($"{leaveType.NameTh}ไม่สามารถลาย้อนหลังได้ — วันที่เริ่มลาต้องไม่ก่อนวันนี้");

        if (leaveType.AdvanceNoticeDays is int noticeDays && noticeDays > 0)
        {
            var earliestAllowed = today.AddDays(noticeDays);
            if (start < earliestAllowed)
                throw new InvalidOperationException(
                    $"{leaveType.NameTh}ต้องแจ้งล่วงหน้าอย่างน้อย {noticeDays} วัน — วันที่เริ่มลาที่เร็วที่สุดคือ {earliestAllowed:dd/MM/yyyy}");
        }

        // OncePerEmployment: any earlier request of this type that wasn't
        // rejected/returned/cancelled uses up the once-in-a-lifetime right.
        if (leaveType.EntitlementFrequency == LeaveEntitlementFrequency.OncePerEmployment)
        {
            var priorRequests = await context.Lve_LeaveRequests
                .Where(r => r.HremployeeId == hremployeeId && r.LeaveTypeId == leaveTypeId)
                .Select(r => new { r.Id, r.JobMasterId })
                .ToListAsync(ct);
            if (priorRequests.Count > 0)
            {
                var priorJobIds = priorRequests.Where(r => r.JobMasterId is not null).Select(r => r.JobMasterId!.Value).ToList();
                var priorStatuses = await context.job_masters.Where(j => priorJobIds.Contains(j.jobmasterid))
                    .ToDictionaryAsync(j => j.jobmasterid, j => j.status, ct);
                var stillCounts = priorRequests.Any(r =>
                    r.JobMasterId is null || !NonBlockingStatuses.Contains(priorStatuses.GetValueOrDefault(r.JobMasterId.Value)));
                if (stillCounts)
                    throw new InvalidOperationException($"{leaveType.NameTh}: สิทธิ์นี้ใช้ได้ครั้งเดียวตลอดอายุงาน — คุณเคยยื่นคำขอประเภทนี้ไปแล้ว");
            }
        }

        var policy = await context.Lve_LeavePolicies
            .FirstOrDefaultAsync(p => p.CompanyId == emp.companyid && p.LeaveTypeId == leaveTypeId, ct);
        if (policy?.MinServiceMonths is int minMonths)
        {
            var monthsOfService = TenureHelper.MonthsOfService(emp.WorkDate, DateOnly.FromDateTime(DateTime.Today));
            if ((monthsOfService ?? 0) < minMonths)
            {
                var eligibleDate = DateOnly.FromDateTime(emp.WorkDate ?? DateTime.Today).AddMonths(minMonths);
                throw new InvalidOperationException($"ประเภทการลานี้ต้องมีอายุงานอย่างน้อย {minMonths} เดือน — จะมีสิทธิ์วันที่ {eligibleDate:dd/MM/yyyy}");
            }
        }

        if (isHalfDay)
            end = start;
        if (end < start)
            throw new InvalidOperationException("วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มลา");

        await EnsureNoOverlapAsync(context, hremployeeId, start, end, excludeRequestId: null, ct);

        var totalDays = isHalfDay ? 0.5m : await CalculateDurationAsync(context, leaveType.DayCountMethod, emp.companyid, start, end, ct);

        var request = new Lve_LeaveRequest
        {
            HremployeeId = emp.id,
            EmpNo = emp.EmpNo,
            CompanyId = emp.companyid,
            LeaveTypeId = leaveTypeId,
            StartDate = start,
            EndDate = end,
            TotalDays = totalDays,
            IsHalfDay = isHalfDay,
            HalfDayPeriod = isHalfDay ? halfDayPeriod : null,
            Reason = reason,
        };
        context.Lve_LeaveRequests.Add(request);
        await context.SaveChangesAsync(ct);
        return request.Id;
    }

    public async Task<long> SubmitAsync(long requestId, long actorUserId, string? actorEmpNo, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var request = await context.Lve_LeaveRequests.Include(r => r.Lve_LeaveType).FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้");
        if (request.JobMasterId is not null)
            throw new InvalidOperationException("คำขอนี้ถูกส่งเข้าสู่กระบวนการอนุมัติไปแล้ว");

        // Required-attachment gate: when the catalog names a document and the
        // request's duration reaches the threshold (AttachmentMinDays, null =
        // from day 1), submission is blocked until a doc_center row exists.
        // Generalizes the old sick-leave-only medical-certificate hint —
        // doctypecode stays "LEAVE_MEDCERT" so existing rows/endpoints keep
        // working, but the document's NAME now comes from the catalog.
        var leaveType = request.Lve_LeaveType;
        if (!string.IsNullOrWhiteSpace(leaveType.AttachmentDocName)
            && request.TotalDays >= (leaveType.AttachmentMinDays ?? 0m))
        {
            var hasAttachment = request.MedCertDocCenterId is not null
                || await context.doc_centers.AnyAsync(d => d.refid == requestId && d.doctypecode == AttachmentDocTypeCode && d.isActive == true, ct);
            if (!hasAttachment)
                throw new InvalidOperationException(leaveType.AttachmentMinDays is decimal minDays && minDays > 0
                    ? $"การลา{leaveType.NameTh}ตั้งแต่ {minDays:0.#} วันขึ้นไปต้องแนบ{leaveType.AttachmentDocName}ก่อนส่งขออนุมัติ"
                    : $"ต้องแนบ{leaveType.AttachmentDocName}ก่อนส่งขออนุมัติ");
        }

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == WorkflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{WorkflowCode}' — ตรวจสอบว่า migration ถูก apply แล้ว");

        var subject = $"ขอลา: {request.EmpNo} {request.Lve_LeaveType.NameTh} {request.StartDate:dd/MM/yyyy}-{request.EndDate:dd/MM/yyyy}";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Lve_LeaveRequest", requestId.ToString(),
            actorUserId, actorEmpNo, subject, null, ct);

        request.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);
        return jobId;
    }

    public async Task AttachMedCertAsync(long requestId, string fileName, string relativePath, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var request = await context.Lve_LeaveRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้");

        var doc = new doc_center
        {
            refid = requestId,
            doctypecode = AttachmentDocTypeCode,
            files = fileName,
            path = relativePath,
            isActive = true,
            moddate = DateTime.Now,
        };
        context.doc_centers.Add(doc);
        await context.SaveChangesAsync(ct);

        request.MedCertDocCenterId = doc.id;
        await context.SaveChangesAsync(ct);
    }

    // HR-triggered, mirrors OtRequestList.razor's PushToPayrollAsync exactly:
    // manual button click after workflow COMPLETED, guarded against double-push
    // by AdhocPayItemId, writes into the same Pay_AdhocPayItem pipeline
    // PayrollCalculationService already consumes automatically — no changes
    // needed there.
    public async Task<long> PushUnpaidToPayrollAsync(long requestId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var request = await context.Lve_LeaveRequests.Include(r => r.Lve_LeaveType).FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้");
        if (request.AdhocPayItemId is not null)
            throw new InvalidOperationException("ผลักเข้าระบบเงินเดือนไปแล้ว");

        var approved = request.JobMasterId is not null &&
            await context.job_masters.AnyAsync(j => j.jobmasterid == request.JobMasterId && j.isJobClosed == true && j.status == WorkflowEngineService.StatusCompleted, ct);
        if (!approved)
            throw new InvalidOperationException("คำขอนี้ยังไม่ผ่านการอนุมัติ");

        var policy = await context.Lve_LeavePolicies.FirstOrDefaultAsync(p => p.CompanyId == request.CompanyId && p.LeaveTypeId == request.LeaveTypeId, ct);
        if (policy is null || policy.IsPaid)
            throw new InvalidOperationException("ประเภทการลานี้เป็นการลาแบบได้รับค่าจ้าง ไม่ต้องผลักเข้าระบบเงินเดือน");

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == request.HremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        var payItemType = await context.Pay_PayItemTypes.FirstOrDefaultAsync(t => t.Code == UnpaidLeavePayItemCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบประเภทรายการเงินเดือน '{UnpaidLeavePayItemCode}' — ตรวจสอบว่า migration ถูก apply แล้ว");

        // Day rate = monthly salary / calendar days in the leave's month —
        // matches the "YYYYMM" monthly-period grain Pay_AdhocPayItem.TargetPeriod
        // and Pay_PayrollRun.PayrollPeriod already use throughout Pay_*.
        var daysInMonth = DateTime.DaysInMonth(request.StartDate.Year, request.StartDate.Month);
        var dayRate = (emp.SalaryAmt ?? 0m) / daysInMonth;
        var amount = Math.Round(dayRate * request.TotalDays, 2, MidpointRounding.AwayFromZero);

        var adhocItem = new Pay_AdhocPayItem
        {
            HremployeeId = emp.id,
            PayItemTypeId = payItemType.Id,
            // InvariantCulture is load-bearing: on a Thai-locale OS the
            // default calendar renders the Buddhist year ("2569xx"), which
            // never matches any Gregorian "yyyyMM" payroll period.
            TargetPeriod = request.StartDate.ToString("yyyyMM", CultureInfo.InvariantCulture),
            Amount = amount,
            IsTaxable = false,
            Reason = $"หักเงินลาไม่รับค่าจ้าง ({request.Lve_LeaveType.NameTh} {request.StartDate:dd/MM/yyyy}-{request.EndDate:dd/MM/yyyy}, {request.TotalDays:0.#} วัน)",
            Status = PayAdhocItemStatus.Approved,
            RequestedByUserId = actorUserId,
            ApprovedByUserId = actorUserId,
            ApprovedDate = DateTime.Now,
        };
        context.Pay_AdhocPayItems.Add(adhocItem);
        await context.SaveChangesAsync(ct);

        request.AdhocPayItemId = adhocItem.Id;
        await context.SaveChangesAsync(ct);
        return adhocItem.Id;
    }

    // Draft (never submitted) is hard-deleted — no approval history exists to
    // preserve. A submitted-but-still-open request is closed via the shared
    // WorkflowEngineService.CancelAsync primitive instead (see that method's
    // comment) and the Lve_LeaveRequest row itself is left untouched — its
    // "status" was always derived from job_master.status, so CANCELLED just
    // becomes one more value that flows through automatically.
    public async Task CancelAsync(long requestId, long actorUserId, bool isAdmin, string? reason, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var request = await context.Lve_LeaveRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้");

        if (request.JobMasterId is null)
        {
            context.Lve_LeaveRequests.Remove(request);
            await context.SaveChangesAsync(ct);
            return;
        }

        await engine.CancelAsync(request.JobMasterId.Value, actorUserId, isAdmin, reason, ct);
    }

    public async Task EnsureNoOverlapAsync(HRMContext context, long hremployeeId, DateOnly start, DateOnly end, long? excludeRequestId, CancellationToken ct)
    {
        var overlapping = await context.Lve_LeaveRequests
            .Where(r => r.HremployeeId == hremployeeId && (excludeRequestId == null || r.Id != excludeRequestId.Value)
                && r.StartDate <= end && r.EndDate >= start)
            .ToListAsync(ct);
        if (overlapping.Count == 0) return;

        var jobIds = overlapping.Where(r => r.JobMasterId is not null).Select(r => r.JobMasterId!.Value).ToList();
        var statuses = await context.job_masters.Where(j => jobIds.Contains(j.jobmasterid)).ToDictionaryAsync(j => j.jobmasterid, j => j.status, ct);

        // A request still blocks the range unless its job resolved to a
        // non-blocking terminal status (rejected/returned) — Draft
        // (JobMasterId null) and anything still pending/approved counts as
        // "the date is spoken for."
        var blocking = overlapping.FirstOrDefault(r =>
            r.JobMasterId is null || !NonBlockingStatuses.Contains(statuses.GetValueOrDefault(r.JobMasterId.Value)));
        if (blocking is not null)
            throw new InvalidOperationException($"ช่วงวันที่นี้ทับกับคำขอลา #{blocking.Id} ที่มีอยู่แล้ว");
    }

    // Duration for a request, honoring the leave type's DayCountMethod:
    // CalendarDays = plain inclusive calendar count (no holiday/workday
    // exclusion — Thai-law convention for maternity/ordination/military),
    // WorkingDays = the existing LeaveDayCalculator path below.
    public static async Task<decimal> CalculateDurationAsync(HRMContext context, LeaveDayCountMethod dayCountMethod,
        string? companyId, DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        if (end < start) return 0m;
        return dayCountMethod == LeaveDayCountMethod.CalendarDays
            ? end.DayNumber - start.DayNumber + 1
            : await CalculateWorkingDaysAsync(context, companyId, start, end, ct);
    }

    public static async Task<decimal> CalculateWorkingDaysAsync(HRMContext context, string? companyId, DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        var holidayDates = await context.Lve_CompanyHolidays
            .Where(h => h.CompanyId == companyId && h.IsActive && h.HolidayDate >= start && h.HolidayDate <= end)
            .Select(h => h.HolidayDate)
            .ToListAsync(ct);

        var workDaysMask = (await context.Lve_CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct))?.WorkDaysMask;

        return LeaveDayCalculator.CalculateWorkingDays(start, end, holidayDates.ToHashSet(), workDaysMask);
    }
}
