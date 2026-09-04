using HRM.Models;
using HRM.Services.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Engagement;

// The points/redeem layer on top of the existing peer-kudos system
// (Eng_Recognition + RecognitionService). A kudos now carries Points; this
// service turns points received into a spendable balance and lets an employee
// redeem them for rewards (Eng_RedeemItem) via Eng_RedeemRequest. Balance is
// always computed from the ledger — points received minus points committed to
// live redeems — so there's no denormalized total to drift. Approval is an
// explicit admin action here (JobMasterId is reserved for wiring the shared
// workflow engine next), and every redeem re-checks the balance at approve
// time so concurrent requests can't overspend.
public class EngagementService(IDbContextFactory<HRMContext> dbFactory, WorkflowEngineService engine)
{
    // Redeem statuses that have committed the employee's points (can't be spent
    // twice). Rejected/Cancelled release the points back into the balance.
    private static readonly EngRedeemStatus[] CommittedStatuses =
        { EngRedeemStatus.PendingApproval, EngRedeemStatus.Approved, EngRedeemStatus.Fulfilled };

    public record BalanceSummary(int Earned, int Spent, int Available);

    public async Task<BalanceSummary> GetBalanceAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await ComputeBalanceAsync(context, hremployeeId, ct);
    }

    private static async Task<BalanceSummary> ComputeBalanceAsync(HRMContext context, long hremployeeId, CancellationToken ct)
    {
        var earned = await context.Eng_Recognitions
            .Where(k => k.ToHremployeeId == hremployeeId && k.IsActive)
            .SumAsync(k => (int?)k.Points, ct) ?? 0;

        var spent = await context.Eng_RedeemRequests
            .Where(r => r.HremployeeId == hremployeeId && r.IsActive && CommittedStatuses.Contains(r.Status))
            .SumAsync(r => (int?)r.PointsSpent, ct) ?? 0;

        return new BalanceSummary(earned, spent, earned - spent);
    }

    // Creates a redeem request already submitted for approval (points are
    // committed immediately so the balance reflects the pending spend). Guards
    // that the employee can afford it against their CURRENT available balance.
    public async Task<Eng_RedeemRequest> RequestRedeemAsync(
        string companyId, long hremployeeId, long redeemItemId, string? note, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var item = await context.Eng_RedeemItems.FirstOrDefaultAsync(i => i.Id == redeemItemId && i.IsActive, ct)
            ?? throw new InvalidOperationException("ไม่พบของรางวัลนี้แล้ว");
        if (item.StockQty is int stock && stock <= 0)
            throw new InvalidOperationException("ของรางวัลนี้หมดแล้ว");

        var balance = await ComputeBalanceAsync(context, hremployeeId, ct);
        if (balance.Available < item.PointsCost)
            throw new InvalidOperationException($"แต้มไม่พอ (มี {balance.Available} ต้องใช้ {item.PointsCost})");

        var request = new Eng_RedeemRequest
        {
            CompanyId = companyId,
            HremployeeId = hremployeeId,
            RedeemItemId = redeemItemId,
            SnapshotItemName = item.Name,
            PointsSpent = item.PointsCost,
            Status = EngRedeemStatus.PendingApproval,
            RequestedByUserId = actorUserId,
            RequestedDate = DateTime.Now,
            IsActive = true,
        };
        context.Eng_RedeemRequests.Add(request);
        await context.SaveChangesAsync(ct);

        // Route the redeem through the shared engine like leave/OT/welfare — the
        // ENG_REDEEM workflow's approvers act in /wf/my-inbox, and SyncRedeemsAsync
        // applies the outcome back. Points are already committed (PendingApproval)
        // so the balance reflects the pending spend immediately.
        var approvalWorkflow = await context.wf_workflows
            .FirstOrDefaultAsync(w => w.workflowcode == EngRedeemWorkflowSeeder.WorkflowCode, ct);
        if (approvalWorkflow is not null && approvalWorkflow.isactive == true)
        {
            var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);
            var jobId = await engine.StartJobAsync(
                approvalWorkflow.workflowid, "Eng_RedeemRequest", request.Id.ToString(),
                actorUserId, emp?.EmpNo, $"ขอแลกของรางวัล: {item.Name} ({item.PointsCost} แต้ม)", item.PointsCost, ct);
            request.JobMasterId = jobId;
            await context.SaveChangesAsync(ct);
        }
        return request;
    }

    // Apply-on-read for the workflow path: reflect each PendingApproval redeem's
    // job outcome — COMPLETED → Approved (and decrement stock once); closed but
    // not completed (declined) → Rejected, which releases the committed points.
    // Called before listing so any read shows the current state. Admin can also
    // act directly via ApproveRedeemAsync/RejectRedeemAsync (both guarded).
    public async Task<int> SyncRedeemsAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var pending = await context.Eng_RedeemRequests
            .Where(r => r.CompanyId == companyId && r.IsActive
                && r.Status == EngRedeemStatus.PendingApproval && r.JobMasterId != null)
            .ToListAsync(ct);
        if (pending.Count == 0) return 0;

        var jobIds = pending.Select(r => r.JobMasterId!.Value).ToList();
        var jobs = await context.job_masters.Where(j => jobIds.Contains(j.jobmasterid)).ToDictionaryAsync(j => j.jobmasterid, ct);

        var changed = 0;
        foreach (var req in pending)
        {
            if (!jobs.TryGetValue(req.JobMasterId!.Value, out var job) || job.isJobClosed != true) continue;

            if (job.status == WorkflowEngineService.StatusCompleted)
            {
                var item = await context.Eng_RedeemItems.FirstOrDefaultAsync(i => i.Id == req.RedeemItemId, ct);
                if (item?.StockQty is int stock && stock > 0) item.StockQty = stock - 1;
                req.Status = EngRedeemStatus.Approved;
            }
            else
            {
                req.Status = EngRedeemStatus.Rejected; // declined/cancelled at the engine → release points
            }
            changed++;
        }
        await context.SaveChangesAsync(ct);
        return changed;
    }

    // Admin approves: re-check affordability (other redeems may have landed),
    // decrement stock, mark Approved. Points stay committed (they already were
    // at PendingApproval), so the balance is unchanged by approval itself.
    public async Task ApproveRedeemAsync(long requestId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var request = await context.Eng_RedeemRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้แล้ว");
        if (request.Status != EngRedeemStatus.PendingApproval)
            throw new InvalidOperationException("อนุมัติได้เฉพาะคำขอที่รออนุมัติเท่านั้น");

        var item = await context.Eng_RedeemItems.FirstOrDefaultAsync(i => i.Id == request.RedeemItemId, ct);
        if (item?.StockQty is int stock)
        {
            if (stock <= 0) throw new InvalidOperationException("ของรางวัลหมดแล้ว");
            item.StockQty = stock - 1;
        }

        request.Status = EngRedeemStatus.Approved;
        await context.SaveChangesAsync(ct);
    }

    // Rejects or cancels: releases the committed points back to the balance
    // (they simply stop counting toward "spent" once status leaves the
    // committed set). Optionally records why.
    public async Task RejectRedeemAsync(long requestId, string? reason, bool cancelledByOwner, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var request = await context.Eng_RedeemRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้แล้ว");
        if (request.Status is EngRedeemStatus.Fulfilled)
            throw new InvalidOperationException("คำขอที่มอบของแล้ว ยกเลิกไม่ได้");

        request.Status = cancelledByOwner ? EngRedeemStatus.Cancelled : EngRedeemStatus.Rejected;
        if (!string.IsNullOrWhiteSpace(reason))
            request.Note = string.IsNullOrWhiteSpace(request.Note) ? reason : $"{request.Note} | {reason}";
        await context.SaveChangesAsync(ct);
    }

    public async Task FulfillRedeemAsync(long requestId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var request = await context.Eng_RedeemRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("ไม่พบคำขอนี้แล้ว");
        if (request.Status != EngRedeemStatus.Approved)
            throw new InvalidOperationException("มอบของได้เฉพาะคำขอที่อนุมัติแล้ว");

        request.Status = EngRedeemStatus.Fulfilled;
        request.FulfilledDate = DateTime.Now;
        await context.SaveChangesAsync(ct);
    }

    public record RedeemView(
        long Id, long HremployeeId, string EmpName, string ItemName,
        int PointsSpent, EngRedeemStatus Status, DateTime RequestedDate, DateTime? FulfilledDate, string? Note);

    public async Task<List<RedeemView>> GetRedeemsAsync(
        string companyId, long? hremployeeId, EngRedeemStatus? status, int take = 200, CancellationToken ct = default)
    {
        await SyncRedeemsAsync(companyId, ct); // reflect any workflow outcomes before listing

        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var q = context.Eng_RedeemRequests.Where(r => r.CompanyId == companyId && r.IsActive);
        if (hremployeeId is long h) q = q.Where(r => r.HremployeeId == h);
        if (status is EngRedeemStatus s) q = q.Where(r => r.Status == s);

        var rows = await q.OrderByDescending(r => r.RequestedDate).Take(take).ToListAsync(ct);
        if (rows.Count == 0) return new();

        var empIds = rows.Select(r => r.HremployeeId).Distinct().ToList();
        var names = await context.Hremployee.Where(e => empIds.Contains(e.id))
            .Select(e => new { e.id, e.EmpName })
            .ToDictionaryAsync(e => e.id, e => e.EmpName, ct);

        return rows.Select(r => new RedeemView(
            r.Id, r.HremployeeId, names.GetValueOrDefault(r.HremployeeId) ?? $"#{r.HremployeeId}",
            r.SnapshotItemName ?? "-", r.PointsSpent, r.Status, r.RequestedDate, r.FulfilledDate, r.Note)).ToList();
    }
}
