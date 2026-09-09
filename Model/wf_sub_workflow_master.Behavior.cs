using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// ============================================================================
//  wf_sub_workflow_master — "ขั้นหนึ่งขั้น" ตอบคำถามของตัวเอง
//
//  CEO, 9 ก.ย. 2569: "class มี class เดียว คือ wf_subworkflowmaster"
//  ตรงกับต้นฉบับ JSP: SubWorkFlowMaster.getUser() — ตัว config เองเป็นคนตอบว่า
//  ขั้นนี้ใครเกี่ยวข้อง ไม่ใช่มีคลาสอื่นมาอ่าน flag ของมันแทน
//
//  การเพิ่มขั้นใน workflow = เพิ่มแถว wlevel อีกหนึ่งแถว แล้วปล่อยให้งานวิ่งจน
//  เจอ istop ไม่ต้องเพิ่มคลาส ไม่ต้องแก้โค้ดใด ๆ — เส้นทางเป็นข้อมูล ไม่ใช่โค้ด
// ============================================================================
// ผลของการหาคนหนึ่งทาง — บอกด้วยว่ามาจาก config ฟิลด์ไหน เพื่อให้หน้าจอ
// "ทดลองยื่น" อธิบายได้ว่าทำไมคนนี้ถึงโผล่มา ไม่ใช่รู้แค่ว่าโผล่มา
public record ApproverSource(string Field, string ResolverKind, List<long> UserIds, string? Note = null);

public partial class wf_sub_workflow_master
{
    // ขั้นนี้ใครเกี่ยวข้อง — อ่าน config ของตัวเอง ทุกทางบวกกัน ไม่ใช่เลือกทางเดียว
    public async Task<List<sc_user>> GetUserAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        var sources = await GetUserBySourceAsync(db, job, ct);
        var ids = sources.SelectMany(s => s.UserIds).Distinct().ToList();
        if (ids.Count == 0) return new();

