using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// HREMPLOYEE → ระดับตำแหน่ง / ประเภทพนักงาน: id คือความจริง · POS_CODE / EMPTYPE_CODE เป็นสำเนา (Hremployee.Types.cs)
//
// ผู้เขียนเดิม (ฟอร์มพนักงาน, ตัวนำเข้า Excel, seeder) ตั้งแต่รหัส — hook นี้แปลงเป็น id ในบริษัทของพนักงานในทุก SaveChanges
// แบบเดียวกับ HRMContext.OrgParent.cs / UserEmployee.cs:
//   · ตั้ง/แก้ id    → รหัสตาม
//   · ตั้ง/แก้แต่รหัส → หาในบริษัทของพนักงาน ไม่เจอ = ปฏิเสธ (เดิมพิมพ์ผิดก็บันทึกได้ แล้วคนนั้นไม่เข้าเงื่อนไข
//                       ประเภทที่ไม่รับเงินเดือน / ค่าจ้างรายวัน / บทบาทตามตำแหน่ง โดยไม่มีอะไรเตือน)
public partial class HRMContext
{
    private void SyncEmployeeTypes()
    {
        var entries = ChangeTracker.Entries<Hremployee>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();
        foreach (var entry in entries)
        {
            var emp = entry.Entity;
            var added = entry.State == EntityState.Added;

            // ระดับตำแหน่ง
            var posIdChanged = added ? emp.PosExecTypeId is not null : entry.Property(e => e.PosExecTypeId).IsModified;
            var posCodeChanged = added ? !string.IsNullOrWhiteSpace(emp.PosCode) : entry.Property(e => e.PosCode).IsModified;
            if (posIdChanged)
            {
                emp.PosCode = emp.PosExecTypeId is long pid
                    ? Pos_ExecTypes.AsNoTracking().Where(p => p.Id == pid).Select(p => p.Code).FirstOrDefault()
                      ?? throw new InvalidOperationException($"ไม่พบระดับตำแหน่ง id {pid} (พนักงาน {emp.EmpNo})")
                    : null;
            }
            else if (posCodeChanged)
            {
                if (string.IsNullOrWhiteSpace(emp.PosCode)) { emp.PosCode = null; emp.PosExecTypeId = null; }
                else
                {
                    var code = emp.PosCode.Trim();
                    emp.PosExecTypeId = Pos_ExecTypes.AsNoTracking().Where(p => p.CompanyId == emp.companyid && p.Code == code)
                        .OrderByDescending(p => p.IsActive).Select(p => (long?)p.Id).FirstOrDefault()
                        ?? throw new InvalidOperationException($"ไม่พบระดับตำแหน่งรหัส \"{code}\" ในบริษัท {emp.companyid} (พนักงาน {emp.EmpNo})");
                    emp.PosCode = code;
                }
            }

            // ประเภทพนักงาน
            var typeIdChanged = added ? emp.EmployeeTypeId is not null : entry.Property(e => e.EmployeeTypeId).IsModified;
            var typeCodeChanged = added ? !string.IsNullOrWhiteSpace(emp.EmptypeCode) : entry.Property(e => e.EmptypeCode).IsModified;
            if (typeIdChanged)
            {
                emp.EmptypeCode = emp.EmployeeTypeId is long tid
                    ? Pos_EmployeeTypes.AsNoTracking().Where(t => t.Id == tid).Select(t => t.Code).FirstOrDefault()
                      ?? throw new InvalidOperationException($"ไม่พบประเภทพนักงาน id {tid} (พนักงาน {emp.EmpNo})")
                    : null;
            }
            else if (typeCodeChanged)
            {
                if (string.IsNullOrWhiteSpace(emp.EmptypeCode)) { emp.EmptypeCode = null; emp.EmployeeTypeId = null; }
                else
                {
                    var code = emp.EmptypeCode.Trim();
                    emp.EmployeeTypeId = Pos_EmployeeTypes.AsNoTracking().Where(t => t.CompanyId == emp.companyid && t.Code == code)
                        .Select(t => (long?)t.Id).FirstOrDefault()
                        ?? throw new InvalidOperationException($"ไม่พบประเภทพนักงานรหัส \"{code}\" ในบริษัท {emp.companyid} (พนักงาน {emp.EmpNo})");
                    emp.EmptypeCode = code;
                }
            }
        }
    }
}
