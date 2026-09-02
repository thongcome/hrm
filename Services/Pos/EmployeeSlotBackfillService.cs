namespace HRM.Services.Pos;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Populates the establishment (อัตรา) from the actual workforce so the org chart
// — which is slot-driven — can show people and vacancies. Owner model
// (2026-09-03): ประเภทพนักงาน → ตำแหน่ง (Pos_ExecType) → อัตรา (Pos_PositionSlot) →
// คน (empno in the slot). Employees carry POS_CODE (→ their existing Pos_ExecType,
// no new positions created) and EMPTYPE_CODE (→ the single employeetype master,
// 01=พนักงาน/02=กรรมการ). For every active employee not yet occupying an active
// slot, this creates one active slot in their org unit referencing that position
// and employee type, with the employee as occupant. Idempotent — re-running only
// fills employees who still have no slot. Never mutates existing slots.
//
// employeetype vs Pos_EmployeeType were historically duplicated (owner: "ใช้อัน
// ใดอันหนึ่งได้ มันคือค่าเดียวกัน"); this aligns Pos_EmployeeType to the employeetype
// master per company (the establishment tables FK to Pos_EmployeeType) so a
// slot's EmployeeTypeId can be resolved from the occupant's EMPTYPE_CODE.
public class EmployeeSlotBackfillService(IDbContextFactory<HRMContext> dbFactory)
{
    public record BackfillResult(int SlotsCreated, int Skipped, int EmployeeTypesAligned, int NoPositionMatch);

    public async Task<BackfillResult> BackfillAsync(string companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        // 1) Align Pos_EmployeeType to the single employeetype master for this
        //    company (add any missing 01/02 code, don't touch existing rows).
        var masters = await context.employeetypes.ToListAsync(ct);
        var posEmpTypes = await context.Pos_EmployeeTypes.Where(t => t.CompanyId == companyId).ToListAsync(ct);
        var aligned = 0;
        foreach (var m in masters)
        {
            if (posEmpTypes.Any(p => p.Code == m.code)) continue;
            var row = new Pos_EmployeeType { CompanyId = companyId, Code = m.code, Name = m.name, IsActive = true };
            context.Pos_EmployeeTypes.Add(row);
            posEmpTypes.Add(row);
            aligned++;
        }
        if (aligned > 0) await context.SaveChangesAsync(ct);
        var empTypeIdByCode = posEmpTypes.Where(p => p.Code != null)
            .GroupBy(p => p.Code!).ToDictionary(g => g.Key, g => g.First().Id);

        // 2) Position lookup — POS_CODE → the employee's existing Pos_ExecType.
        var execByCode = await context.Pos_ExecTypes
            .Where(t => t.CompanyId == companyId)
            .ToDictionaryAsync(t => t.Code, t => t, ct);

        // 3) Active employees in this company not yet occupying an active slot.
        var occupied = (await context.Pos_PositionSlots
            .Where(s => s.CompanyId == companyId && s.IsActive && s.HremployeeId != null)
            .Select(s => s.HremployeeId!.Value).ToListAsync(ct)).ToHashSet();

        var employees = await context.Hremployee
            .Where(e => e.companyid == companyId && e.ResignDate == null
                        && e.OrganizationId != null && e.PosCode != null)
            .ToListAsync(ct);

        int created = 0, skipped = 0, noPos = 0;
        foreach (var e in employees)
        {
            if (occupied.Contains(e.id)) { skipped++; continue; }
            if (!execByCode.TryGetValue(e.PosCode!, out var exec)) { noPos++; continue; }

            long? empTypeId = e.EmptypeCode != null && empTypeIdByCode.TryGetValue(e.EmptypeCode, out var etid)
                ? etid : (long?)null;

            context.Pos_PositionSlots.Add(new Pos_PositionSlot
            {
                CompanyId = companyId,
                PosCode = e.EmpNo,                 // human-facing running number (legacy convention)
                PosExecTypeId = exec.Id,
                OrganizationId = e.OrganizationId,
                EmployeeTypeId = empTypeId,
                Name = exec.Name,
                HremployeeId = e.id,
                EmpNo = e.EmpNo,
                IsActive = true,
                IsManpower = true,
                IsBoss = exec.IsBoss,
                CreateDate = DateTime.Now,
                CreateBy = $"EmployeeSlotBackfill(user {actorUserId})",
            });
            created++;
        }
        if (created > 0) await context.SaveChangesAsync(ct);

        return new BackfillResult(created, skipped, aligned, noPos);
    }
}
