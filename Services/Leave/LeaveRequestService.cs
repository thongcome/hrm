namespace HRM.Services.Leave;

using HRM.Models;
using HRM.Services.Pay;
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
    private static readonly string[] NonBlockingStatuses = ["REJECTED", "RETURNED"];

    public async Task<long> CreateDraftAsync(long hremployeeId, LeaveType leaveType, DateOnly start, DateOnly end,
        bool isHalfDay, HalfDayPeriod? halfDayPeriod, string? reason, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        if (isHalfDay)
            end = start;
        if (end < start)
            throw new InvalidOperationException("วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มลา");

        await EnsureNoOverlapAsync(context, hremployeeId, start, end, excludeRequestId: null, ct);

        var totalDays = isHalfDay ? 0.5m : await CalculateWorkingDaysAsync(context, emp.companyid, start, end, ct);

        var request = new Lve_LeaveRequest
        {
            HremployeeId = emp.id,
            EmpNo = emp.EmpNo,
            CompanyId = emp.companyid,
            LeaveType = leaveType,
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
        var request = await context.Lve_LeaveRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้");
        if (request.JobMasterId is not null)
            throw new InvalidOperationException("คำขอนี้ถูกส่งเข้าสู่กระบวนการอนุมัติไปแล้ว");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == WorkflowCode, ct)
            ?? throw new InvalidOperationException($"ไม่พบ workflow '{WorkflowCode}' — ตรวจสอบว่า migration ถูก apply แล้ว");

        var subject = $"ขอลา: {request.EmpNo} {LeaveTypeLabel(request.LeaveType)} {request.StartDate:dd/MM/yyyy}-{request.EndDate:dd/MM/yyyy}";
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
            doctypecode = "LEAVE_MEDCERT",
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
        var request = await context.Lve_LeaveRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้");
        if (request.AdhocPayItemId is not null)
            throw new InvalidOperationException("ผลักเข้าระบบเงินเดือนไปแล้ว");

        var approved = request.JobMasterId is not null &&
            await context.job_masters.AnyAsync(j => j.jobmasterid == request.JobMasterId && j.isJobClosed == true && j.status == WorkflowEngineService.StatusCompleted, ct);
        if (!approved)
            throw new InvalidOperationException("คำขอนี้ยังไม่ผ่านการอนุมัติ");

        var policy = await context.Lve_LeavePolicies.FirstOrDefaultAsync(p => p.CompanyId == request.CompanyId && p.LeaveType == request.LeaveType, ct);
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
            TargetPeriod = request.StartDate.ToString("yyyyMM"),
            Amount = amount,
            IsTaxable = false,
            Reason = $"หักเงินลาไม่รับค่าจ้าง ({LeaveTypeLabel(request.LeaveType)} {request.StartDate:dd/MM/yyyy}-{request.EndDate:dd/MM/yyyy}, {request.TotalDays:0.#} วัน)",
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

    public static async Task<decimal> CalculateWorkingDaysAsync(HRMContext context, string? companyId, DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        var holidayDates = await context.Lve_CompanyHolidays
            .Where(h => h.CompanyId == companyId && h.IsActive && h.HolidayDate >= start && h.HolidayDate <= end)
            .Select(h => h.HolidayDate)
            .ToListAsync(ct);

        var workDaysMask = (await context.Lve_CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct))?.WorkDaysMask;

        return LeaveDayCalculator.CalculateWorkingDays(start, end, holidayDates.ToHashSet(), workDaysMask);
    }

    public static string LeaveTypeLabel(LeaveType type) => type switch
    {
        LeaveType.Sick => "ลาป่วย",
        LeaveType.Personal => "ลากิจ",
        LeaveType.Vacation => "ลาพักร้อน",
        LeaveType.Maternity => "ลาคลอด",
        LeaveType.Ordination => "ลาบวช",
        _ => "อื่นๆ",
    };
}
