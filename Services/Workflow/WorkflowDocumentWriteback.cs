namespace HRM.Services.Workflow;

using HRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ── เขียนผลกลับไปที่เอกสารของโมดูลตอนปิดงาน ─────────────────────────────────
//
//  CEO, 11 ก.ย. 2569: "workflow ตอนกด อนุมัติ/Decline แล้ว ต้องเอามาปรับปรุงที่ table/refid
//  ด้วย แล้วถือว่าสิ้นสุดงาน"
//
//  เดิมโมดูลต้อง "มาอ่านเอง" ตอนเปิดหน้า (SyncStatusFromJobAsync แบบ lazy) — เอกสารจึงเปลี่ยน
//  สถานะช้ากว่าการอนุมัติ และถ้าไม่มีใครเปิดหน้าก็ไม่เปลี่ยนเลย ตอนนี้ engine ทั้งสองตัวเรียก
//  NotifyClosedAsync ทันทีที่ปิดงาน (approve / decline / cancel / auto-approve) แล้ว dispatcher
//  หา handler ตาม job.reftable ให้โมดูลนั้นเขียนผลลงแถวของตัวเอง (job.refid)
//
//  แนวคิด: engine ไม่รู้จักตารางโมดูล (ยังคง 5 ตารางของตัวเอง) — รู้แค่ว่า reftable ไหนมี
//  "ตัวเขียนผล" ตัวไหน handler ใช้ method ของโมดูลที่มีอยู่แล้ว (idempotent, กันซ้ำด้วยสถานะ
//  PendingApproval) การอ่านแบบ lazy ตอนเปิดหน้ายังอยู่เป็นตาข่ายรองรับ ถ้า handler ล้มเหลว
//  งานถือว่าปิดแล้ว (commit ไปแล้ว) แต่จะบันทึก audit ไว้ว่าเขียนผลกลับไม่สำเร็จ
public enum WorkflowCloseOutcome { Approved, Declined, Cancelled }

public sealed record WorkflowClosedEvent(
    long JobMasterId, string WorkflowCode, string RefTable, string RefId,
    WorkflowCloseOutcome Outcome, long? ActorUserId, string? Reason);

public interface IWorkflowDocumentHandler
{
    string RefTable { get; }
    Task OnClosedAsync(IServiceProvider services, WorkflowClosedEvent e, CancellationToken ct);
}

// handler ธรรมดา: ผูก reftable กับ delegate ที่เรียก service ของโมดูล
public sealed class DelegatingDocumentHandler : IWorkflowDocumentHandler
{
    private readonly Func<IServiceProvider, WorkflowClosedEvent, CancellationToken, Task> _apply;
    public DelegatingDocumentHandler(string refTable, Func<IServiceProvider, WorkflowClosedEvent, CancellationToken, Task> apply)
    {
        RefTable = refTable;
        _apply = apply;
    }
    public string RefTable { get; }
    public Task OnClosedAsync(IServiceProvider services, WorkflowClosedEvent e, CancellationToken ct) => _apply(services, e, ct);
}

public sealed class WorkflowDocumentWriteback
{
    private readonly IServiceProvider _services;
    private readonly IEnumerable<IWorkflowDocumentHandler> _handlers;
    private readonly HRM.Services.Audit.IAuditLogger _audit;
    private readonly ILogger<WorkflowDocumentWriteback> _logger;

    public WorkflowDocumentWriteback(IServiceProvider services, IEnumerable<IWorkflowDocumentHandler> handlers,
        HRM.Services.Audit.IAuditLogger audit, ILogger<WorkflowDocumentWriteback> logger)
    {
        _services = services;
        _handlers = handlers;
        _audit = audit;
        _logger = logger;
    }

    public static WorkflowCloseOutcome OutcomeOf(string? reasonClosed) => reasonClosed switch
    {
        WorkflowEngineService.ClosedByDecline => WorkflowCloseOutcome.Declined,
        WorkflowEngineService.ClosedByCancel => WorkflowCloseOutcome.Cancelled,
        _ => WorkflowCloseOutcome.Approved,   // Approve / AutoApprove
    };

