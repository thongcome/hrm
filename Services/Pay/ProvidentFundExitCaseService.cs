namespace HRM.Services.Pay;

using HRM.Models;
using HRM.Services.Pay.Calculators;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// A membership-termination case: HR picks a Pay_ProvidentFundExitReasonRule
// (fraud, death, retirement, quit-fund-not-job, ordinary resignation, ...)
// and the case goes through the Workflow Approval Engine. Only once approved
// does the system compute the final vesting ruling — either the reason
// rule's own override (0%/100%, with the age+fund-membership exception
// check), or falling back to the ordinary Pay_ProvidentFundVestingTier table
// exactly as before this feature existed. Same lazy apply-on-read pattern as
// ProvidentFundRateChangeRequestService.
public class ProvidentFundExitCaseService
{
    public const string WorkflowCode = "PVD_EXIT_APPROVAL";

    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly WorkflowEngineService _engine;

    public ProvidentFundExitCaseService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
    {
        _dbFactory = dbFactory;
        _engine = engine;
    }

    public async Task<long> SubmitAsync(long hremployeeId, long exitReasonRuleId, DateOnly exitDate, long requestedByUserId, string? requesterEmpId, string? note, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        var policy = await context.Pay_ProvidentFundPolicies
            .Where(p => p.CompanyId == emp.companyid && p.IsEnabled)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("บริษัทนี้ยังไม่เปิดใช้งานกองทุนสำรองเลี้ยงชีพ");

        var reasonRule = await context.Pay_ProvidentFundExitReasonRules.FirstOrDefaultAsync(r => r.Id == exitReasonRuleId && r.PolicyId == policy.Id, ct)
            ?? throw new InvalidOperationException("ไม่พบเหตุผลการสิ้นสุดสมาชิกภาพที่เลือก");

        var exitCase = new Pay_ProvidentFundExitCase
        {
            HremployeeId = hremployeeId,
            PolicyId = policy.Id,
            ExitReasonRuleId = exitReasonRuleId,
            ExitDate = exitDate,
            Status = ProvidentFundRequestStatus.PendingApproval,
            Note = note,
            RequestedByUserId = requestedByUserId,
            RequestedDate = DateTime.Now,
        };
        context.Pay_ProvidentFundExitCases.Add(exitCase);
        await context.SaveChangesAsync(ct);

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == WorkflowCode && w.isactive == true, ct);
        if (workflow is not null)
        {
            var jobId = await _engine.StartJobAsync(workflow.workflowid, "Pay_ProvidentFundExitCase", exitCase.Id.ToString(),
                requestedByUserId, requesterEmpId, $"ปิดสมาชิกภาพกองทุนสำรองเลี้ยงชีพ ({reasonRule.Name}): {emp.EmpName} {emp.EmpSurname}", null, ct);
            exitCase.JobMasterId = jobId;
            await context.SaveChangesAsync(ct);
        }

