namespace HRM.Services.Pay;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

// Emails a generated payslip PDF to the employee's registered email. Extracted
// from EssPayslips.razor's inline send once HR needed the identical action from
// the payroll-run screen ("an employee asks for their slip, HR opens them and
// sends it") — the send must stay identical across both entry points: same
// file-name shape, same audit entity ("Pay_PayslipEmail"), same email
// resolution (CUR address → AdnEmail fallback via EmployeeEmailResolver), so a
// change in one place can't silently diverge the other. Same extract-on-
// second-use precedent as EmployeeEmailResolver / OrgEmployeeResolverHelper.
//
// The PDF itself is already password-protected at generation time
// (PayslipPasswordService), so the password is never placed in the email body —
// the employee opens it with the pattern they already know, exactly as the ESS
// download does. Every send is audit-logged as a sensitive access (PDPA: a
// payslip is personal financial data leaving the system to an external
// mailbox), which is also what drives the "last sent" timestamp both screens
// show.
public class PayslipEmailService(
    IDbContextFactory<HRMContext> dbFactory,
    PrivateFileStorage fileStorage,
    EmailSender emailSender,
    IAuditLogger auditLogger)
{
    public record SendResult(bool Success, string? Email, string? ErrorReason);

    // origin distinguishes the ESS self-send from an HR-initiated send in the
    // audit trail, without changing what is sent.
    public async Task<SendResult> SendToEmployeeAsync(long payslipId, string origin, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var payslip = await context.Pay_Payslips
            .Include(p => p.Pay_PayrollEmployee).ThenInclude(pe => pe.Pay_PayrollRun)
            .Include(p => p.Pay_PayrollEmployee).ThenInclude(pe => pe.Hremployee)
            .FirstOrDefaultAsync(p => p.Id == payslipId, ct);
        if (payslip is null)
            return new(false, null, "ไม่พบสลิปเงินเดือน");

        var pe = payslip.Pay_PayrollEmployee;
        var run = pe.Pay_PayrollRun;

        var email = await EmployeeEmailResolver.ResolveAsync(context, pe.HremployeeId, ct);
        if (string.IsNullOrWhiteSpace(email))
            return new(false, null, "พนักงานยังไม่มีอีเมลในระบบ (ให้พนักงานกรอกอีเมลในโปรไฟล์ หรือ HR กรอกให้)");

        byte[] bytes;
        try
        {
            bytes = await fileStorage.ReadAsync(payslip.PdfStoragePath);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Payslip email: reading PDF failed for payslip {Id}", payslipId);
            return new(false, email, "อ่านไฟล์สลิปไม่สำเร็จ");
        }

        var emp = pe.Hremployee;
        var empName = $"{emp?.EmpName} {emp?.EmpSurname}".Trim();
        var fileName = $"payslip_{pe.EmpNo}_{run.PayrollPeriod}.pdf";
        var subject = $"สลิปเงินเดือน งวด {run.PayrollPeriod}";
        var body = $"<p>เรียนคุณ {empName}</p><p>แนบสลิปเงินเดือนงวด {run.PayrollPeriod} มาพร้อมนี้ (ไฟล์มีรหัสผ่านตามที่บริษัทกำหนด)</p>";

        try
        {
            await emailSender.SendEmailWithAttachmentAsync(email, subject, body, fileName, bytes);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Payslip email send failed for payslip {Id}", payslipId);
            return new(false, email, "ส่งอีเมลไม่สำเร็จ (ตรวจสอบการตั้งค่า SMTP)");
        }

        await auditLogger.LogAccessAsync("Pay_PayslipEmail", payslipId.ToString(), isSensitive: true,
            note: $"ส่งสลิปไปที่ {email} ({origin})");

        return new(true, email, null);
    }
}
