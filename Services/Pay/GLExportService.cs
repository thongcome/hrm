namespace HRM.Services.Pay;

using System.Text;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Generic self-balancing CSV journal for a Posted+ run: every earning line
// debits its Pay_PayItemType.GLAccountCode, every deduction credits its
// GLAccountCode, and one closing credit line for the total net pay balances
// the journal against a "net pay payable" account — standard double-entry
// shape without assuming any particular chart of accounts.
public class GLExportService
{
    private const string NetPayableAccountCode = "2100-NETPAY-PAYABLE";

    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly PrivateFileStorage _fileStorage;

    public GLExportService(IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage fileStorage)
    {
        _dbFactory = dbFactory;
        _fileStorage = fileStorage;
    }

    public async Task<long> ExportAsync(long runId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Pay_PayrollRun {runId} not found.");

        if (run.Status < PayrollRunStatus.Posted || run.Status == PayrollRunStatus.Cancelled)
            throw new InvalidOperationException("สร้างไฟล์บัญชี (GL) ได้เฉพาะรอบที่บันทึกบัญชีแล้ว (Posted ขึ้นไป) เท่านั้น");

        var lineItems = await context.Pay_PayrollLineItems
            .Include(li => li.Pay_PayItemType)
            .Include(li => li.Pay_PayrollEmployee)
            .Where(li => li.Pay_PayrollEmployee.PayrollRunId == runId && !li.Pay_PayrollEmployee.IsExcluded)
            .ToListAsync(ct);

        var grouped = lineItems
            .GroupBy(li => li.Pay_PayItemType.GLAccountCode ?? $"UNMAPPED-{li.Pay_PayItemType.Code}")
            .Select(g => new
            {
                GLAccountCode = g.Key,
                Debit = g.Where(li => li.SignFlag > 0).Sum(li => li.Amount),
                Credit = g.Where(li => li.SignFlag < 0).Sum(li => li.Amount),
            })
            .Where(g => g.Debit != 0 || g.Credit != 0)
            .ToList();

        var totalNetPay = await context.Pay_PayrollEmployees
            .Where(e => e.PayrollRunId == runId && !e.IsExcluded)
            .SumAsync(e => e.NetPay, ct);

        var csv = new StringBuilder();
        csv.AppendLine("GLAccountCode,Debit,Credit,Description");
        decimal totalDebit = 0, totalCredit = 0;
        foreach (var g in grouped)
        {
            csv.AppendLine($"{g.GLAccountCode},{g.Debit:0.00},{g.Credit:0.00},\"งวด {run.PayrollPeriod}\"");
            totalDebit += g.Debit;
            totalCredit += g.Credit;
        }
        csv.AppendLine($"{NetPayableAccountCode},0.00,{totalNetPay:0.00},\"เงินเดือนค้างจ่าย งวด {run.PayrollPeriod}\"");
        totalCredit += totalNetPay;

        var fileBytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"gl_{runId}_{DateTime.Now:yyyyMMddHHmmss}.csv";
        var (relativePath, _) = await _fileStorage.SaveAsync("gl-exports", fileName, fileBytes, ct);

        var batch = new Pay_GLExportBatch
        {
            PayrollRunId = runId,
            ExportFormatCode = "GENERIC_CSV",
            FilePath = relativePath,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            GeneratedByUserId = actorUserId,
        };
        context.Pay_GLExportBatches.Add(batch);
        await context.SaveChangesAsync(ct);

        foreach (var g in grouped)
        {
            context.Pay_GLExportEntries.Add(new Pay_GLExportEntry
            {
                GLExportBatchId = batch.Id,
                GLAccountCode = g.GLAccountCode,
                DebitAmount = g.Debit,
                CreditAmount = g.Credit,
                Description = $"งวด {run.PayrollPeriod}",
            });
        }
        context.Pay_GLExportEntries.Add(new Pay_GLExportEntry
        {
            GLExportBatchId = batch.Id,
            GLAccountCode = NetPayableAccountCode,
            DebitAmount = 0,
            CreditAmount = totalNetPay,
            Description = $"เงินเดือนค้างจ่าย งวด {run.PayrollPeriod}",
        });
        await context.SaveChangesAsync(ct);

        return batch.Id;
    }
}
