using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Exp;

// Expense/travel claim pipeline: employee builds a draft (header + line
// items, each optionally with a receipt), submits it into the generic
// Workflow Approval Engine (mirrors Lve_LeaveRequest's JobMasterId
// convention exactly — see that model's header comment), then once
// approved HR pushes it into payroll as a non-taxable Pay_AdhocPayItem
// (reimbursements aren't taxable income — same reasoning SeveranceService
// documents for its own IsTaxable=false default).
public class ExpenseClaimService
{
    private const string ReimbursementCode = "REIMBURSEMENT";

    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public ExpenseClaimService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<long> CreateDraftAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");

        var header = new Exp_ClaimHeader
        {
            HremployeeId = emp.id,
            EmpNo = emp.EmpNo,
            CompanyId = emp.companyid,
        };
        context.Exp_ClaimHeaders.Add(header);
        await context.SaveChangesAsync(ct);
        return header.Id;
    }

    public async Task AddLineItemAsync(long claimHeaderId, DateOnly expenseDate, long categoryId, string? description, decimal amount, CancellationToken ct = default)
    {
        if (amount <= 0) throw new InvalidOperationException("จำนวนเงินต้องมากกว่า 0");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var header = await context.Exp_ClaimHeaders.FirstOrDefaultAsync(h => h.Id == claimHeaderId, ct)
            ?? throw new InvalidOperationException("ไม่พบใบเบิกนี้");
        if (header.JobMasterId is not null)
            throw new InvalidOperationException("ใบเบิกนี้ถูกส่งเข้าสู่กระบวนการอนุมัติไปแล้ว แก้ไขไม่ได้");

        context.Exp_ClaimLineItems.Add(new Exp_ClaimLineItem
        {
            ClaimHeaderId = claimHeaderId,
            ExpenseDate = expenseDate,
            CategoryId = categoryId,
            Description = description,
            Amount = amount,
        });
        await context.SaveChangesAsync(ct);
        await RecomputeTotalAsync(context, claimHeaderId, ct);
    }

    public async Task RemoveLineItemAsync(long lineItemId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var line = await context.Exp_ClaimLineItems.FirstOrDefaultAsync(l => l.Id == lineItemId, ct);
        if (line is null) return;

        var header = await context.Exp_ClaimHeaders.FirstOrDefaultAsync(h => h.Id == line.ClaimHeaderId, ct);
        if (header?.JobMasterId is not null)
            throw new InvalidOperationException("ใบเบิกนี้ถูกส่งเข้าสู่กระบวนการอนุมัติไปแล้ว แก้ไขไม่ได้");

        context.Exp_ClaimLineItems.Remove(line);
        await context.SaveChangesAsync(ct);
        await RecomputeTotalAsync(context, line.ClaimHeaderId, ct);
    }

    public async Task AttachReceiptAsync(long lineItemId, string fileName, string relativePath, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var line = await context.Exp_ClaimLineItems.FirstOrDefaultAsync(l => l.Id == lineItemId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการนี้");

        var doc = new doc_center
        {
            refid = lineItemId,
            doctypecode = "EXP_RECEIPT",
            files = fileName,
            path = relativePath,
            isActive = true,
            moddate = DateTime.Now,
        };
        context.doc_centers.Add(doc);
        await context.SaveChangesAsync(ct);

        line.ReceiptDocCenterId = doc.id;
        await context.SaveChangesAsync(ct);
    }

    private static async Task RecomputeTotalAsync(HRMContext context, long claimHeaderId, CancellationToken ct)
    {
        var total = await context.Exp_ClaimLineItems
            .Where(l => l.ClaimHeaderId == claimHeaderId)
            .SumAsync(l => (decimal?)l.Amount, ct) ?? 0m;

        var header = await context.Exp_ClaimHeaders.FirstOrDefaultAsync(h => h.Id == claimHeaderId, ct);
        if (header is null) return;
        header.TotalAmount = total;
        await context.SaveChangesAsync(ct);
    }

    public async Task<long> SubmitAsync(long claimHeaderId, long actorUserId, Workflow.WorkflowEngineService engine, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var header = await context.Exp_ClaimHeaders.FirstOrDefaultAsync(h => h.Id == claimHeaderId, ct)
            ?? throw new InvalidOperationException("ไม่พบใบเบิกนี้");
        if (header.JobMasterId is not null)
            throw new InvalidOperationException("ใบเบิกนี้ถูกส่งเข้าสู่กระบวนการอนุมัติไปแล้ว");

        var lineCount = await context.Exp_ClaimLineItems.CountAsync(l => l.ClaimHeaderId == claimHeaderId, ct);
        if (lineCount == 0)
            throw new InvalidOperationException("กรุณาเพิ่มรายการค่าใช้จ่ายอย่างน้อย 1 รายการก่อนส่งอนุมัติ");
        if (header.TotalAmount <= 0)
            throw new InvalidOperationException("ยอดรวมต้องมากกว่า 0");

        var workflow = await context.wf_workflows.FirstOrDefaultAsync(w => w.workflowcode == "EXPENSE_CLAIM_APPROVAL", ct)
            ?? throw new InvalidOperationException("ไม่พบ workflow EXPENSE_CLAIM_APPROVAL — ตรวจสอบว่า migration ถูก apply แล้ว");

        var subject = $"ใบเบิกค่าใช้จ่าย: {header.EmpNo} จำนวน {header.TotalAmount:N2} บาท";
        var jobId = await engine.StartJobAsync(workflow.workflowid, "Exp_ClaimHeader", claimHeaderId.ToString(),
            actorUserId, header.EmpNo, subject, header.TotalAmount, ct);

        header.JobMasterId = jobId;
        await context.SaveChangesAsync(ct);
        return jobId;
    }

    public async Task<List<Exp_ClaimHeader>> GetClaimsForEmployeeAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Exp_ClaimHeaders
            .Where(h => h.HremployeeId == hremployeeId)
            .OrderByDescending(h => h.Id)
            .ToListAsync(ct);
    }

    public async Task<List<Exp_ClaimHeader>> GetAllClaimsAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Exp_ClaimHeaders
            .Where(h => h.CompanyId == companyId)
            .OrderByDescending(h => h.Id)
            .ToListAsync(ct);
    }

    // Mirrors LeaveRequestList.razor's jobStatuses dictionary pattern exactly
    // ("-" = still draft, no job started yet).
    public async Task<Dictionary<long, string>> GetJobStatusesAsync(IEnumerable<long> claimHeaderIds, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var jobIds = await context.Exp_ClaimHeaders
            .Where(h => claimHeaderIds.Contains(h.Id) && h.JobMasterId != null)
            .Select(h => new { h.Id, h.JobMasterId })
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

    public async Task PushToPayrollAsync(long claimHeaderId, string targetPeriod, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var header = await context.Exp_ClaimHeaders.FirstOrDefaultAsync(h => h.Id == claimHeaderId, ct)
            ?? throw new InvalidOperationException("ไม่พบใบเบิกนี้");
        if (header.AdhocPayItemId is not null)
            throw new InvalidOperationException("ใบเบิกนี้ถูกผลักเข้า payroll ไปแล้ว");
        if (header.JobMasterId is null)
            throw new InvalidOperationException("ใบเบิกนี้ยังไม่ได้ส่งอนุมัติ");

        var job = await context.job_masters.FirstOrDefaultAsync(j => j.jobmasterid == header.JobMasterId, ct)
            ?? throw new InvalidOperationException("ไม่พบข้อมูล workflow ของใบเบิกนี้");
        if (job.isJobClosed != true || job.status != Workflow.WorkflowEngineService.StatusCompleted)
            throw new InvalidOperationException("ใบเบิกนี้ยังไม่ได้รับการอนุมัติครบถ้วน");

        var reimbursementTypeId = await context.Pay_PayItemTypes
            .Where(t => t.Code == ReimbursementCode)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(ct);
        if (reimbursementTypeId == 0)
            throw new InvalidOperationException("ไม่พบประเภทรายการ REIMBURSEMENT ในระบบ — ต้องรัน migration ก่อน");

        var item = new Pay_AdhocPayItem
        {
            HremployeeId = header.HremployeeId,
            PayItemTypeId = reimbursementTypeId,
            TargetPeriod = targetPeriod,
            Amount = header.TotalAmount,
            IsTaxable = false,
            Reason = $"เบิกค่าใช้จ่าย (ใบเบิก #{header.Id})" + (string.IsNullOrWhiteSpace(header.Reason) ? "" : $" — {header.Reason}"),
            Status = PayAdhocItemStatus.Pending,
            RequestedByUserId = actorUserId,
        };
        context.Pay_AdhocPayItems.Add(item);
        await context.SaveChangesAsync(ct);

        header.AdhocPayItemId = item.Id;
        await context.SaveChangesAsync(ct);
    }
}
