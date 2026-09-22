using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// ผังองค์กรต่อกันด้วย parentID (id) — parent_code เป็นสำเนาไว้ให้คนอ่าน/นำเข้า (CEO, 21 ก.ย. 2569:
// "ปกติก็ควรใช้ id … ยังไม่มี production จริง เปลี่ยนเป็น id เลย")
//
// เหตุผล: id ไม่เปลี่ยนตลอดอายุแถว ใส่ FK ได้ ไม่กำกวมข้ามบริษัท — รหัสเป็นข้อมูลธุรกิจที่เปลี่ยนได้ (re-code ตอนปรับโครงสร้าง)
// เดิมสองคอลัมน์ไม่มีอะไรบังคับให้ตรงกัน: ตัว seed/ตัวนำเข้า/คำขอเปลี่ยนผังเขียนแต่ parent_code จน parentID ว่าง 162/173 แถว
// และโค้ดแต่ละที่เลือกอ่านคนละคอลัมน์ (bug เวลาเข้างานต่อสาขา 21 ก.ย. 2569)
//
// hook นี้ทำให้ "ทุกทางที่บันทึก com_organization" จบที่ข้อมูลชุดเดียวกัน โดยไม่ต้องไล่แก้ผู้เขียนทีละตัว:
//   · ตั้ง/แก้ parentID            → parent_code = รหัสของแม่ (null เมื่อไม่มีแม่)
//   · ตั้ง/แก้แต่ parent_code      → แปลงเป็น parentID (ในบริษัทเดียวกัน) — หาแม่ไม่เจอ = ปฏิเสธ ไม่ปล่อยให้เป็นกำพร้าเงียบ ๆ
//   · เปลี่ยน code ของหน่วยงาน     → parent_code ของลูกทุกตัวตามไปเอง
//   · ห้ามเป็นแม่ของตัวเอง / ห้ามวน
public partial class HRMContext
{
    // คืนรายการ (ลูก, แม่) ที่แม่ยังไม่มี id เพราะถูกเพิ่มในชุดเดียวกัน — ผู้เรียกตั้ง parentID ให้หลังบันทึกรอบแรก
    private List<(com_organization Child, com_organization Parent)> SyncOrganizationParents()
    {
        var deferred = new List<(com_organization, com_organization)>();
        var entries = ChangeTracker.Entries<com_organization>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();
        if (entries.Count == 0) return deferred;

        foreach (var entry in entries)
        {
            var org = entry.Entity;
            var idChanged = entry.State == EntityState.Added ? org.parentID is not null : entry.Property(o => o.parentID).IsModified;
            var codeRefChanged = entry.State == EntityState.Added ? !string.IsNullOrWhiteSpace(org.parent_code) : entry.Property(o => o.parent_code).IsModified;

            if (idChanged)
            {
                // id คือความจริง — รหัสแม่ตามมา
                var byId = org.parentID is long pid ? FindOrganization(pid) : null;
                org.parent_code = byId?.code;
                if (byId is not null) org.companyid ??= byId.companyid;
            }
            else if (codeRefChanged)
            {
                if (string.IsNullOrWhiteSpace(org.parent_code)) org.parentID = null;
                else
                {
                    var parent = FindOrganizationByCode(org.parent_code!, org.companyid, org.id)
                        ?? throw new InvalidOperationException($"ไม่พบหน่วยงานแม่รหัส \"{org.parent_code}\" ในบริษัทเดียวกัน — บันทึกผังไม่ได้");
                    org.parent_code = parent.code;
                    org.companyid ??= parent.companyid;   // หน่วยงานใหม่ที่ผู้เขียนลืมใส่บริษัท (เจอจริงใน OrgChangeRequestService) — อยู่บริษัทเดียวกับแม่
                    if (parent.id > 0) org.parentID = parent.id;
                    else deferred.Add((org, parent));     // แม่ถูกเพิ่มในชุดเดียวกัน ยังไม่มี id
                }
            }

            if (org.parentID is long p)
            {
                if (entry.State != EntityState.Added && p == org.id)
                    throw new InvalidOperationException("หน่วยงานเป็นแม่ของตัวเองไม่ได้");
                if (entry.State != EntityState.Added && IsDescendant(p, org.id))
                    throw new InvalidOperationException("ย้ายหน่วยงานไปอยู่ใต้หน่วยงานลูกของตัวเองไม่ได้ (ผังจะวน)");
            }

            // หัวหน้า / ผู้อนุมัติ: id ของพนักงานคือความจริง · EMP_NO เป็นสำเนา (com_organization.Heads.cs)
            SyncHead(entry, org, isApprover: true);
            SyncHead(entry, org, isApprover: false);

            // เปลี่ยนรหัสของหน่วยงาน → สำเนารหัสแม่บนลูกทุกตัวตามไป
            if (entry.State == EntityState.Modified && entry.Property(o => o.code).IsModified)
            {
                foreach (var child in com_organizations.Where(c => c.parentID == org.id).ToList())
                    child.parent_code = org.code;
            }
        }
        return deferred;
    }

