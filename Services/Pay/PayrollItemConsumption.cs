using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// "รายการที่รอบหนึ่งกินไปแล้ว" มีสามชนิด และทั้งสามต้องคืนพร้อมกันเสมอ:
//   Pay_AdhocPayItem            (โบนัส/ค่าคอมมิชชั่น/เงินหักรายครั้ง)  Approved -> Consumed
//   Pay_EmployeeLoanInstallment (งวดผ่อนเงินกู้)                        Pending  -> Consumed + ลดยอดคงเหลือ
//   Pay_SalaryAdvance           (เงินเบิกล่วงหน้า)                      Approved -> Deducted
//
// เดิมการคืนสถานะเขียนไว้ที่เดียวคือตอน "ยกเลิกทั้งรอบ" (CancelAsync) ทำให้สองทางนี้รั่ว:
//   · พักการจ่ายรายคน (hold) แล้วคำนวณใหม่ — คนนั้นหลุดจากลูป รายการค้าง Consumed ชี้รอบนี้ตลอดไป
//     รอบอื่นหยิบไม่ได้ (หยิบเฉพาะ Approved/Pending) = โบนัสหาย งวดผ่อนถูกนับว่าหักแล้วทั้งที่ไม่เคยหักเงิน
//   · กันพนักงานออกจากรอบ (IsExcluded) — คนนั้นถูกคำนวณและกินรายการไปแล้ว แต่ถูกตัดออกจาก
//     ไฟล์ธนาคาร/สลิป/GL/ยอดสะสม จึงไม่ได้เงินสักบาท ขณะที่ยอดหนี้เงินกู้ลดไปแล้ว
//
// ที่นี่คือที่เดียวที่คืนสถานะ ใช้ร่วมกันทั้งสามที่ (ยกเลิกรอบ / เริ่มคำนวณใหม่ / กันคนออก)
// พอร์ตจาก Advance.Payroll (handoff 20 ก.ย. 2569 ข้อ 1)
public static class PayrollItemConsumption
{
    /// <summary>
    /// คืนรายการที่รอบนี้กินไปแล้วกลับสู่สถานะพร้อมใช้ ถ้าระบุ employeeIds จะคืนเฉพาะของคนเหล่านั้น
    /// ไม่เรียก SaveChanges — ผู้เรียกคุม transaction เอง คืนค่า = จำนวนรายการที่คืน
    /// </summary>
    public static async Task<int> ReleaseAsync(HRMContext context, long runId,
        IReadOnlyCollection<long>? employeeIds = null, CancellationToken ct = default)
    {
        var all = employeeIds is null;
        var ids = employeeIds ?? Array.Empty<long>();
        var released = 0;

        var adhocItems = await context.Pay_AdhocPayItems
            .Where(a => a.ConsumedByPayrollRunId == runId && (all || ids.Contains(a.HremployeeId)))
            .ToListAsync(ct);
        foreach (var item in adhocItems)
        {
            item.Status = PayAdhocItemStatus.Approved;
            item.ConsumedByPayrollRunId = null;
            released++;
        }

        var advances = await context.Pay_SalaryAdvances
            .Where(a => a.ConsumedByPayrollRunId == runId && (all || ids.Contains(a.HremployeeId)))
            .ToListAsync(ct);
        foreach (var adv in advances)
        {
            adv.Status = PaySalaryAdvanceStatus.Approved;
            adv.ConsumedByPayrollRunId = null;
            released++;
        }

        // งวดผ่อน: ต้องคืนยอดคงเหลือของเงินกู้ด้วย ไม่งั้นลูกหนี้หายไปเท่ากับงวดนั้น
        var installments = await context.Pay_EmployeeLoanInstallments
            .Include(i => i.Pay_EmployeeLoan)
            .Where(i => i.ConsumedByPayrollRunId == runId
                        && (all || ids.Contains(i.Pay_EmployeeLoan.HremployeeId)))
            .ToListAsync(ct);
        foreach (var inst in installments)
        {
            inst.Status = Pay_LoanInstallmentStatus.Pending;
            inst.ConsumedByPayrollRunId = null;
            inst.Pay_EmployeeLoan.RemainingBalance = inst.BalanceAfter + inst.Amount;
            if (inst.Pay_EmployeeLoan.Status == Pay_EmployeeLoanStatus.PaidOff)
                inst.Pay_EmployeeLoan.Status = Pay_EmployeeLoanStatus.Active;
            released++;
        }

        return released;
    }

