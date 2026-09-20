using System.Globalization;
using HRM.Models;
using HRM.Services.Leave;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// Leaver final pay (CEO, 19 ก.ย. 2569 — พ.ร.บ.คุ้มครองแรงงาน ม.70 วรรคสอง: when the employer ends the
// employment, wages owed are due within 3 days, not at the next month-end).
//
//   1. PreviewAsync     — what the leaver is owed besides salary: statutory severance (SeveranceService),
//                         a suggested pay in lieu of notice (ม.17/1) and unused annual leave (ม.67).
//   2. CreateItemsAsync — those amounts become ad-hoc items (Pending → approved on /pay/adhoc like any
//                         other), targeted at the FinalPay run of the resign month.
//   3. CreateRunAsync   — a FinalPay run for the resign month holding just this leaver (more can be
//                         added). It is calculated like a regular run for its members only (salary up to
//                         the resign date, OT, SSO, PF, tax settled on actual income), approved through
//                         the same workflow, and has its own bank file. The regular run of that period
//                         skips its members (PayrollEligibility.PaidInFinalPayRunAsync).
public class FinalPayService(IDbContextFactory<HRMContext> dbFactory, SeveranceService severance, LeaveBalanceService leave)
{
    public const string NoticePayCode = "NOTICE_PAY";
    public const string LeavePayoutCode = "LEAVE_PAYOUT";
    // the statutory annual holiday (ม.30) — seeded leave types carry the section in LawReference
    public const string AnnualLeaveLawReference = "มาตรา 30";

    public sealed record Settlement(
        long HremployeeId, string EmpNo, string Name, DateOnly ResignDate, SeparationType? SeparationType,
        decimal DailyWage, decimal MonthlyWage,
        decimal? SeveranceAmount, int SeveranceDays, string? SeveranceNote,
        decimal SuggestedNoticePay, decimal AnnualLeaveDaysLeft, decimal SuggestedLeavePayout,
        string Period, IReadOnlyList<Pay_AdhocPayItem> ExistingItems, Pay_PayrollRun? OpenFinalPayRun);

    public async Task<Settlement> PreviewAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var emp = await db.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงาน");
        if (emp.ResignDate is null)
            throw new InvalidOperationException("พนักงานคนนี้ยังไม่มีวันที่ออก — ส่งคำขอสิ้นสุดการจ้างงานและรออนุมัติก่อน");
        var resign = DateOnly.FromDateTime(emp.ResignDate.Value);

        var isDaily = (emp.DailyWage ?? 0m) > 0m && (emp.SalaryAmt ?? 0m) <= 0m;
        var monthly = isDaily ? (emp.DailyWage ?? 0m) * 30m : emp.SalaryAmt ?? 0m;
        var daily = isDaily ? emp.DailyWage ?? 0m : Math.Round(monthly / 30m, 2, MidpointRounding.AwayFromZero);

        decimal? severanceAmount = null; var severanceDays = 0; string? severanceNote;
        try
        {
            var s = await severance.PreviewAsync(hremployeeId, ct);
            severanceAmount = s.Amount; severanceDays = s.EntitledDays;
            severanceNote = s.TierDescription;
        }
        catch (InvalidOperationException ex) { severanceNote = ex.Message; }

        // ม.67: unused annual holiday is paid on termination — except dismissal under ม.119
        var leaveDays = 0m;
        if (emp.SeparationType != SeparationType.TerminationSection119)
        {
            var annualTypeIds = await db.Lve_LeaveTypes.Where(t => t.LawReference == AnnualLeaveLawReference).Select(t => t.Id).ToListAsync(ct);
            var balances = await leave.GetBalancesAsync(hremployeeId, emp.companyid, year: resign.Year, ct: ct);
            leaveDays = Math.Max(0m, balances.Where(b => annualTypeIds.Contains(b.LeaveTypeId)).Sum(b => b.RemainingDays));
        }