    // ตั้ง id → EMP_NO ตามไป · ตั้งแต่ EMP_NO (ผู้เขียนเดิมทุกตัว: ตัวนำเข้า, ตั้งหัวหน้า, มอบอำนาจ, คำขอเปลี่ยนผัง) → แปลงเป็น id
    // ของพนักงานในบริษัทเดียวกับหน่วยงาน หาไม่เจอ = ปฏิเสธ ไม่ปล่อยให้ผังชี้ไปหาคนที่ไม่มีอยู่เงียบ ๆ
    private void SyncHead(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<com_organization> entry, com_organization org, bool isApprover)
    {
        var added = entry.State == EntityState.Added;
        var idProp = isApprover ? nameof(com_organization.approver_hremployee_id) : nameof(com_organization.boss_hremployee_id);
        var codeProp = isApprover ? nameof(com_organization.approver_empid) : nameof(com_organization.boss_emp_id);
        var id = isApprover ? org.approver_hremployee_id : org.boss_hremployee_id;
        var code = isApprover ? org.approver_empid : org.boss_emp_id;

        var idChanged = added ? id is not null : entry.Property(idProp).IsModified;
        var codeChanged = added ? !string.IsNullOrWhiteSpace(code) : entry.Property(codeProp).IsModified;
        if (!idChanged && !codeChanged) return;

        if (idChanged)
        {
            code = id is long hid ? Hremployee.AsNoTracking().Where(e => e.id == hid).Select(e => e.EmpNo).FirstOrDefault() : null;
            if (id is not null && code is null) throw new InvalidOperationException($"ไม่พบพนักงาน id {id} ที่ตั้งเป็นหัวหน้าหน่วยงาน {org.code}");
        }
        else if (string.IsNullOrWhiteSpace(code)) { id = null; code = null; }
        else
        {
            var empNo = code.Trim();
            var companyCode = org.companyid is long cid ? com_companies.AsNoTracking().Where(c => c.id == cid).Select(c => c.code).FirstOrDefault() : null;
            var matches = Hremployee.AsNoTracking()
                .Where(e => e.EmpNo == empNo && (companyCode == null || e.companyid == companyCode))
                .Select(e => e.id).Take(2).ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException(matches.Count == 0
                    ? $"ไม่พบพนักงานรหัส \"{empNo}\" ในบริษัทของหน่วยงาน {org.code} — ตั้งเป็นหัวหน้า/ผู้อนุมัติไม่ได้"
                    : $"รหัสพนักงาน \"{empNo}\" ซ้ำหลายบริษัท และหน่วยงาน {org.code} ไม่ได้ระบุบริษัท");
            id = matches[0];
            code = empNo;
        }

        if (isApprover) { org.approver_hremployee_id = id; org.approver_empid = code; }
        else { org.boss_hremployee_id = id; org.boss_emp_id = code; }
    }

    private com_organization? FindOrganization(long id)
        => com_organizations.Local.FirstOrDefault(o => o.id == id) ?? com_organizations.AsNoTracking().FirstOrDefault(o => o.id == id);

    private com_organization? FindOrganizationByCode(string code, long? companyId, long selfId)
    {
        var trimmed = code.Trim();
        // บริษัทเดียวกันก่อน · หน่วยงานที่ยังไม่รู้บริษัท (companyid ว่าง) ยอมรับแม่จากรหัสอย่างเดียว แล้วรับบริษัทของแม่มา
        bool Match(com_organization o) => !ReferenceEquals(o, null) && o.code == trimmed && (o.companyid == companyId || companyId is null);
        return com_organizations.Local.Where(o => (selfId == 0 || o.id != selfId) && Match(o)).OrderBy(o => o.id).FirstOrDefault()
            ?? com_organizations.AsNoTracking().Where(o => o.id != selfId && o.code == trimmed && (o.companyid == companyId || companyId == null)).OrderBy(o => o.id).FirstOrDefault();
    }

    // candidateParent อยู่ใต้ nodeId หรือไม่ (ไต่ขึ้นจาก candidateParent)
    private bool IsDescendant(long candidateParent, long nodeId)
    {
        var current = (long?)candidateParent;
        for (var guard = 0; current is long id && guard < 100; guard++)
        {
            if (id == nodeId) return true;
            current = FindOrganization(id)?.parentID;
        }
        return false;
    }
}
