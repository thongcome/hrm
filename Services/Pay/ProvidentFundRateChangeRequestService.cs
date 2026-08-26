namespace HRM.Services.Pay;

using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

// Rate-change requests (submitted by the employee via ESS, only inside an
// open Pay_ProvidentFundRateChangeWindow, or by HR directly at any time) go
// through the Workflow Approval Engine before they ever become a real
// Pay_ProvidentFundElection row — mirrors the lazy apply-on-read pattern
// used everywhere else in this codebase (PerfApprovalService,
// OrgChangeRequestService, ...): SyncStatusFromJobAsync is called on every
// read and only actually applies the election once the workflow job has
// genuinely closed.
public class ProvidentFundRateChangeRequestService
{
    public const string WorkflowCode = "PVD_RATE_CHANGE_APPROVAL";

    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly WorkflowEngineService _engine;
    private readonly ProvidentFundRateMatrixService _matrixService;

    public ProvidentFundRateChangeRequestService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine, ProvidentFundRateMatrixService matrixService)
    {
        _dbFactory = dbFactory;
        _engine = engine;
        _matrixService = matrixService;
    }

    public async Task<Pay_ProvidentFundPolicy?> GetActivePolicyAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await context.Pay_ProvidentFundPolicies
            .Where(p => p.CompanyId == companyId && p.IsEnabled && p.EffectiveFrom <= today && (p.EffectiveTo == null || p.EffectiveTo >= today))
            .FirstOrDefaultAsync(ct);
    }

    // Returns the window currently open today, if any — used both to decide
    // whether ESS should even show the "request rate change" button, and to
    // resolve the effective date a request will take when submitted inside it.
    public async Task<Pay_ProvidentFundRateChangeWindow?> GetOpenWindowAsync(long policyId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var windows = await context.Pay_ProvidentFundRateChangeWindows
            .Where(w => w.PolicyId == policyId && w.IsActive)
            .ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        return windows.FirstOrDefault(w => IsWithinWindow(w, today));
    }

    private static bool IsWithinWindow(Pay_ProvidentFundRateChangeWindow w, DateOnly today)
    {
        // Recurring annual month/day range, assumed not to cross the year
        // boundary (matches every real-world example seen so far — a window
        // entirely within one calendar year, e.g. 1 May - 31 May).
        var from = new DateOnly(today.Year, w.OpenFromMonth, w.OpenFromDay);
        var to = new DateOnly(today.Year, w.OpenToMonth, w.OpenToDay);
        return today >= from && today <= to;
    }

    private static DateOnly ResolveEffectiveDate(Pay_ProvidentFundRateChangeWindow? window, DateOnly today)
    {
        if (window is null) return today;
        var effective = new DateOnly(today.Year, window.EffectiveMonth, window.EffectiveDay);
        // The window's effective date can fall before "today" within the same
        // calendar year (e.g. request window in Nov, effective date in Jan) —
        // roll to next year in that case.
        return effective < today ? effective.AddYears(1) : effective;
    }

    // isEmployeeInitiated gates the open-window requirement — HR submitting
    // directly (e.g. a correction, or a case the window rule doesn't cover)
    // is not restricted to the window, matching the "HR เลือกได้" half of
    // the design (only the employee's own self-service path is gated).
    public async Task<long> SubmitAsync(long hremployeeId, decimal requestedEmployeeRate, long requestedByUserId, string? requesterEmpId, bool isEmployeeInitiated, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var policy = await context.Pay_ProvidentFundPolicies
            .Where(p => p.CompanyId == emp.companyid && p.IsEnabled && p.EffectiveFrom <= today && (p.EffectiveTo == null || p.EffectiveTo >= today))
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("บริษัทนี้ยังไม่เปิดใช้งานกองทุนสำรองเลี้ยงชีพ");

        if (requestedEmployeeRate < policy.MinEmployeeRate || requestedEmployeeRate > policy.MaxEmployeeRate)
            throw new InvalidOperationException($"อัตราเงินสะสมต้องอยู่ระหว่าง {policy.MinEmployeeRate:0.0}-{policy.MaxEmployeeRate:0.0}%");

        var windows = await context.Pay_ProvidentFundRateChangeWindows.Where(w => w.PolicyId == policy.Id && w.IsActive).ToListAsync(ct);
        var openWindow = windows.FirstOrDefault(w => IsWithinWindow(w, today));

        if (isEmployeeInitiated && windows.Count > 0 && openWindow is null)
            throw new InvalidOperationException("ขณะนี้ไม่อยู่ในช่วงเวลาที่เปิดให้ยื่นคำขอเปลี่ยนอัตรา");

        var yearsOfService = emp.WorkDate is DateTime wd
            ? Math.Round((today.DayNumber - DateOnly.FromDateTime(wd).DayNumber) / 365m, 2)
            : 0m;
        var suggestion = await _matrixService.SuggestCompanyRateAsync(policy.Id, yearsOfService, requestedEmployeeRate, ct);

        var request = new Pay_ProvidentFundRateChangeRequest
        {
            HremployeeId = hremployeeId,
            PolicyId = policy.Id,
            RequestedEmployeeRate = requestedEmployeeRate,
            SuggestedCompanyRate = suggestion.SuggestedRate,
            RequestedCompanyRate = suggestion.SuggestedRate ?? policy.MinCompanyRate,
            WindowId = openWindow?.Id,
            RequestedEffectiveFrom = ResolveEffectiveDate(openWindow, today),
            Status = ProvidentFundRequestStatus.PendingApproval,
            RequestedByUserId = requestedByUserId,
            RequestedDate = DateTime.Now,
            IsEmployeeInitiated = isEmployeeInitiated,
        };
        context.Pay_ProvidentFundRateChangeRequests.Add(request);
        await context.SaveChangesAsync(ct);

        if (suggestion.SuggestedRate is not null)
        {
            context.Pay_ProvidentFundCalculationDetails.Add(new Pay_ProvidentFundCalculationDetail
            {
                CalculationType = ProvidentFundCalculationType.RateMatrix,
                RateChangeRequestId = request.Id,
                InputsSummary = $"อายุงาน {yearsOfService:0.00} ปี, ขอสะสม {requestedEmployeeRate:0.00}%",
                MatchedRuleDescription = suggestion.MatchedRuleDescription,
                ResultValue = suggestion.SuggestedRate.Value,
                CalculatedDate = DateTime.Now,
            });
            await context.SaveChangesAsync(ct);
        }

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == WorkflowCode && w.isactive == true, ct);
        if (workflow is not null)
        {
            var jobId = await _engine.StartJobAsync(workflow.workflowid, "Pay_ProvidentFundRateChangeRequest", request.Id.ToString(),
                requestedByUserId, requesterEmpId, $"ขอเปลี่ยนอัตราเงินสะสมกองทุนสำรองเลี้ยงชีพ: {emp.EmpName} {emp.EmpSurname}", null, ct);
            request.JobMasterId = jobId;
            await context.SaveChangesAsync(ct);
        }

        return request.Id;
    }

    public async Task SyncStatusFromJobAsync(long requestId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var request = await context.Pay_ProvidentFundRateChangeRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null || request.Status != ProvidentFundRequestStatus.PendingApproval || request.JobMasterId is null) return;

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == request.JobMasterId, ct);
        if (job is null || job.isJobClosed != true) return;

        if (job.status == WorkflowEngineService.StatusCompleted)
        {
            request.Status = ProvidentFundRequestStatus.Approved;

            var existingActive = await context.Pay_ProvidentFundElections
                .Where(e => e.HremployeeId == request.HremployeeId && e.IsActive)
                .ToListAsync(ct);
            foreach (var old in existingActive)
            {
                old.IsActive = false;
                old.EffectiveTo = request.RequestedEffectiveFrom.AddDays(-1);
            }

            context.Pay_ProvidentFundElections.Add(new Pay_ProvidentFundElection
            {
                HremployeeId = request.HremployeeId,
                EmployeeContributionRate = request.RequestedEmployeeRate,
                CompanyContributionRate = request.RequestedCompanyRate,
                InvestmentPolicyId = existingActive.OrderByDescending(e => e.EffectiveFrom).FirstOrDefault()?.InvestmentPolicyId,
                EffectiveFrom = request.RequestedEffectiveFrom,
                IsActive = true,
                ElectedByUserId = request.RequestedByUserId,
                ElectedDate = DateTime.Now,
            });
        }
        else
        {
            request.Status = ProvidentFundRequestStatus.Rejected;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Pay_ProvidentFundRateChangeRequest>> GetRequestsForEmployeeAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var pendingIds = await context.Pay_ProvidentFundRateChangeRequests
            .Where(r => r.HremployeeId == hremployeeId && r.Status == ProvidentFundRequestStatus.PendingApproval)
            .Select(r => r.Id)
            .ToListAsync(ct);
        foreach (var id in pendingIds)
            await SyncStatusFromJobAsync(id, ct);

        await using var freshContext = await _dbFactory.CreateDbContextAsync(ct);
        return await freshContext.Pay_ProvidentFundRateChangeRequests
            .Where(r => r.HremployeeId == hremployeeId)
            .OrderByDescending(r => r.RequestedDate)
            .ToListAsync(ct);
    }

    public async Task<List<Pay_ProvidentFundCalculationDetail>> GetCalculationDetailsAsync(long requestId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Pay_ProvidentFundCalculationDetails
            .Where(d => d.RateChangeRequestId == requestId)
            .OrderByDescending(d => d.CalculatedDate)
            .ToListAsync(ct);
    }
}