        // ม.17/1 — only when the EMPLOYER ends the employment without full notice. A safe default is one
        // pay period's wage; HR edits it to the actual shortfall (the notice would have taken effect on
        // the pay date after next).
        var noticeApplies = emp.SeparationType is SeparationType.TerminationOrdinary;
        var period = Period(resign);
        var items = await db.Pay_AdhocPayItems.Include(a => a.Pay_PayItemType)
            .Where(a => a.HremployeeId == hremployeeId && a.TargetPeriod == period
                        && a.Status != PayAdhocItemStatus.Rejected && a.Status != PayAdhocItemStatus.Cancelled)
            .ToListAsync(ct);
        var openRun = await db.Pay_PayrollRunMembers
            .Join(db.Pay_PayrollRuns, m => m.PayrollRunId, r => r.Id, (m, r) => new { m.HremployeeId, Run = r })
            .Where(x => x.HremployeeId == hremployeeId && x.Run.RunType == PayrollRunType.FinalPay && x.Run.Status != PayrollRunStatus.Cancelled)
            .Select(x => x.Run).FirstOrDefaultAsync(ct);

        return new Settlement(emp.id, emp.EmpNo, $"{emp.EmpName} {emp.EmpSurname}".Trim(), resign, emp.SeparationType,
            daily, monthly, severanceAmount, severanceDays, severanceNote,
            noticeApplies ? monthly : 0m, leaveDays, Math.Round(leaveDays * daily, 2, MidpointRounding.AwayFromZero),
            period, items, openRun);
    }

    // Pay in lieu of notice + unused annual leave as ad-hoc items of the leaver run. Severance goes
    // through SeveranceService.SubmitAsync (tax split, statutory minimum) with targetRunType FinalPay.
    public async Task<int> CreateItemsAsync(long hremployeeId, decimal noticePay, decimal leaveDays, decimal leavePayout,
        long actorUserId, CancellationToken ct = default)
    {
        if (noticePay < 0 || leavePayout < 0 || leaveDays < 0) throw new InvalidOperationException("จำนวนเงินติดลบไม่ได้");
        var s = await PreviewAsync(hremployeeId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var types = await db.Pay_PayItemTypes.Where(t => t.Code == NoticePayCode || t.Code == LeavePayoutCode)
            .ToDictionaryAsync(t => t.Code, t => t.Id, ct);
        if (types.Count < 2) throw new InvalidOperationException("ไม่พบประเภทรายการ NOTICE_PAY / LEAVE_PAYOUT — ต้องลง migration ก่อน");

        var created = 0;
        void Add(string code, decimal amount, string reason)
        {
            if (amount <= 0) return;
            if (s.ExistingItems.Any(i => i.Pay_PayItemType?.Code == code))
                throw new InvalidOperationException($"มีรายการ {code} ของพนักงานคนนี้ในงวด {s.Period} อยู่แล้ว — แก้/ยกเลิกที่หน้ารายการเฉพาะกิจ");
            db.Pay_AdhocPayItems.Add(new Pay_AdhocPayItem
            {
                HremployeeId = hremployeeId, PayItemTypeId = types[code], TargetPeriod = s.Period,
                TargetRunType = PayrollRunType.FinalPay, Amount = amount, IsTaxable = true, Reason = reason,
                Status = PayAdhocItemStatus.Pending, RequestedByUserId = actorUserId,
            });
            created++;
        }
        Add(NoticePayCode, noticePay, $"สินจ้างแทนการบอกกล่าวล่วงหน้า (ม.17/1) — ออก {s.ResignDate:dd/MM/yyyy}");
        Add(LeavePayoutCode, leavePayout, $"ค่าจ้างวันหยุดพักผ่อนประจำปีที่ไม่ได้ใช้ {leaveDays:0.##} วัน (ม.67)");
        await db.SaveChangesAsync(ct);
        return created;
    }

    // A FinalPay run for the leaver's resign month (the half-month term containing the resign date for a
    // company paying twice a month), with this leaver as its first member. Default pay date: 3 days after
    // the resign date (ม.70).
    public async Task<long> CreateRunAsync(long hremployeeId, DateOnly? payDate, long actorUserId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await PayrollStepPermission.EnsureAsync(db, actorUserId, PayrollAction.Calculate, ct);
        var emp = await db.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct) ?? throw new InvalidOperationException("ไม่พบพนักงาน");
        if (emp.ResignDate is null) throw new InvalidOperationException("พนักงานคนนี้ยังไม่มีวันที่ออก");
        var resign = DateOnly.FromDateTime(emp.ResignDate.Value);

        var (start, end, term) = await TermContainingAsync(db, emp.companyid, resign, ct);
        var run = new Pay_PayrollRun
        {
            CompanyId = emp.companyid, PayrollPeriod = Period(resign), TermNo = term,
            PeriodStart = start, PeriodEnd = end, PayDate = payDate ?? resign.AddDays(3),
            RunType = PayrollRunType.FinalPay, Status = PayrollRunStatus.Draft, CreatedByUserId = actorUserId,
        };
        await ValidateMemberAsync(db, run, emp, ct);   // before anything is written — a clear message, never a half-made run
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Pay_PayrollRuns.Add(run);
        await db.SaveChangesAsync(ct);
        await AddMemberCoreAsync(db, run, emp, actorUserId, ct);
        await tx.CommitAsync(ct);
        return run.Id;
    }

    public async Task AddMemberAsync(long runId, long hremployeeId, long actorUserId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await PayrollStepPermission.EnsureAsync(db, actorUserId, PayrollAction.Calculate, ct);
        var run = await db.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct) ?? throw new InvalidOperationException("ไม่พบรอบ");
        var emp = await db.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct) ?? throw new InvalidOperationException("ไม่พบพนักงาน");
        await AddMemberCoreAsync(db, run, emp, actorUserId, ct);
    }

    public async Task RemoveMemberAsync(long runId, long hremployeeId, long actorUserId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await PayrollStepPermission.EnsureAsync(db, actorUserId, PayrollAction.Calculate, ct);
        var run = await db.Pay_PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct) ?? throw new InvalidOperationException("ไม่พบรอบ");
        if (run.Status is not (PayrollRunStatus.Draft or PayrollRunStatus.Calculated))
            throw new InvalidOperationException("รอบนี้ส่งอนุมัติแล้ว — เปลี่ยนรายชื่อไม่ได้");
        await db.Pay_PayrollRunMembers.Where(m => m.PayrollRunId == runId && m.HremployeeId == hremployeeId).ExecuteDeleteAsync(ct);
    }

    public async Task<List<Pay_PayrollRunMember>> MembersAsync(long runId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Pay_PayrollRunMembers.Where(m => m.PayrollRunId == runId).OrderBy(m => m.EmpNo).ToListAsync(ct);
    }

    private static async Task AddMemberCoreAsync(HRMContext db, Pay_PayrollRun run, Hremployee emp, long actorUserId, CancellationToken ct)
    {
        await ValidateMemberAsync(db, run, emp, ct);
        db.Pay_PayrollRunMembers.Add(new Pay_PayrollRunMember
        {
            PayrollRunId = run.Id, HremployeeId = emp.id, EmpNo = emp.EmpNo, AddedByUserId = actorUserId, AddedAt = DateTime.Now,
        });
        await db.SaveChangesAsync(ct);
    }

    // run may be unsaved (Id 0) — CreateRunAsync validates before writing anything
    private static async Task ValidateMemberAsync(HRMContext db, Pay_PayrollRun run, Hremployee emp, CancellationToken ct)
    {
        if (run.RunType != PayrollRunType.FinalPay) throw new InvalidOperationException("เพิ่มรายชื่อได้เฉพาะรอบจ่ายคนออก");
        if (run.Status is not (PayrollRunStatus.Draft or PayrollRunStatus.Calculated))
            throw new InvalidOperationException("รอบนี้ส่งอนุมัติแล้ว — เพิ่มรายชื่อไม่ได้");
        if (emp.companyid != run.CompanyId) throw new InvalidOperationException("พนักงานต่างบริษัท");
        if (emp.ResignDate is null || emp.SeparationType is null)
            throw new InvalidOperationException($"{emp.EmpNo} ยังไม่มีคำขอสิ้นสุดการจ้างงานที่อนุมัติแล้ว");
        var resign = DateOnly.FromDateTime(emp.ResignDate.Value);
        if (resign < run.PeriodStart || resign > run.PeriodEnd)
            throw new InvalidOperationException($"{emp.EmpNo} ออกวันที่ {resign:dd/MM/yyyy} ไม่อยู่ในงวดของรอบนี้ ({run.PeriodStart:dd/MM/yyyy}–{run.PeriodEnd:dd/MM/yyyy})");

        var otherFinal = await db.Pay_PayrollRunMembers
            .Join(db.Pay_PayrollRuns, m => m.PayrollRunId, r => r.Id, (m, r) => new { m.HremployeeId, Run = r })
            .Where(x => x.HremployeeId == emp.id && x.Run.RunType == PayrollRunType.FinalPay && x.Run.Status != PayrollRunStatus.Cancelled)
            .Select(x => x.Run.Id).FirstOrDefaultAsync(ct);
        if (otherFinal != 0)
            throw new InvalidOperationException(otherFinal == run.Id ? $"{emp.EmpNo} อยู่ในรอบนี้แล้ว" : $"{emp.EmpNo} อยู่ในรอบจ่ายคนออก #{otherFinal} แล้ว");

        // already calculated in the regular run of the same period and term → would be paid twice
        var inRegular = await db.Pay_PayrollEmployees
            .Where(pe => pe.HremployeeId == emp.id && !pe.IsExcluded
                         && pe.Pay_PayrollRun.RunType == PayrollRunType.Regular && pe.Pay_PayrollRun.CompanyId == run.CompanyId
                         && pe.Pay_PayrollRun.PayrollPeriod == run.PayrollPeriod && pe.Pay_PayrollRun.TermNo == run.TermNo
                         && pe.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled)
            .Select(pe => new { pe.Pay_PayrollRun.Id, pe.Pay_PayrollRun.Status }).FirstOrDefaultAsync(ct);
        if (inRegular is not null)
            throw new InvalidOperationException(inRegular.Status is PayrollRunStatus.Draft or PayrollRunStatus.Calculated
                ? $"{emp.EmpNo} ถูกคำนวณอยู่ในรอบปกติ #{inRegular.Id} ของงวดนี้ — พักการจ่ายคนนี้ในรอบปกตินั้น แล้วกดคำนวณใหม่ก่อน จึงสร้าง/เพิ่มในรอบจ่ายคนออกได้"
                : $"{emp.EmpNo} ได้รับเงินเดือนงวดนี้ในรอบปกติ #{inRegular.Id} ไปแล้ว — จ่ายค่าชดเชย/ค่าบอกกล่าว/พักร้อนผ่านรอบโบนัสของงวดนี้แทน");
    }

    // same term split as /pay/runs/create: a company with a twice-monthly schedule has term 1 = day 1..(second
    // term start − 1) and term 2 = the rest; otherwise the whole month is term 1
    private static async Task<(DateOnly Start, DateOnly End, int Term)> TermContainingAsync(HRMContext db, string companyId, DateOnly day, CancellationToken ct)
    {
        var first = new DateOnly(day.Year, day.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var semi = await db.Pay_PaySchedules
            .Where(s => s.CompanyId == companyId && s.IsActive && s.PeriodsPerMonth == 2
                        && s.EffectiveFrom <= first && (s.EffectiveTo == null || s.EffectiveTo >= first))
            .OrderByDescending(s => s.EffectiveFrom).FirstOrDefaultAsync(ct);
        if (semi is null) return (first, last, 1);
        var secondStart = Math.Clamp(semi.SecondTermStartDay, 2, 28);
        return day.Day < secondStart
            ? (first, new DateOnly(day.Year, day.Month, secondStart - 1), 1)
            : (new DateOnly(day.Year, day.Month, secondStart), last, 2);
    }

    private static string Period(DateOnly d) => d.ToString("yyyyMM", CultureInfo.InvariantCulture);
}