    /// <summary>
    /// เอาคนที่เคยถูกกันออกกลับเข้ารอบ — ผูกรายการที่แถวผลลัพธ์ของเขาอ้างถึงกลับเป็น "กินแล้วโดยรอบนี้"
    /// ตามรายการใน Pay_PayrollLineItem ของแถวนั้น (ยอดในแถวคำนวณไว้แล้ว ถ้าไม่ผูกคืนจะถูกจ่ายซ้ำในรอบหน้า)
    /// ถ้ารายการไหนถูกอีกรอบหนึ่งหยิบไปแล้วจะโยน error ให้ไปคำนวณรอบนี้ใหม่แทนการเดา
    /// </summary>
    public static async Task ReconsumeAsync(HRMContext context, long runId, long payrollEmployeeId,
        string empNo, CancellationToken ct = default)
    {
        var refs = await context.Pay_PayrollLineItems
            .Where(li => li.PayrollEmployeeId == payrollEmployeeId && li.SourceRefTable != null && li.SourceRefId != null)
            .Select(li => new { li.SourceRefTable, li.SourceRefId })
            .ToListAsync(ct);
        if (refs.Count == 0) return;

        var adhocIds = refs.Where(r => r.SourceRefTable == "Pay_AdhocPayItem").Select(r => r.SourceRefId!.Value).ToList();
        var advanceIds = refs.Where(r => r.SourceRefTable == "Pay_SalaryAdvance").Select(r => r.SourceRefId!.Value).ToList();
        var installmentIds = refs.Where(r => r.SourceRefTable == "Pay_EmployeeLoanInstallment").Select(r => r.SourceRefId!.Value).ToList();

        foreach (var item in await context.Pay_AdhocPayItems.Where(a => adhocIds.Contains(a.Id)).ToListAsync(ct))
        {
            EnsureFree(item.ConsumedByPayrollRunId, runId, empNo);
            item.Status = PayAdhocItemStatus.Consumed;
            item.ConsumedByPayrollRunId = runId;
        }
        foreach (var adv in await context.Pay_SalaryAdvances.Where(a => advanceIds.Contains(a.Id)).ToListAsync(ct))
        {
            EnsureFree(adv.ConsumedByPayrollRunId, runId, empNo);
            adv.Status = PaySalaryAdvanceStatus.Deducted;
            adv.ConsumedByPayrollRunId = runId;
        }
        foreach (var inst in await context.Pay_EmployeeLoanInstallments.Include(i => i.Pay_EmployeeLoan)
                     .Where(i => installmentIds.Contains(i.Id)).ToListAsync(ct))
        {
            EnsureFree(inst.ConsumedByPayrollRunId, runId, empNo);
            inst.Status = Pay_LoanInstallmentStatus.Consumed;
            inst.ConsumedByPayrollRunId = runId;
            inst.Pay_EmployeeLoan.RemainingBalance = inst.BalanceAfter;
            if (inst.InstallmentNo == inst.Pay_EmployeeLoan.TotalInstallments)
                inst.Pay_EmployeeLoan.Status = Pay_EmployeeLoanStatus.PaidOff;
        }
    }

    private static void EnsureFree(long? consumedBy, long runId, string empNo)
    {
        if (consumedBy is long other && other != runId)
            throw new InvalidOperationException(
                $"เอา {empNo} กลับเข้ารอบไม่ได้ — รายการเงินได้/เงินหักของเขาถูกรอบ #{other} หยิบไปแล้ว ให้คำนวณรอบนี้ใหม่แทน");
    }
}
