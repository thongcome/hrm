using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Hr;

// Reward/commendation case pipeline — sibling of DisciplinaryActionService,
// mirrors its shape exactly: HR drafts a case for an employee, then submits
// it into the generic Workflow Approval Engine before it becomes official
// (JobMasterId null = still an editable draft; once submitted, status is
// derived live from job_masters.status).
public class RewardCaseService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public RewardCaseService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<long> CreateDraftAsync(long hremployeeId, RewardType rewardType, DateOnly awardDate,
        string description, decimal? amount, long actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("กรุณาระบุรายละเอียดผลงาน/เหตุผลที่ได้รับรางวัล");
        if (rewardType == RewardType.CashBonus && (amount is null || amount <= 0))
            throw new InvalidOperationException("กรุณาระบุจำนวนเงินรางวัล");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        var item = new Hr_RewardCase
        {
            HremployeeId = emp.id,
            EmpNo = emp.EmpNo,
            CompanyId = emp.companyid,
            RewardType = rewardType,
            AwardDate = awardDate,
            Description = description,
            Amount = rewardType == RewardType.CashBonus ? amount : null,
            CreatedByUserId = actorUserId,
        };
        context.Hr_RewardCases.Add(item);
        await context.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task AttachEvidenceAsync(long caseId, string fileName, string relativePath, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var exists = await context.Hr_RewardCases.AnyAsync(c => c.Id == caseId, ct);
        if (!exists) throw new InvalidOperationException("ไม่พบกรณีนี้");

        context.doc_centers.Add(new doc_center
        {
            refid = caseId,
            doctypecode = "HR_REWARD",
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
        var item = await context.Hr_RewardCases.FirstOrDefaultAsync(c => c.Id == caseId, ct)
            ?? throw new InvalidOperationException("ไม่พบกรณีนี้");
        if (item.JobMasterId is not null)
            throw new InvalidOperationException("กรณีนี้ถูกส่งเข้าสู่กระบวนการอนุมัติไปแล้ว");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "REWARD_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow REWARD_APPROVAL — ตรวจสอบว่า migration ถูก apply แล้ว");

        var subject = $"รางวัล: {item.EmpNo} — {RewardTypeLabel(item.RewardType)}";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Hr_RewardCase", caseId.ToString(),
            actorUserId, item.EmpNo, subject, item.Amount, ct);

        item.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);
        return jobId;
    }

    public async Task<List<Hr_RewardCase>> GetCasesAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Hr_RewardCases
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);
    }

    public async Task<List<Hr_RewardCase>> GetCasesForEmployeeAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Hr_RewardCases
            .Where(c => c.HremployeeId == hremployeeId)
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);
    }

    // Mirrors DisciplinaryActionService.GetJobStatusesAsync exactly ("-" = still draft).
    public async Task<Dictionary<long, string>> GetJobStatusesAsync(IEnumerable<long> caseIds, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var jobIds = await context.Hr_RewardCases
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

    public static string RewardTypeLabel(RewardType type) => type switch
    {
        RewardType.Commendation => "ยกย่อง/ชมเชย",
        RewardType.PerformanceAward => "รางวัลผลงานดีเด่น",
        RewardType.LengthOfServiceAward => "รางวัลอายุงาน",
        RewardType.CashBonus => "เงินรางวัลพิเศษ",
        _ => type.ToString(),
    };
}
