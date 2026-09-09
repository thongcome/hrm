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
    //
    // CEO, 10 ก.ย. 2569: "ใน 1 check field ใน wf_subworkflowmaster คุณต้องสร้าง
    // 1 method ไว้ทำงาน" — เมธอดนี้จึงไม่มี logic ของตัวเอง แค่เรียกเมธอดของ
    // แต่ละ field ตามลำดับ แล้วเก็บอันที่ตอบกลับมา
    //
    // เพิ่ม config ใหม่ = เขียนเมธอดของมัน 1 ตัว แล้วต่อท้ายรายการนี้ จบ
    public async Task<List<ApproverSource>> GetUserBySourceAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        var found = new List<ApproverSource>();
        foreach (var field in new Func<HRMContext, job_master, CancellationToken, Task<ApproverSource?>>[]
        {
            IsCustomUserAsync,
            UserId123Async,
            IsAdhocUserAsync,
            IsCustomRoleAsync,
            IsLOAAsync,
            SupervisorChainAsync,
            IsApproverSameCostCenterAsync,
        })
        {
            var one = await field(db, job, ct);
            if (one is not null) found.Add(one);
        }
        // หมายเหตุ: isReturnSender ไม่มีเมธอดที่นี่ เพราะไม่ใช่ตัวหาผู้อนุมัติ
        // แต่เป็นสิทธิของผู้อนุมัติว่าส่งกลับได้ไกลแค่ไหน (ดู ReturnToSenderMove)
        return found;
    }

    // ── หนึ่ง field หนึ่งเมธอด ── คืน null เมื่อ field ของตัวเองไม่ได้ถูกตั้งไว้

    // iscustomUser — ระบุชื่อคนไว้ตรง ๆ จะเอาใครก็ได้ในขั้นนี้
    private async Task<ApproverSource?> IsCustomUserAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        if (!iscustomUser) return null;
        var q = db.wf_custom_users.Where(c => c.workflowid == workflowid && c.wlevel == wlevel && c.isactive);
        if (job.loaid is long loa) q = q.Where(c => c.loaid == loa);
        var ids = await q.Select(c => c.userid).ToListAsync(ct);
        return new("iscustomUser", "USER", ids,
            ids.Count == 0 ? "ติ๊กไว้แต่ยังไม่ได้เลือกใคร" : null);
    }

    // userid1/2/3 — ระบุ userid ตรง ๆ บนตัวขั้นเอง
    private Task<ApproverSource?> UserId123Async(HRMContext db, job_master job, CancellationToken ct)
    {
        var ids = new[] { userid1, userid2, userid3 }.Where(u => u is not null).Select(u => u!.Value).ToList();
        return Task.FromResult<ApproverSource?>(
            ids.Count == 0 ? null : new ApproverSource("userid1/2/3", "USER", ids));
    }

    // isAdhocUser — ผู้อนุมัติที่ถูกดึงเข้ามาสด ๆ ตอนงานวิ่ง (CEO: ผู้อนุมัติเรียก
    // คนมาเพิ่มได้ ณ ตอนกำลังอนุมัติ ใช้ตอนประมูลที่ต้องเรียกด่วน)
    // อ่านด้วย jobmasterid ไม่ใช่ workflowid เพราะเป็นของเฉพาะงานใบนั้น
    private async Task<ApproverSource?> IsAdhocUserAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        if (!isAdhocUser || job.jobmasterid <= 0) return null;
        var ids = await db.wf_adhoc_users
            .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel == wlevel && a.isactive == true)
            .OrderBy(a => a.orderTh)
            .Select(a => a.userid).ToListAsync(ct);
        return new("isAdhocUser", "USER", ids, ids.Count == 0 ? "ยังไม่มีใครถูกเรียกเข้ามา" : null);
    }

    // iscustomRole — ทุกคนที่มี role ที่ระบุไว้ (เช่นส่งให้ฝ่าย HR ทั้งฝ่าย)
    private async Task<ApproverSource?> IsCustomRoleAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        if (!iscustomRole) return null;
        var q = db.wf_custom_roles.Where(c => c.workflowid == workflowid && c.wlevel == wlevel && c.isactive != false);
        if (job.loaid is long loa) q = q.Where(c => c.loaid == loa);
        var roleIds = await q.Select(c => c.roleid).ToListAsync(ct);
        var users = roleIds.Count == 0 ? new List<long>() : await db.sc_user_roles
            .Where(ur => roleIds.Contains(ur.roleid) && ur.isactive)
            .Select(ur => ur.userid).ToListAsync(ct);
        return new("iscustomRole", "ROLE", users,
            roleIds.Count == 0 ? "ติ๊กไว้แต่ยังไม่ได้เลือก role" : $"{roleIds.Count} role");
    }

    // isLOA — Level of Authority: ใครมีสิทธิ์อนุมัติตามวงเงินของงานนี้
    private async Task<ApproverSource?> IsLOAAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        if (!isLOA) return null;
        var (users, note) = await ResolveLoaAsync(db, job, ct);
        return new("isLOA", "LOA", users, note);
    }

    // หัวหน้าตามผังองค์กร — "ผ่านหัวหน้ากี่ชั้น"
    //
    // CEO, 10 ก.ย. 2569: "สมัยก่อนให้วิ่งตาม sc_user ตอนหลังทำ org tree แล้วเลยไม่ใช้"
    // isupperrole/isupperuser เป็นชื่อจากยุคที่ไต่ตาม sc_user.upperuserid /
    // sc_role.upperrole ซึ่งเลิกใช้แล้ว ทั้งคู่จึงหมายถึง "ไต่ผัง 1 ชั้น" เท่ากัน
    // ของใหม่ใช้ isNeedsupervisorapprove (int) ตัวเดียว = ไต่กี่ชั้น อ่านง่ายกว่า
    // และครอบของเดิมได้หมด — 6 ขั้นที่ยังตั้ง flag เก่าไว้จึงทำงานต่อได้ตามปกติ
    private async Task<ApproverSource?> SupervisorChainAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        var climb = isNeedsupervisorapprove ?? 0;
        if (climb <= 0 && !isupperrole && !isupperuser) return null;
        if (climb <= 0) climb = 1;   // flag เก่าไม่ได้บอกจำนวนชั้น ถือว่า 1 ชั้น

        var field = isNeedsupervisorapprove > 0 ? "isNeedsupervisorapprove"
                  : isupperrole ? "isupperrole" : "isupperuser";
        var (boss, note) = await ResolveOrgChainAsync(db, job, climb, ct);
        return new(field, "ORG", boss is long b ? new List<long> { b } : new List<long>(), note);
    }

    // isApproverSameCostCenter — หัวหน้าตัวจริงที่มีอำนาจทางการเงินของ cost center นั้น
    // (CEO: ตรวจว่าเป็นหัวหน้าจริง ๆ ที่มีผลเรื่องเงิน) หา com_organization ที่
    // CostCenterCode ตรงกับของงาน แล้วเอา approver_empid ของหน่วยงานนั้น
    private async Task<ApproverSource?> IsApproverSameCostCenterAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        if (!isApproverSameCostCenter) return null;
        if (string.IsNullOrWhiteSpace(job.costcenter))
            return new("isApproverSameCostCenter", "ORG", new(), "งานนี้ไม่มี cost center");

        var org = await db.com_organizations
            .FirstOrDefaultAsync(o => o.CostCenterCode == job.costcenter && o.isActive != false, ct);
        var uid = org?.approver_empid is null ? null : await db.sc_users
            .Where(u => u.empid == org.approver_empid && u.isdisable != true)
            .Select(u => (long?)u.userid).FirstOrDefaultAsync(ct);
        return new("isApproverSameCostCenter", "ORG",
            uid is long cc ? new List<long> { cc } : new List<long>(),
            org is null ? $"ไม่พบหน่วยงานที่ cost center {job.costcenter}" : $"หน่วยงาน {org.code}");
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
    //   climb = ต้องผ่านหัวหน้ากี่ชั้น (1 = หัวหน้าตรง, 2 = หัวหน้าของหัวหน้า)
    //   ข้ามผู้ขอเองเสมอ — ถ้าผู้ขอเป็นหัวหน้าหน่วยงานตัวเอง ต้องให้หัวหน้าเขาอนุมัติ
    //   ข้ามหน่วยงานที่ยังไม่ตั้งผู้อนุมัติ — ไต่ต่อจนเจอคนจริงหรือสุดผัง
    private static async Task<(long? UserId, string? Note)> ResolveOrgChainAsync(
        HRMContext db, job_master job, int climb, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(job.reqOrg)) return (null, "ผู้ขอไม่มีหน่วยงาน (reqOrg ว่าง)");

        var org = await db.com_organizations.FirstOrDefaultAsync(o => o.code == job.reqOrg, ct);
        var levelsFound = 0;
        for (var hop = 0; org is not null && hop < 20; hop++)   // 20 = กันผังที่วนกลับมาหาตัวเอง
        {
            if (!string.IsNullOrWhiteSpace(org.approver_empid))
            {
                var u = await db.sc_users
                    .Where(x => x.empid == org.approver_empid && x.isdisable != true)
                    .Select(x => new { x.userid }).FirstOrDefaultAsync(ct);

                // เจอผู้ขอเอง = ยังไม่นับเป็นหัวหน้าหนึ่งชั้น ต้องไต่ขึ้นต่อ
                if (u is not null && u.userid != job.createuserid)
                {
                    levelsFound++;
                    if (levelsFound >= climb)
                        return (u.userid, $"{org.code}" + (climb > 1 ? $" (หัวหน้าชั้นที่ {climb})" : ""));
                }
            }
            if (string.IsNullOrWhiteSpace(org.parent_code))
                return (null, levelsFound == 0
                    ? "ไต่จนสุดผังแล้วไม่พบผู้อนุมัติ"
                    : $"ผังมีหัวหน้าแค่ {levelsFound} ชั้น แต่ตั้งไว้ {climb} ชั้น");
            org = await db.com_organizations.FirstOrDefaultAsync(o => o.code == org.parent_code, ct);
        }
        return (null, "ไต่จนสุดผังแล้วไม่พบผู้อนุมัติ");
    }
}