        return await db.sc_users
            .Where(u => ids.Contains(u.userid) && u.isdisable != true)
            .ToListAsync(ct);
    }

    // ทางเดียวกัน แต่แยกให้เห็นว่าใครมาจาก config ตัวไหน
    // ส่ง db กับ job เข้ามาให้มันคิดและตัดสินใจเอง
    public async Task<List<ApproverSource>> GetUserBySourceAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        var found = new List<ApproverSource>();

        // USER — ระบุชื่อคนไว้ตรง ๆ จะเอาใครก็ได้ในขั้นนี้
        if (iscustomUser)
        {
            var q = db.wf_custom_users.Where(c => c.workflowid == workflowid && c.wlevel == wlevel && c.isactive);
            if (job.loaid is long lu) q = q.Where(c => c.loaid == lu);
            found.Add(new("iscustomUser", "USER", await q.Select(c => c.userid).ToListAsync(ct)));
        }

        // USER — ระบุ userid ตรง ๆ บนตัวขั้นเอง
        var direct = new[] { userid1, userid2, userid3 }.Where(u => u is not null).Select(u => u!.Value).ToList();
        if (direct.Count > 0)
            found.Add(new("userid1/2/3", "USER", direct));

        // USER — ผู้อนุมัติเฉพาะงานนี้ ที่ถูกดึงเข้ามาสด ๆ ตอนงานวิ่ง
        // (CEO: ผู้อนุมัติเรียกคนมาเพิ่มได้ ณ ตอนกำลังอนุมัติ ใช้ตอนประมูลที่ต้องเรียกด่วน)
        // อ่านด้วย jobmasterid ไม่ใช่ workflowid เพราะเป็นของเฉพาะงานนั้น
        if (isAdhocUser && job.jobmasterid > 0)
        {
            var adhoc = await db.wf_adhoc_users
                .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == wlevel && a.isactive == true)
                .OrderBy(a => a.orderTh)
                .Select(a => a.userid).ToListAsync(ct);
            if (adhoc.Count > 0)
                found.Add(new("isAdhocUser", "USER", adhoc));
        }

        // ROLE — ทุกคนที่มี role ที่ระบุไว้ (เช่นส่งให้ฝ่าย HR ทั้งฝ่าย)
        if (iscustomRole)
        {
            var q = db.wf_custom_roles.Where(c => c.workflowid == workflowid && c.wlevel == wlevel && c.isactive != false);
            if (job.loaid is long lr) q = q.Where(c => c.loaid == lr);
            var roleIds = await q.Select(c => c.roleid).ToListAsync(ct);
            var users = roleIds.Count == 0 ? new List<long>() : await db.sc_user_roles
                .Where(ur => roleIds.Contains(ur.roleid) && ur.isactive)
                .Select(ur => ur.userid).ToListAsync(ct);
            found.Add(new("iscustomRole", "ROLE", users, $"{roleIds.Count} role"));
        }

        // LOA — Level of Authority: ใครมีสิทธิ์อนุมัติ "ตามวงเงิน" ของงานนี้
        if (isLOA)
        {
            var (users, note) = await ResolveLoaAsync(db, job, ct);
            found.Add(new("isLOA", "LOA", users, note));
        }

        // ORG — หัวหน้าตามผังองค์กร ไต่ขึ้นจนเจอคนที่ไม่ใช่ผู้ขอเอง
        if (isupperrole || isupperuser)
        {
            var (boss, note) = await ResolveOrgChainAsync(db, job, ct);
            found.Add(new(isupperrole ? "isupperrole" : "isupperuser", "ORG",
                boss is long b ? new List<long> { b } : new List<long>(), note));
        }

        // ORG — หัวหน้าตัวจริงที่มีอำนาจทางการเงินของ cost center นั้น
        // (CEO: ตรวจว่าเป็นหัวหน้าจริง ๆ ที่มีผลเรื่องเงิน) หา com_organization
        // ที่ CostCenterCode ตรงกับของงาน แล้วเอา approver_empid ของหน่วยงานนั้น
        if (isApproverSameCostCenter && !string.IsNullOrWhiteSpace(job.costcenter))
        {
            var org = await db.com_organizations
                .FirstOrDefaultAsync(o => o.CostCenterCode == job.costcenter && o.isActive != false, ct);
            var uid = org?.approver_empid is null ? null : await db.sc_users
                .Where(u => u.empid == org.approver_empid && u.isdisable != true)
                .Select(u => (long?)u.userid).FirstOrDefaultAsync(ct);
            found.Add(new("isApproverSameCostCenter", "ORG",
                uid is long cc ? new List<long> { cc } : new List<long>(),
                org is null ? $"ไม่พบหน่วยงานที่ cost center {job.costcenter}" : $"หน่วยงาน {org.code}"));
        }

        // หมายเหตุ: isReturnSender ไม่อยู่ที่นี่ เพราะไม่ใช่ตัวหาผู้อนุมัติ
        // แต่เป็นสิทธิของผู้อนุมัติว่าส่งกลับได้ไกลแค่ไหน (ดู ReturnToSenderMove)
        return found;
    }

    // LOA: จำนวนเงินของงานตกอยู่ในแถบไหน แถบนั้นใครมีอำนาจ
    private async Task<(List<long> Users, string? Note)> ResolveLoaAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        var amount = job.reqamont;
        if (amount is null) return (new(), "งานนี้ไม่ได้ระบุจำนวนเงิน");

        var bands = await db.wf_loas
            .Where(l => l.wfid == workflowid && l.nowWorkflowid == workflowid
                     && l.nowLevel == wlevel && l.isactive != false)
            .ToListAsync(ct);

        var band = bands
            .Where(l => amount >= (l.min ?? decimal.MinValue) && amount <= (l.max ?? decimal.MaxValue))
            .Where(l => string.IsNullOrEmpty(l.orgcode) || l.orgcode == job.reqOrg)
            .OrderByDescending(l => !string.IsNullOrEmpty(l.orgcode))   // แถบเฉพาะหน่วยงานชนะแถบทั่วไป
            .FirstOrDefault();
        if (band is null) return (new(), $"ไม่พบแถบวงเงินที่ครอบ {amount:N2}");

        job.loaid = band.loaid;   // งานพกวงเงินที่ตรงกันติดตัวไป ขั้นถัดไปกรองตามนี้ได้

        var users = await db.wf_loa_users
            .Where(u => u.loaid == band.id && u.isactive)
            .Select(u => u.userid).ToListAsync(ct);
        return (users, $"วงเงิน {band.min:N0}–{band.max:N0}");
    }

    // ผังองค์กร: หน่วยงานของผู้ขอ -> approver_empid ยังไม่ตั้งก็ไต่ parent_code ขึ้นไป
    // ไต่ข้ามผู้ขอเองด้วย — ถ้าผู้ขอเป็นหัวหน้าหน่วยงานตัวเอง ต้องให้หัวหน้าเขาอนุมัติ
    // (ไม่ใช่กฎแยกที่ต้อง config — เป็นนิยามของการไต่ผังอยู่แล้ว)
    private static async Task<(long? UserId, string? Note)> ResolveOrgChainAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(job.reqOrg)) return (null, "ผู้ขอไม่มีหน่วยงาน (reqOrg ว่าง)");

        var org = await db.com_organizations.FirstOrDefaultAsync(o => o.code == job.reqOrg, ct);
        var skipped = 0;
        for (var hop = 0; org is not null && hop < 20; hop++)   // 20 = กันผังที่วนกลับมาหาตัวเอง
        {
            if (!string.IsNullOrWhiteSpace(org.approver_empid))
            {
                var u = await db.sc_users
                    .Where(x => x.empid == org.approver_empid && x.isdisable != true)
                    .Select(x => new { x.userid }).FirstOrDefaultAsync(ct);
                if (u is not null && u.userid != job.createuserid)
                    return (u.userid, $"{org.code}" + (skipped > 0 ? $" (ไต่ขึ้น {skipped} ชั้น)" : ""));
                if (u is not null) skipped++;   // เจอผู้ขอเอง — ไต่ขึ้นต่อ
            }
            if (string.IsNullOrWhiteSpace(org.parent_code)) break;
            org = await db.com_organizations.FirstOrDefaultAsync(o => o.code == org.parent_code, ct);
        }
        return (null, "ไต่จนสุดผังแล้วไม่พบผู้อนุมัติ");
    }
}
