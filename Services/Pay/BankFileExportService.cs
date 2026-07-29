namespace HRM.Services.Pay;

using System.Text;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Generic CSV bank transfer file for an Approved+ run — Pay_BankFileExportBatch.BankFormatCode
// defaults to "GENERIC_CSV"; a real bank's fixed-width/CSV spec can be added
// later as a new format code without changing the schema.
public class BankFileExportService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;
    private readonly PrivateFileStorage _fileStorage;

    public BankFileExportService(IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage fileStorage)
    {
        _dbFactory = dbFactory;
        _fileStorage = fileStorage;
    }

    public async Task<long> ExportAsync(long runId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Pay_PayrollRun {runId} not found.");

        if (run.Status < PayrollRunStatus.Approved || run.Status == PayrollRunStatus.Cancelled)
            throw new InvalidOperationException("สร้างไฟล์ธนาคารได้เฉพาะรอบที่อนุมัติแล้ว (Approved ขึ้นไป) เท่านั้น");

        var employees = await context.Pay_PayrollEmployees
            .Include(e => e.Hremployee)
            .Where(e => e.PayrollRunId == runId && !e.IsExcluded)
            .OrderBy(e => e.EmpNo)
            .ToListAsync(ct);

        var csv = new StringBuilder();
        csv.AppendLine("EmpNo,Name,BankCode,BankBranchCode,BankAccountNo,Amount");
        foreach (var emp in employees)
        {
            var name = $"{emp.Hremployee.EmpName} {emp.Hremployee.EmpSurname}".Replace("\"", "\"\"");
            csv.AppendLine($"{emp.EmpNo},\"{name}\",{emp.BankCode},{emp.BankBranchCode},{emp.BankAccountNo},{emp.NetPay:0.00}");
        }

        var fileBytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"bankfile_{runId}_{DateTime.Now:yyyyMMddHHmmss}.csv";
        var (relativePath, _) = await _fileStorage.SaveAsync("bank-exports", fileName, fileBytes, ct);

        var batch = new Pay_BankFileExportBatch
        {
            PayrollRunId = runId,
            BankFormatCode = "GENERIC_CSV",
            FilePath = relativePath,
            TotalAmount = employees.Sum(e => e.NetPay),
            TotalRecordCount = employees.Count,
            Status = BankFileExportStatus.Generated,
            GeneratedByUserId = actorUserId,
        };
        context.Pay_BankFileExportBatches.Add(batch);
        await context.SaveChangesAsync(ct);

        foreach (var emp in employees)
        {
            context.Pay_BankFileExportLines.Add(new Pay_BankFileExportLine
            {
                BankFileExportBatchId = batch.Id,
                PayrollEmployeeId = emp.Id,
                BankCode = emp.BankCode,
                BankBranchCode = emp.BankBranchCode,
                BankAccountNo = emp.BankAccountNo,
                Amount = emp.NetPay,
            });
        }
        await context.SaveChangesAsync(ct);

        return batch.Id;
    }
}