    // เรียก "หลัง" engine commit การปิดงานแล้ว — handler ของโมดูลเปิด context ของตัวเองและอ่าน
    // job_master ที่ปิดแล้วได้ทันที
    public async Task NotifyClosedAsync(job_master job, CancellationToken ct = default)
    {
        if (job.isJobClosed != true) return;
        if (string.IsNullOrWhiteSpace(job.reftable) || string.IsNullOrWhiteSpace(job.refid)) return;

        var handler = _handlers.FirstOrDefault(h => string.Equals(h.RefTable, job.reftable, StringComparison.OrdinalIgnoreCase));
        if (handler is null)
        {
            // โมดูลที่ไม่มีคอลัมน์สถานะของตัวเอง (ลา, ใบเบิก, สวัสดิการ, วินัย, รางวัล, เครื่องแบบ, OT)
            // อ่านสถานะจาก job_master โดยตรงอยู่แล้ว — ไม่มีอะไรให้เขียน
            _logger.LogDebug("Job {Job}: no write-back handler for {RefTable}; module derives status from job_master", job.jobmasterid, job.reftable);
            return;
        }

        var e = new WorkflowClosedEvent(job.jobmasterid, job.workflowcode ?? "", job.reftable!, job.refid!,
            OutcomeOf(job.reasonClosed), job.approvedUserID, job.remark);
        try
        {
            await handler.OnClosedAsync(_services, e, ct);
            await _audit.LogAccessAsync(job.reftable!, job.refid, isSensitive: false,
                note: $"workflow write-back {e.Outcome} from job {job.jobmasterid}", ct);
        }
        catch (Exception ex)
        {
            // งานปิดไปแล้ว (commit แล้ว) — ห้ามให้ผู้อนุมัติเห็น error ราวกับอนุมัติไม่สำเร็จ
            // แต่ต้องมีร่องรอย: log + audit; หน้าโมดูลจะ sync แบบ lazy ให้อีกครั้งตอนเปิด
            _logger.LogError(ex, "Job {Job}: write-back to {RefTable} #{RefId} failed", job.jobmasterid, job.reftable, job.refid);
            await _audit.LogAccessAsync(job.reftable!, job.refid, isSensitive: false,
                note: $"workflow write-back FAILED from job {job.jobmasterid}: {ex.Message}", ct);
        }
    }
}

// ── ตารางจับคู่ reftable → ตัวเขียนผลของโมดูล ───────────────────────────────
public static class WorkflowDocumentHandlers
{
    private static long Id(WorkflowClosedEvent e) => long.Parse(e.RefId);

    public static IServiceCollection AddWorkflowDocumentHandlers(this IServiceCollection services)
    {
        services.AddScoped<WorkflowDocumentWriteback>();

        void Map(string refTable, Func<IServiceProvider, WorkflowClosedEvent, CancellationToken, Task> apply)
            => services.AddScoped<IWorkflowDocumentHandler>(_ => new DelegatingDocumentHandler(refTable, apply));

        Map("Att_TimesheetSubmission", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Att.TimesheetService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Att_CorrectionRequest", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Att.AttendanceCorrectionService>().SyncAsync(Id(e), ct));
        Map("Idp_Plan", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Idp.IdpPlanService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Km_Article", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Km.KmArticleService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Lms_Enrollment", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Lms.LmsEnrollmentService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Perf_EvaluationInstance", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Perf.PerfApprovalService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Perf_ImprovementPlan", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Perf.PerfImprovementPlanService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Succ_SuccessorNomination", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Succession.SuccessionService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Rec_Offer", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Rec.RecOfferService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Rec_Requisition", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Rec.RecRequisitionService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Pay_ProvidentFundExitCase", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Pay.ProvidentFundExitCaseService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Pay_ProvidentFundRateChangeRequest", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Pay.ProvidentFundRateChangeRequestService>().SyncStatusFromJobAsync(Id(e), ct));
        Map("Wf_WorkflowStateChangeRequest", (sp, e, ct) => sp.GetRequiredService<WorkflowStateChangeService>().ApplyApprovedAsync(ct));
        Map("Org_OrganizationChangeRequest", (sp, e, ct) => sp.GetRequiredService<HRM.Services.Org.OrgChangeRequestService>().ApplyDueChangesAsync(ct));

        // ตารางที่ method ของโมดูลรับ key อื่น (พนักงาน / บริษัท) — อ่านแถวก่อนแล้วค่อยเรียก
        Map("Hr_SeparationRequest", async (sp, e, ct) =>
        {
            await using var db = await sp.GetRequiredService<IDbContextFactory<HRMContext>>().CreateDbContextAsync(ct);
            var empId = await db.Hr_SeparationRequests.Where(r => r.Id == Id(e)).Select(r => (long?)r.HremployeeId).FirstOrDefaultAsync(ct);
            if (empId is long id) await sp.GetRequiredService<HRM.Services.Hr.SeparationRequestService>().SyncStatusFromJobAsync(id, ct);
        });
        Map("Hr_SeparationDateChange", async (sp, e, ct) =>
        {
            await using var db = await sp.GetRequiredService<IDbContextFactory<HRMContext>>().CreateDbContextAsync(ct);
            var empId = await db.Hr_SeparationDateChanges.Where(r => r.Id == Id(e)).Select(r => (long?)r.HremployeeId).FirstOrDefaultAsync(ct);
            if (empId is long id) await sp.GetRequiredService<HRM.Services.Hr.SeparationRequestService>().SyncDateChangeStatusFromJobAsync(id, ct);
        });
        Map("Eng_RedeemRequest", async (sp, e, ct) =>
        {
            await using var db = await sp.GetRequiredService<IDbContextFactory<HRMContext>>().CreateDbContextAsync(ct);
            var company = await db.Eng_RedeemRequests.Where(r => r.Id == Id(e)).Select(r => r.CompanyId).FirstOrDefaultAsync(ct);
            if (company is not null) await sp.GetRequiredService<HRM.Services.Engagement.EngagementService>().SyncRedeemsAsync(company, ct);
        });

        return services;
    }
}
