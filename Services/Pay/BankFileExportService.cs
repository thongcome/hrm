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
        if (run.RunType == PayrollRunType.Reversal)
            throw new InvalidOperationException("รอบกลับรายการไม่มีการโอนเงิน — ไฟล์ธนาคารสร้างจากรอบปรับปรุงที่คำนวณใหม่แทน");

        var employees = await context.Pay_PayrollEmployees
            .Include(e => e.Hremployee)
            .Where(e => e.PayrollRunId == runId && !e.IsExcluded)
            .OrderBy(e => e.EmpNo)
            .ToListAsync(ct);

        // รอบปรับปรุง = คำนวณงวดใหม่ทั้งงวดหลังกลับรายการ แต่เงินของรอบต้นทาง "โอนไปแล้ว" — ไฟล์ธนาคารจึงต้องเป็น
        // ส่วนต่าง (ใหม่ − ที่โอนแล้ว) ไม่ใช่ยอดเต็มงวดซ้ำ (12 ก.ย. 2569 พบจากเทสทั้งปี 2568) ยอดใหม่ที่ต่ำกว่าที่โอนแล้ว
        // โอนติดลบไม่ได้ → สร้างรายการหักคืนในงวดถัดไปให้ HR อนุมัติ  ถ้ารอบต้นทางยังไม่ได้จ่าย/ยังไม่ได้ส่งไฟล์ = ไฟล์เต็มงวดตามเดิม
        Pay_PayrollRun? paidOriginal = null;
        if (run.RunType == PayrollRunType.Adjustment && run.AdjustmentOfRunId is long originalId)
        {
            var original = await context.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == originalId, ct);
            var sent = original is not null && (original.Status == PayrollRunStatus.Paid
                || await context.Pay_BankFileExportBatches.AnyAsync(b => b.PayrollRunId == originalId && b.Status >= BankFileExportStatus.Downloaded, ct));
            if (sent) paidOriginal = original;
        }

        var lines = new List<(Pay_PayrollEmployee Emp, decimal Amount)>();
        var recoveries = new List<(long HremployeeId, string EmpNo, decimal PaidBefore, decimal NewNet)>();
        string? remark = null;
        if (paidOriginal is null)
        {
            lines.AddRange(employees.Select(e => (e, e.NetPay)));
        }
        else
        {
            var paidBefore = await context.Pay_PayrollEmployees
                .Where(e => e.PayrollRunId == paidOriginal.Id && !e.IsExcluded)
                .Select(e => new { e.HremployeeId, e.EmpNo, e.NetPay })
                .ToListAsync(ct);
            var paidByEmp = paidBefore.ToDictionary(p => p.HremployeeId, p => p.NetPay);
            foreach (var e in employees)
            {
                var delta = e.NetPay - paidByEmp.GetValueOrDefault(e.HremployeeId);
                if (delta > 0m) lines.Add((e, delta));
                else if (delta < 0m) recoveries.Add((e.HremployeeId, e.EmpNo ?? "?", paidByEmp[e.HremployeeId], e.NetPay));
            }
            // คนที่โอนไปแล้วแต่ไม่อยู่ในรอบปรับปรุง (ถูกกันออก) = ต้องเรียกคืนทั้งก้อน
            var inAdjustment = employees.Select(e => e.HremployeeId).ToHashSet();
            foreach (var p in paidBefore.Where(p => !inAdjustment.Contains(p.HremployeeId) && p.NetPay > 0m))
                recoveries.Add((p.HremployeeId, p.EmpNo ?? "?", p.NetPay, 0m));

            if (lines.Count == 0 && recoveries.Count == 0)
                throw new InvalidOperationException($"รอบปรับปรุงนี้ยอดสุทธิทุกคนเท่ากับรอบ #{paidOriginal.Id} ที่โอนไปแล้ว — ไม่มีส่วนต่างต้องโอน");

            await CreateRecoveryItemsAsync(context, run, paidOriginal, recoveries, actorUserId, ct);
            remark = $"ส่วนต่างจากรอบ #{paidOriginal.Id}: โอนเพิ่ม {lines.Count} คน {lines.Sum(l => l.Amount):N2} บาท"
                     + (recoveries.Count > 0
                         ? $" · เรียกคืน {recoveries.Count} คน {recoveries.Sum(r => r.PaidBefore - r.NewNet):N2} บาท ({string.Join(", ", recoveries.Take(5).Select(r => r.EmpNo))}{(recoveries.Count > 5 ? " …" : "")}) — สร้างรายการหักคืนงวดถัดไปไว้แล้ว รอ HR อนุมัติ"
                         : "");
        }

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
            : $"bankfile_{(paidOriginal is null ? "" : "delta_")}{runId}_{DateTime.Now:yyyyMMddHHmmss}.csv";
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
            DeltaOfPayrollRunId = paidOriginal?.Id,
            Remark = remark,
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

    // ยอดใหม่ต่ำกว่าที่โอนไปแล้ว → รายการหัก (ADHOC_DEDUCT, ไม่กระทบภาษี) ในงวดถัดไป สถานะรอ HR อนุมัติ
    // ทำเครื่องหมายไว้ใน Remark กันสร้างซ้ำถ้าสร้างไฟล์ธนาคารรอบเดิมอีกครั้ง
    private static async Task CreateRecoveryItemsAsync(HRMContext context, Pay_PayrollRun run, Pay_PayrollRun original,
        List<(long HremployeeId, string EmpNo, decimal PaidBefore, decimal NewNet)> recoveries, long actorUserId, CancellationToken ct)
    {
        if (recoveries.Count == 0) return;
        var deductType = await context.Pay_PayItemTypes.FirstOrDefaultAsync(t => t.Code == "ADHOC_DEDUCT", ct)
            ?? throw new InvalidOperationException("ไม่พบรายการรับ-จ่ายรหัส ADHOC_DEDUCT สำหรับสร้างรายการหักคืน");
        var nextPeriod = run.PeriodStart.AddMonths(1).ToString("yyyyMM", System.Globalization.CultureInfo.InvariantCulture);
        var marker = $"BANKDELTA:{run.Id}";
        var existing = (await context.Pay_AdhocPayItems
                .Where(a => a.Remark != null && a.Remark.StartsWith(marker) && a.Status != PayAdhocItemStatus.Cancelled && a.Status != PayAdhocItemStatus.Rejected)
                .Select(a => a.HremployeeId).ToListAsync(ct)).ToHashSet();
        foreach (var r in recoveries.Where(r => !existing.Contains(r.HremployeeId)))
        {
            context.Pay_AdhocPayItems.Add(new Pay_AdhocPayItem
            {
                HremployeeId = r.HremployeeId,
                PayItemTypeId = deductType.Id,
                TargetPeriod = nextPeriod,
                TargetRunType = PayrollRunType.Regular,
                Amount = r.PaidBefore - r.NewNet,
                IsTaxable = false,
                Reason = $"เรียกคืนเงินที่โอนเกินจากรอบ #{original.Id} งวด {original.PayrollPeriod}: โอนไปแล้ว {r.PaidBefore:N2} ยอดใหม่ตามรอบปรับปรุง #{run.Id} = {r.NewNet:N2}",
                Remark = $"{marker}:{r.HremployeeId}",
                Status = PayAdhocItemStatus.Pending,
                RequestedByUserId = actorUserId,
            });
        }
    }
}
