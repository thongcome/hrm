using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// sc_user ↔ พนักงาน: hremployee_id คือความจริง · empid (EMP_NO) เป็นสำเนา (sc_user.Employee.cs)
//
// ผู้เขียนทุกตัวเดิม (สร้างบัญชีจากหน้าพนักงาน, ตัวนำเข้า Excel, หน้าจัดการผู้ใช้, seeder) ตั้งแต่ empid —
// hook นี้แปลงเป็น id ให้ในทุก SaveChanges แทนที่จะไล่แก้ผู้เขียนทีละตัว (แบบเดียวกับผังองค์กร HRMContext.OrgParent.cs):
//   · ตั้ง/แก้ hremployee_id → empid = EMP_NO ของพนักงานคนนั้น · บัญชีที่ยังไม่มีบริษัทรับบริษัทของพนักงาน
//   · ตั้ง/แก้แต่ empid     → หาพนักงานรหัสนั้นในบริษัทของบัญชี ต้องเจอคนเดียว ไม่งั้นปฏิเสธ
//                             (เดิมพิมพ์รหัสผิดก็บันทึกได้ แล้วคนนั้นเปิด ESS ไม่ได้โดยไม่มีอะไรบอก)
//   · sc_user_role.empid   → สำเนาของ empid ของผู้ใช้ (ไม่มีใครอ่าน เก็บไว้ตามตารางเดิม) ตามให้ตรงเสมอ
public partial class HRMContext
{
    private void SyncUserEmployees()
    {
        var users = ChangeTracker.Entries<sc_user>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();
        foreach (var entry in users)
        {
            var user = entry.Entity;
            var added = entry.State == EntityState.Added;
            var idChanged = added ? user.hremployee_id is not null : entry.Property(u => u.hremployee_id).IsModified;
            var codeChanged = added ? !string.IsNullOrWhiteSpace(user.empid) : entry.Property(u => u.empid).IsModified;
            if (!idChanged && !codeChanged) continue;

            if (idChanged)
            {
                if (user.hremployee_id is long hid)
                {
                    var emp = Hremployee.AsNoTracking().Where(e => e.id == hid).Select(e => new { e.EmpNo, e.companyid }).FirstOrDefault()
                        ?? throw new InvalidOperationException($"ไม่พบพนักงาน id {hid} ที่ผูกกับบัญชี {user.loginname}");
                    user.empid = emp.EmpNo;
                    if (user.company_id <= 0 && emp.companyid is not null)
                        user.company_id = com_companies.AsNoTracking().Where(c => c.code == emp.companyid).Select(c => c.id).FirstOrDefault();
                }
                else user.empid = null;
            }
            else if (string.IsNullOrWhiteSpace(user.empid))
            {
                user.empid = null;
                user.hremployee_id = null;
            }
            else
            {
                var empNo = user.empid.Trim();
                var companyCode = user.company_id > 0
                    ? com_companies.AsNoTracking().Where(c => c.id == user.company_id).Select(c => c.code).FirstOrDefault()
                    : null;
                var matches = Hremployee.AsNoTracking()
                    .Where(e => e.EmpNo == empNo && (companyCode == null || e.companyid == companyCode))
                    .Select(e => new { e.id, e.companyid }).Take(2).ToList();
                if (matches.Count != 1)
                    throw new InvalidOperationException(matches.Count == 0
                        ? $"ไม่พบพนักงานรหัส \"{empNo}\" ในบริษัทของบัญชี {user.loginname} — ผูกบัญชีกับพนักงานไม่ได้"
                        : $"รหัสพนักงาน \"{empNo}\" ซ้ำหลายบริษัท และบัญชี {user.loginname} ไม่ได้ระบุบริษัท");
                user.empid = empNo;
                user.hremployee_id = matches[0].id;
                if (user.company_id <= 0 && matches[0].companyid is not null)
                    user.company_id = com_companies.AsNoTracking().Where(c => c.code == matches[0].companyid).Select(c => c.id).FirstOrDefault();
            }

            // สำเนาบน sc_user_role ของผู้ใช้คนนี้ตามไป
            if (!added)
            {
                foreach (var role in sc_user_roles.Where(r => r.userid == user.userid && r.empid != user.empid).ToList())
                    role.empid = user.empid;
            }
        }

        foreach (var entry in ChangeTracker.Entries<sc_user_role>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList())
        {
            var role = entry.Entity;
            // ผู้ใช้ใหม่ที่เพิ่มในชุดเดียวกัน (ยังไม่มี userid) ผูกผ่าน navigation
            var owner = entry.Reference(r => r.user).CurrentValue
                        ?? (role.userid > 0
                            ? sc_users.Local.FirstOrDefault(u => u.userid == role.userid) ?? sc_users.AsNoTracking().FirstOrDefault(u => u.userid == role.userid)
                            : null);
            if (owner is not null && role.empid != owner.empid) role.empid = owner.empid;
        }
    }
}