        return exitCase.Id;
    }

    public async Task SyncStatusFromJobAsync(long exitCaseId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var exitCase = await context.Pay_ProvidentFundExitCases.FirstOrDefaultAsync(c => c.Id == exitCaseId, ct);
        if (exitCase is null || exitCase.Status != ProvidentFundRequestStatus.PendingApproval || exitCase.JobMasterId is null) return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == exitCase.JobMasterId, ct);
        if (job is null || job.isJobClosed != true) return;

        if (job.status != WorkflowEngineService.StatusCompleted)
        {
            exitCase.Status = ProvidentFundRequestStatus.Rejected;
            await context.SaveChangesAsync(ct);
            return;
        }

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == exitCase.HremployeeId, ct);
        var reasonRule = await context.Pay_ProvidentFundExitReasonRules.FirstOrDefaultAsync(r => r.Id == exitCase.ExitReasonRuleId, ct);
        var policy = await context.Pay_ProvidentFundPolicies.FirstOrDefaultAsync(p => p.Id == exitCase.PolicyId, ct);
        if (reasonRule is null || policy is null)
        {
            exitCase.Status = ProvidentFundRequestStatus.Rejected;
            await context.SaveChangesAsync(ct);
            return;
        }

        // Which tenure clock to use — fund-membership years (resets on
        // rejoin, per Pay_ProvidentFundMembershipPeriod) or plain employment
        // years — is a per-policy toggle so companies that don't need the
        // distinction see identical behavior to before this table existed.
        DateOnly tenureStartDate;
        if (policy.UseFundMembershipYearsForVesting)
        {
            var membership = await context.Pay_ProvidentFundMembershipPeriods
                .Where(m => m.HremployeeId == exitCase.HremployeeId && m.LeaveDate == null)
                .OrderByDescending(m => m.JoinDate)
                .FirstOrDefaultAsync(ct);
            tenureStartDate = membership?.JoinDate
                ?? (emp?.WorkDate is DateTime wd ? DateOnly.FromDateTime(wd) : exitCase.ExitDate);
        }
        else
        {
            tenureStartDate = emp?.WorkDate is DateTime wd2 ? DateOnly.FromDateTime(wd2) : exitCase.ExitDate;
        }

        var tenureYears = exitCase.ExitDate >= tenureStartDate
            ? Math.Round((exitCase.ExitDate.DayNumber - tenureStartDate.DayNumber) / 365m, 2)
            : 0m;

        var age = emp?.BirthDate is DateTime bd ? exitCase.ExitDate.Year - bd.Year - (exitCase.ExitDate < new DateOnly(exitCase.ExitDate.Year, bd.Month, bd.Day) ? 1 : 0) : (int?)null;

        var exceptionMet = reasonRule.RequiresAgeAndMembershipCheck
            && age is int a && a >= (reasonRule.MinAgeForException ?? int.MaxValue)
            && tenureYears >= (reasonRule.MinMembershipYearsForException ?? int.MaxValue);

        decimal vestingPercent;
        string matchedRuleDesc;

        if (reasonRule.OverrideType == ProvidentFundExitVestingOverride.ForceFull || (reasonRule.OverrideType == ProvidentFundExitVestingOverride.ForceZero && exceptionMet))
        {
            vestingPercent = 100m;
            matchedRuleDesc = exceptionMet
                ? $"{reasonRule.Name} — เข้าเงื่อนไขข้อยกเว้น (อายุ≥{reasonRule.MinAgeForException} ปี และเป็นสมาชิก≥{reasonRule.MinMembershipYearsForException} ปี) จ่ายเต็มจำนวน"
                : $"{reasonRule.Name} — จ่ายเต็มจำนวนตามกติกา";
        }
        else if (reasonRule.OverrideType == ProvidentFundExitVestingOverride.ForceZero)
        {
            vestingPercent = 0m;
            matchedRuleDesc = $"{reasonRule.Name} — ริบเงินสมทบทั้งหมดตามกติกา";
        }
        else
        {
            var tiers = await context.Pay_ProvidentFundVestingTiers.Where(t => t.PolicyId == policy.Id).OrderBy(t => t.SortOrder).ToListAsync(ct);
            var vesting = ProvidentFundVestingCalculator.ResolveVesting(tenureStartDate, exitCase.ExitDate, tiers);
            vestingPercent = vesting.VestingPercent;
            matchedRuleDesc = $"{reasonRule.Name} — ใช้ตาราง vesting ปกติ ({(vesting.MatchedTierNote ?? "ไม่มีช่วงที่ตรง = 100%")})";
        }

        exitCase.ComputedVestingPercent = vestingPercent;
        exitCase.Status = ProvidentFundRequestStatus.Approved;

        context.Pay_ProvidentFundCalculationDetails.Add(new Pay_ProvidentFundCalculationDetail
        {
            CalculationType = ProvidentFundCalculationType.VestingExit,
            ExitCaseId = exitCase.Id,
            InputsSummary = $"อายุงาน/สมาชิกภาพ {tenureYears:0.00} ปี ณ วันที่ {exitCase.ExitDate:dd/MM/yyyy}, เหตุผล: {reasonRule.Name}",
            MatchedRuleDescription = matchedRuleDesc,
            ResultValue = vestingPercent,
            CalculatedDate = DateTime.Now,
        });

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Pay_ProvidentFundExitCase>> GetCasesForEmployeeAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var pendingIds = await context.Pay_ProvidentFundExitCases
            .Where(c => c.HremployeeId == hremployeeId && c.Status == ProvidentFundRequestStatus.PendingApproval)
            .Select(c => c.Id)
            .ToListAsync(ct);
        foreach (var id in pendingIds)
            await SyncStatusFromJobAsync(id, ct);

        await using var freshContext = await _dbFactory.CreateDbContextAsync(ct);
        return await freshContext.Pay_ProvidentFundExitCases
            .Include(c => c.ExitReasonRule)
            .Where(c => c.HremployeeId == hremployeeId)
            .OrderByDescending(c => c.RequestedDate)
            .ToListAsync(ct);
    }

    public async Task<List<Pay_ProvidentFundCalculationDetail>> GetCalculationDetailsAsync(long exitCaseId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Pay_ProvidentFundCalculationDetails
            .Where(d => d.ExitCaseId == exitCaseId)
            .OrderByDescending(d => d.CalculatedDate)
            .ToListAsync(ct);
    }

    public async Task SaveActualAmountsAsync(long exitCaseId, decimal? employeeAmount, decimal? companyAmountToEmployee, decimal? companyAmountReturned, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var exitCase = await context.Pay_ProvidentFundExitCases.FirstOrDefaultAsync(c => c.Id == exitCaseId, ct);
        if (exitCase is null) return;
        exitCase.EmployeeContributionAmount = employeeAmount;
        exitCase.CompanyAmountToEmployee = companyAmountToEmployee;
        exitCase.CompanyAmountReturnedToEmployer = companyAmountReturned;
        await context.SaveChangesAsync(ct);
    }
}
