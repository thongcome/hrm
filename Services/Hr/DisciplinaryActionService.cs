using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Hr;

// Disciplinary case pipeline: HR drafts a case against an employee, then submits it
// into the generic Workflow Approval Engine before it becomes official — mirrors
// Exp_ClaimHeader/Lve_LeaveRequest's JobMasterId convention exactly (null = still an
// editable draft; once submitted, status is derived live from job_masters.status).
public class DisciplinaryActionService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public DisciplinaryActionService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<long> CreateDraftAsync(long hremployeeId, DisciplinaryActionType actionType, DateOnly incidentDate,
        string description, string? ruleViolated, int? suspensionDays, long actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("กรุณาระบุรายละเอียดเหตุการณ์");
        if (actionType == DisciplinaryActionType.Suspension && (suspensionDays is null || suspensionDays <= 0))
            throw new InvalidOperationException("กรุณาระบุจำนวนวันพักงาน");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        var item = new Hr_DisciplinaryCase
        {
            HremployeeId = emp.id,
            EmpNo = emp.EmpNo,
            CompanyId = emp.companyid,
            ActionType = actionType,
            IncidentDate = incidentDate,
            Description = description,
            RuleViolated = ruleViolated,
            SuspensionDays = actionType == DisciplinaryActionType.Suspension ? suspensionDays : null,
            CreatedByUserId = actorUserId,
        };
        context.Hr_DisciplinaryCases.Add(item);
        await context.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task AttachEvidenceAsync(long caseId, string fileName, string relativePath, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var exists = await context.Hr_DisciplinaryCases.AnyAsync(c => c.Id == caseId, ct);
        if (!exists) throw new InvalidOperationException("ไม่พบกรณีนี้");

        context.doc_centers.Add(new doc_center
        {
            refid = caseId,
            doctypecode = "HR_DISCIPLINARY",
            files = fileName,
            path = relativePath,
            isActive = true,
            moddate = DateTime.Now,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task<long> SubmitAsync(long caseId, long actorUserId, Workflow.WorkflowEngineService engine, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var item = await context.Hr_DisciplinaryCases.FirstOrDefaultAsync(c => c.Id == caseId, ct)
            ?? throw new InvalidOperationException("ไม่พบกรณีนี้");
        if (item.JobMasterId is not null)
            throw new InvalidOperationException("กรณีนี้ถูกส่งเข้าสู่กระบวนการอนุมัติไปแล้ว");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "DISCIPLINARY_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow DISCIPLINARY_APPROVAL — ตรวจสอบว่า migration ถูก apply แล้ว");

        var subject = $"วินัย: {item.EmpNo} — {ActionTypeLabel(item.ActionType)}";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Hr_DisciplinaryCase", caseId.ToString(),
            actorUserId, item.EmpNo, subject, null, ct);

        item.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);
        return jobId;
    }

    public async Task<List<Hr_DisciplinaryCase>> GetCasesAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Hr_DisciplinaryCases
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);
    }

    public async Task<List<Hr_DisciplinaryCase>> GetCasesForEmployeeAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Hr_DisciplinaryCases
            .Where(c => c.HremployeeId == hremployeeId)
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);
    }

    // Mirrors ExpenseClaimService.GetJobStatusesAsync exactly ("-" = still draft).
    public async Task<Dictionary<long, string>> GetJobStatusesAsync(IEnumerable<long> caseIds, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var jobIds = await context.Hr_DisciplinaryCases
            .Where(c => caseIds.Contains(c.Id) && c.JobMasterId != null)
            .Select(c => new { c.Id, c.JobMasterId })
            .ToListAsync(ct);

        var jobMasterIds = jobIds.Select(x => x.JobMasterId!.Value).ToList();
        var statuses = await context.job_masters
            .Where(j => jobMasterIds.Contains(j.jobmasterid))
            .ToDictionaryAsync(j => j.jobmasterid, j => j.status, ct);

        var result = new Dictionary<long, string>();
        foreach (var x in jobIds)
            result[x.Id] = statuses.TryGetValue(x.JobMasterId!.Value, out var s) ? s : "-";
        return result;
    }

    public static string ActionTypeLabel(DisciplinaryActionType type) => type switch
    {
        DisciplinaryActionType.VerbalWarning => "ตักเตือนด้วยวาจา",
        DisciplinaryActionType.WrittenWarning => "ตักเตือนเป็นลายลักษณ์อักษร",
        DisciplinaryActionType.Suspension => "พักงาน",
        DisciplinaryActionType.Termination => "เลิกจ้าง",
        _ => type.ToString(),
    };
}
