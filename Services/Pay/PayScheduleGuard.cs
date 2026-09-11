using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// กติกาที่ทำให้ "ตั้งรอบจ่าย" แก้ยากขึ้น (CEO, 12 ก.ย. 2569 — เลือกข้อ 1–3, ไม่เอาอนุมัติผ่าน workflow):
//   1. แถวที่มีงวดอนุมัติแล้วภายใต้แถวนั้น = ล็อก แก้ได้แค่ชื่อ/หมายเหตุ และ "วันสิ้นสุด" (ไม่ย้อนก่อนงวดอนุมัติล่าสุด)
//   2. วันมีผลของแถวใหม่ต้องเป็นวันที่ 1 ของเดือน และไม่ก่อนเดือนถัดจากงวดที่อนุมัติล่าสุด (ไปข้างหน้าเท่านั้น)
//   3. ทุกการเปลี่ยนต้องมีเหตุผล (หน้าจอบังคับ + Pay_PayScheduleChangeLog)
// ส่วนที่เป็นกติกาล้วน ๆ แยกเป็น static ทดสอบได้โดยไม่ต้องมีฐานข้อมูล
public static class PayScheduleGuard
{
    // งวดที่อนุมัติแล้ว (Approved ขึ้นไป ไม่นับยกเลิก) ของบริษัท — วันเริ่มงวดล่าสุด
    public static async Task<DateOnly?> LastApprovedPeriodStartAsync(HRMContext db, string companyId, CancellationToken ct = default)
        => await db.Pay_PayrollRuns
            .Where(r => r.CompanyId == companyId && r.Status >= PayrollRunStatus.Approved && r.Status != PayrollRunStatus.Cancelled)
            .OrderByDescending(r => r.PeriodStart)
            .Select(r => (DateOnly?)r.PeriodStart)
            .FirstOrDefaultAsync(ct);

    // แถวรอบจ่ายที่ "ถูกใช้แล้ว" = มีงวดอนุมัติแล้วที่วันเริ่มงวดอยู่ในช่วงมีผลของแถว (นับแบบระมัดระวัง ไม่ดูกลุ่มพนักงาน)
    public static async Task<HashSet<long>> LockedScheduleIdsAsync(HRMContext db, string companyId, IEnumerable<Pay_PaySchedule> schedules, CancellationToken ct = default)
    {
        var approvedStarts = await db.Pay_PayrollRuns
            .Where(r => r.CompanyId == companyId && r.Status >= PayrollRunStatus.Approved && r.Status != PayrollRunStatus.Cancelled)
            .Select(r => r.PeriodStart)
            .Distinct()
            .ToListAsync(ct);
        return schedules
            .Where(s => approvedStarts.Any(p => p >= s.EffectiveFrom && (s.EffectiveTo == null || p <= s.EffectiveTo)))
            .Select(s => s.Id)
            .ToHashSet();
    }

    // วันสิ้นสุดของแถวที่ล็อกแล้ว ห้ามย้อนก่อนวันเริ่มงวดอนุมัติล่าสุดที่อยู่ภายใต้แถวนั้น
    public static async Task<DateOnly?> MinEffectiveToAsync(HRMContext db, string companyId, Pay_PaySchedule s, CancellationToken ct = default)
        => await db.Pay_PayrollRuns
            .Where(r => r.CompanyId == companyId && r.Status >= PayrollRunStatus.Approved && r.Status != PayrollRunStatus.Cancelled
                        && r.PeriodStart >= s.EffectiveFrom && (s.EffectiveTo == null || r.PeriodStart <= s.EffectiveTo))
            .OrderByDescending(r => r.PeriodStart)
            .Select(r => (DateOnly?)r.PeriodStart)
            .FirstOrDefaultAsync(ct);

    // ── กติกาล้วน ๆ ────────────────────────────────────────────────────────
    // วันมีผลที่ตั้งได้เร็วสุด = วันที่ 1 ของเดือนถัดจากงวดที่อนุมัติล่าสุด (ไม่มีงวดอนุมัติ = ตั้งได้ทุกเดือน)
    public static DateOnly? MinEffectiveFrom(DateOnly? lastApprovedPeriodStart)
        => lastApprovedPeriodStart is DateOnly p ? new DateOnly(p.Year, p.Month, 1).AddMonths(1) : null;

    public static string? ValidateEffectiveFrom(DateOnly effectiveFrom, DateOnly? lastApprovedPeriodStart)
    {
        if (effectiveFrom.Day != 1)
            return "วันมีผลต้องเป็นวันที่ 1 ของเดือน — รอบจ่ายเปลี่ยนได้ที่ต้นเดือนเท่านั้น";
        var min = MinEffectiveFrom(lastApprovedPeriodStart);
        if (min is DateOnly m && effectiveFrom < m)
            return $"ตั้งย้อนหลังไม่ได้ — งวดล่าสุดที่อนุมัติแล้วเริ่ม {lastApprovedPeriodStart:dd/MM/yyyy} วันมีผลต้องไม่ก่อน {m:dd/MM/yyyy}";
        return null;
    }

    public static string? ValidateEffectiveTo(DateOnly? effectiveTo, DateOnly effectiveFrom, DateOnly? minEffectiveTo)
    {
        if (effectiveTo is not DateOnly to) return null;
        if (to < effectiveFrom) return "วันสิ้นสุดต้องไม่ก่อนวันมีผล";
        if (minEffectiveTo is DateOnly min && to < min)
            return $"ปิดแถวย้อนหลังไม่ได้ — มีงวดที่อนุมัติแล้วภายใต้แถวนี้ถึง {min:dd/MM/yyyy} วันสิ้นสุดต้องไม่ก่อนวันนั้น";
        return null;
    }

    public static string? ValidateReason(string? reason)
        => string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5
            ? "ต้องระบุเหตุผลของการเปลี่ยนแปลง (อย่างน้อย 5 ตัวอักษร)"
            : null;
}
