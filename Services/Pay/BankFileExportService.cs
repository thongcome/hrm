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
    private readonly bool _requireSeparateDuties;

    public BankFileExportService(IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage fileStorage,
        Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
    {
        _dbFactory = dbFactory;
        _fileStorage = fileStorage;
        _requireSeparateDuties = PayrollSeparationOfDuties.IsRequiredAfterApproval(configuration);
    }

    // One live bank file per run (audit H-18): a second file for the same run is the classic
    // way a payroll gets uploaded to the bank twice. To regenerate, void the old one first.
    public static bool IsActive(Pay_BankFileExportBatch batch) => batch.Status != BankFileExportStatus.Voided;

    public async Task VoidAsync(long batchId, string companyId, long actorUserId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("กรุณาระบุเหตุผลที่ยกเลิกไฟล์ธนาคาร");
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var batch = await context.Pay_BankFileExportBatches.Include(b => b.Pay_PayrollRun)
            .FirstOrDefaultAsync(b => b.Id == batchId && b.Pay_PayrollRun.CompanyId == companyId, ct)
            ?? throw new InvalidOperationException("ไม่พบไฟล์ธนาคารนี้");
        if (batch.Status == BankFileExportStatus.Voided)
            throw new InvalidOperationException("ไฟล์นี้ถูกยกเลิกไปแล้ว");
        if (batch.Status == BankFileExportStatus.ConfirmedSent || batch.Pay_PayrollRun.Status == PayrollRunStatus.Paid)
            throw new InvalidOperationException("ไฟล์นี้ส่งธนาคาร/รอบนี้จ่ายเงินแล้ว — ยกเลิกไฟล์ไม่ได้");

        var from = batch.Status;
        batch.Status = BankFileExportStatus.Voided;
        batch.Remark = $"{(string.IsNullOrWhiteSpace(batch.Remark) ? "" : batch.Remark + " · ")}ยกเลิกไฟล์ {DateTime.Now:dd/MM/yyyy HH:mm}: {reason.Trim()}";
        context.Pay_PayrollAuditLogs.Add(new Pay_PayrollAuditLog
        {
            PayrollRunId = batch.PayrollRunId,
            EventType = PayAuditEventType.ManualAdjustment,
            FromStatus = batch.Pay_PayrollRun.Status,
            ToStatus = batch.Pay_PayrollRun.Status,
            ActorUserId = actorUserId,
            Comment = $"Bank file #{batch.Id} voided (was {from}): {reason.Trim()}",
        });
        await context.SaveChangesAsync(ct);
    }

    // Called by the download endpoint: the first download is the moment the file can reach
    // the bank, so it is recorded (status + audit trail).
    public async Task<Pay_BankFileExportBatch?> MarkDownloadedAsync(long batchId, string companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var batch = await context.Pay_BankFileExportBatches.Include(b => b.Pay_PayrollRun)
            .FirstOrDefaultAsync(b => b.Id == batchId && b.Pay_PayrollRun.CompanyId == companyId, ct);
        if (batch is null || batch.Status == BankFileExportStatus.Voided) return null;
        if (batch.Status == BankFileExportStatus.Generated)
            batch.Status = BankFileExportStatus.Downloaded;
        context.Pay_PayrollAuditLogs.Add(new Pay_PayrollAuditLog
        {
            PayrollRunId = batch.PayrollRunId,
            EventType = PayAuditEventType.ManualAdjustment,
            FromStatus = batch.Pay_PayrollRun.Status,
            ToStatus = batch.Pay_PayrollRun.Status,
            ActorUserId = actorUserId,
            Comment = $"Bank file #{batch.Id} downloaded",
        });
        await context.SaveChangesAsync(ct);
        return batch;
    }

    public async Task<long> ExportAsync(long runId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var run = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Pay_PayrollRun {runId} not found.");

        if (!PayrollRunTypes.IsSupported(run.RunType))
            throw new InvalidOperationException(PayrollRunTypes.UnsupportedMessage);
        if (!run.IsFinal())
            throw new InvalidOperationException("สร้างไฟล์ธนาคารได้เฉพาะรอบที่อนุมัติแล้ว (Approved ขึ้นไป) เท่านั้น");
        PayrollSeparationOfDuties.EnsureNotPreparer(run, actorUserId, "การสร้างไฟล์ธนาคาร", _requireSeparateDuties);
        var existing = await context.Pay_BankFileExportBatches
            .Where(b => b.PayrollRunId == runId && b.Status != BankFileExportStatus.Voided)
            .Select(b => (long?)b.Id).FirstOrDefaultAsync(ct);
        if (existing is long existingId)
            throw new InvalidOperationException(
                $"รอบนี้มีไฟล์ธนาคารแล้ว (#{existingId}) — ถ้าต้องสร้างใหม่ ให้ยกเลิกไฟล์เดิมพร้อมเหตุผลก่อน เพื่อกันการส่งธนาคารซ้ำ");

        var employees = await context.Pay_PayrollEmployees
            .Include(e => e.Hremployee)
            .Where(e => e.PayrollRunId == runId && !e.IsExcluded)
            .OrderBy(e => e.EmpNo)
            .ToListAsync(ct);

        var lines = employees.Select(e => (Emp: e, Amount: e.NetPay)).ToList();

        // ไฟล์ที่ธนาคารรับต้องมีเลขบัญชีทุกแถวและยอดเป็นบวก (audit M9) — เจอแถวเสียให้หยุดและบอกชื่อ
        // ดีกว่าตัดคนออกเงียบ ๆ แล้วมีพนักงานไม่ได้เงิน
        var noAccount = lines.Where(l => string.IsNullOrWhiteSpace(l.Emp.BankAccountNo)).Select(l => l.Emp.EmpNo).ToList();
        if (noAccount.Count > 0)
            throw new InvalidOperationException(
                $"พนักงาน {noAccount.Count} คนไม่มีเลขบัญชีธนาคาร (เช่น {string.Join(", ", noAccount.Take(5))}) — บันทึกเลขบัญชีในทะเบียนพนักงานแล้วคำนวณใหม่ หรือกันคนเหล่านี้ออกจากรอบก่อน");
        var nonPositive = lines.Where(l => l.Amount <= 0m).Select(l => l.Emp.EmpNo).ToList();
        if (nonPositive.Count > 0)
            throw new InvalidOperationException(
                $"พนักงาน {nonPositive.Count} คนมียอดสุทธิเป็นศูนย์หรือติดลบ (เช่น {string.Join(", ", nonPositive.Take(5))}) — กันออกจากรอบก่อนสร้างไฟล์");

        // รูปแบบไฟล์ตาม spec ธนาคารเป็น config ต่อบริษัท (12 ก.ย. 2569): แม่แบบที่ตั้งเป็นค่าเริ่มต้น ไม่มี = CSV กลางแบบเดิม
        var format = await context.Pay_BankFileFormats.AsNoTracking()
            .FirstOrDefaultAsync(f => f.CompanyId == run.CompanyId && f.IsActive && f.IsDefault, ct);
        var companyName = await context.Pay_PayslipSettings.AsNoTracking()
            .Where(s => s.CompanyId == run.CompanyId).Select(s => s.CompanyName).FirstOrDefaultAsync(ct) ?? run.CompanyId;
        var totalAmount = lines.Sum(l => l.Amount);
        var batchNo = $"BF{run.PayrollPeriod}-{runId}";
        var fileValues = BankFileValues.ForFile(companyName, format, run.PayDate, run.PayrollPeriod, batchNo, lines.Count, totalAmount);
        var seqNo = 0;
        var lineValues = lines.Select(l => BankFileValues.ForLine(++seqNo, l.Emp.EmpNo, l.Emp.Hremployee.EmpName, l.Emp.Hremployee.EmpSurname,
            l.Emp.BankCode, l.Emp.BankBranchCode, l.Emp.BankAccountNo, l.Amount, l.Emp.Hremployee.IdCard, l.Emp.Hremployee.AdnEmail)).ToList();
        var text = format is null
            ? BankFileTemplate.Build(BankFileTemplate.GenericCsvHeader, BankFileTemplate.GenericCsvLine, null, "CRLF", fileValues, lineValues)
            : BankFileTemplate.Build(format.HeaderTemplate, format.LineTemplate, format.TrailerTemplate, format.LineEnding, fileValues, lineValues);
        var fileBytes = BankFileTemplate.Encode(text, format?.Encoding ?? "UTF8BOM");
        var fileName = format is not null && !string.IsNullOrWhiteSpace(format.FileNamePattern)
            ? BankFileTemplate.Render(format.FileNamePattern, fileValues).Replace('/', '_').Replace('\\', '_')
            : $"bankfile_{runId}_{DateTime.Now:yyyyMMddHHmmss}.csv";
        var (relativePath, _) = await _fileStorage.SaveAsync("bank-exports", $"{runId}_{DateTime.Now:yyyyMMddHHmmss}_{fileName}", fileBytes, ct);

        var batch = new Pay_BankFileExportBatch
        {
            PayrollRunId = runId,
            BankFormatCode = format?.Code ?? "GENERIC_CSV",
            FilePath = relativePath,
            TotalAmount = totalAmount,
            TotalRecordCount = lines.Count,
            Status = BankFileExportStatus.Generated,
            GeneratedByUserId = actorUserId,
        };
        context.Pay_BankFileExportBatches.Add(batch);
        await context.SaveChangesAsync(ct);

        foreach (var (emp, amount) in lines)
        {
            context.Pay_BankFileExportLines.Add(new Pay_BankFileExportLine
            {
                BankFileExportBatchId = batch.Id,
                PayrollEmployeeId = emp.Id,
                BankCode = emp.BankCode,
                BankBranchCode = emp.BankBranchCode,
                BankAccountNo = emp.BankAccountNo,
                Amount = amount,
            });
        }
        await context.SaveChangesAsync(ct);

        return batch.Id;
    }
}
