namespace Advance.Payroll.Engine;

using System.Text;
using Advance.Payroll.Domain;
using Advance.Payroll.Data;
using Microsoft.EntityFrameworkCore;

// Generic self-balancing CSV journal for a Posted+ run: every earning line
// debits its Pay_PayItemType.GLAccountCode, every deduction credits its
// GLAccountCode, and one closing credit line for the total net pay balances
// the journal against a "net pay payable" account — standard double-entry
// shape without assuming any particular chart of accounts.
public class GLExportService
{
    private const string NetPayableAccountCode = "2100-NETPAY-PAYABLE";

    private readonly IDbContextFactory<PayrollDbContext> _dbFactory;
    private readonly PrivateFileStorage _fileStorage;

    public GLExportService(IDbContextFactory<PayrollDbContext> dbFactory, PrivateFileStorage fileStorage)
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

        // บัญชีของรายการที่ไม่ได้อยู่บนสลิป (audit M10): ฝั่งนายจ้าง + เงินเดือนค้างจ่าย อ่านจาก Pay_GLAccountMapping
        var mappings = await context.Pay_GLAccountMappings
            .Where(m => m.CompanyId == run.CompanyId && m.IsActive)
            .ToDictionaryAsync(m => m.MappingKey, ct);
        string Account(string key, bool debit)
        {
            var code = mappings.TryGetValue(key, out var m) ? (debit ? m.DebitAccountCode : m.CreditAccountCode) : null;
            return string.IsNullOrWhiteSpace(code) ? $"UNMAPPED-{key}{(debit ? "" : "-PAYABLE")}" : code!;
        }
        var netPayableAccount = mappings.TryGetValue(GLMappingKeys.NetPayable, out var np) && !string.IsNullOrWhiteSpace(np.CreditAccountCode)
            ? np.CreditAccountCode! : NetPayableAccountCode;

        // ฝั่งนายจ้าง: เดบิตค่าใช้จ่าย เครดิตเจ้าหนี้ — ยอดจากแถวพนักงาน (ไม่ใช่รายการรับ-จ่าย)
        var employer = await context.Pay_PayrollEmployees
            .Where(e => e.PayrollRunId == runId && !e.IsExcluded)
            .GroupBy(e => 1)
            .Select(g => new
            {
                Sso = g.Sum(e => e.SocialSecurityCompanyAmount),
                Pf = g.Sum(e => e.ProvidentFundCompanyAmount),
                Insurance = g.Sum(e => e.InsuranceCompanyAmount),
                WelfareFund = g.Sum(e => e.WelfareFundCompanyAmount),
            })
            .FirstOrDefaultAsync(ct);
        var employerLines = new (string Key, string Label, decimal Amount)[]
        {
            (GLMappingKeys.EmployerSso, "ประกันสังคมส่วนนายจ้าง", employer?.Sso ?? 0m),
            (GLMappingKeys.EmployerProvidentFund, "เงินสมทบกองทุนสำรองเลี้ยงชีพ", employer?.Pf ?? 0m),
            (GLMappingKeys.EmployerInsurance, "เบี้ยประกันกลุ่มส่วนบริษัท", employer?.Insurance ?? 0m),
            (GLMappingKeys.EmployerWelfareFund, "กองทุนสงเคราะห์ลูกจ้างส่วนนายจ้าง", employer?.WelfareFund ?? 0m),
        }.Where(x => x.Amount != 0m).ToList();

        var csv = new StringBuilder();
        csv.AppendLine("GLAccountCode,Debit,Credit,Description");
        decimal totalDebit = 0, totalCredit = 0;
        foreach (var g in grouped)
        {
            csv.AppendLine($"{g.GLAccountCode},{g.Debit:0.00},{g.Credit:0.00},\"งวด {run.PayrollPeriod}\"");
            totalDebit += g.Debit;
            totalCredit += g.Credit;
        }
        csv.AppendLine($"{netPayableAccount},0.00,{totalNetPay:0.00},\"เงินเดือนค้างจ่าย งวด {run.PayrollPeriod}\"");
        totalCredit += totalNetPay;
        foreach (var (key, label, amount) in employerLines)
        {
            csv.AppendLine($"{Account(key, true)},{amount:0.00},0.00,\"{label} งวด {run.PayrollPeriod}\"");
            csv.AppendLine($"{Account(key, false)},0.00,{amount:0.00},\"{label} (ค้างนำส่ง) งวด {run.PayrollPeriod}\"");
            totalDebit += amount;
            totalCredit += amount;
        }

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
            GLAccountCode = netPayableAccount,
            DebitAmount = 0,
            CreditAmount = totalNetPay,
            Description = $"เงินเดือนค้างจ่าย งวด {run.PayrollPeriod}",
        });
        foreach (var (key, label, amount) in employerLines)
        {
            context.Pay_GLExportEntries.Add(new Pay_GLExportEntry
            {
                GLExportBatchId = batch.Id, GLAccountCode = Account(key, true), DebitAmount = amount, CreditAmount = 0,
                Description = $"{label} งวด {run.PayrollPeriod}",
            });
            context.Pay_GLExportEntries.Add(new Pay_GLExportEntry
            {
                GLExportBatchId = batch.Id, GLAccountCode = Account(key, false), DebitAmount = 0, CreditAmount = amount,
                Description = $"{label} (ค้างนำส่ง) งวด {run.PayrollPeriod}",
            });
        }
        await context.SaveChangesAsync(ct);

        return batch.Id;
    }
}
