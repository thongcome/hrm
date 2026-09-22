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
            IsAdhocUserAsync,
            IsCustomRoleAsync,
            IsLOAAsync,
            SupervisorChainAsync,
            IsApproverSameCostCenterAsync,
            IsApproverSameOrgAsync,
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
        // AD.Workflow ข้อ 15: LOA เลือกได้แค่ "ผู้อนุมัติ" — แถวที่ไม่ผูกวงเงิน (loaid ว่าง) ใช้ได้เสมอ
        if (job.loaid is long loa) q = q.Where(c => c.loaid == loa || c.loaid == null);
        var ids = await q.Select(c => c.userid).ToListAsync(ct);
        return new("iscustomUser", "USER", ids,
            ids.Count == 0 ? "ติ๊กไว้แต่ยังไม่ได้เลือกใคร" : null);
    }

    // userid1/2/3 เลิกใช้ (AD.Workflow ข้อ 17, CEO 20 ก.ย. 2569) — ใช้ผังองค์กรแทน จึงไม่มีเมธอดอ่านมันอีก

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
        if (job.loaid is long loa) q = q.Where(c => c.loaid == loa || c.loaid == null);
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
    // ของใหม่ใช้ isNeedsupervisorapprove (bool) + verticalMaxLevel = ไต่กี่ชั้น
    // และครอบของเดิมได้หมด — ขั้นที่ยังตั้ง flag เก่าไว้จึงทำงานต่อได้ตามปกติ (ชั้นเดียว)
    private Task<ApproverSource?> SupervisorChainAsync(HRMContext db, job_master job, CancellationToken ct)
        => SupervisorAtHopAsync(db, job, 1, ct);   // มาถึงระดับนี้ครั้งแรก = หัวหน้าชั้นที่ 1

    // หัวหน้าชั้นที่ N ของผู้ขอ — ใช้ทั้งตอนมาถึงระดับ (ชั้น 1) และตอนไต่ต่อทีละชั้น
    //
    // CEO, 10 ก.ย. 2569: "ถ้ามี isNeedsupervisorapprove ผลที่เกิดคือ jobsequence
    // ต้อง +1 ด้วย แต่ level ยังไม่เดิน แล้วต้อง stamp ทุกครั้งที่มี workflow action"
    // -> หัวหน้าแต่ละชั้นเซ็นแล้วงานอยู่ระดับเดิม นับก้าวเพิ่ม ประทับรอยเท้าทุกชั้น
    //    จนครบจำนวนชั้นที่ตั้งไว้ ถึงจะเดินไประดับถัดไป
    public async Task<ApproverSource?> SupervisorAtHopAsync(HRMContext db, job_master job, int hop, CancellationToken ct)
    {
        if (SupervisorLevels <= 0) return null;

        var field = isNeedsupervisorapprove ? "isNeedsupervisorapprove"
                  : isupperrole ? "isupperrole" : "isupperuser";
        var startOrg = await SenderOrgIdAsync(db, job, ct);
        var (boss, note) = await ResolveOrgChainAsync(db, startOrg, hop, ct);
        return new(field, "ORG", boss is long b ? new List<long> { b } : new List<long>(),
            SupervisorLevels > 1 ? $"หัวหน้าชั้นที่ {hop}/{SupervisorLevels}" + (note is null ? "" : $" · {note}") : note);
    }

    // ระดับนี้ต้องผ่านหัวหน้ากี่ชั้น (0 = ไม่ผ่าน) — isNeedsupervisorapprove เปิด = verticalMaxLevel ชั้น
    // (ว่าง = 1) ส่วน flag เก่า isupperrole/isupperuser ถือว่า 1 ชั้น (AD.Workflow ข้อ 13, 14)
    public int SupervisorLevels =>
        isNeedsupervisorapprove ? Math.Max(1, verticalMaxLevel ?? 1)
        : (isupperrole || isupperuser) ? 1 : 0;

    // ตัวช่วยให้หน้าจอตั้งค่าถามคำถามเดียว "ผ่านหัวหน้ากี่ชั้น" แล้วแปลงเป็น 2 ฟิลด์ให้เอง
    // 0 = ปิดทั้งสาย รวมถึง flag เก่าที่เคยเปิดค้างไว้ (ไม่งั้นหน้าจอโชว์ 0 แต่ engine ยังไต่ 1 ชั้น)
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int SupervisorTiers
    {
        get => SupervisorLevels;
        set
        {
            isNeedsupervisorapprove = value > 0;
            verticalMaxLevel = value > 1 ? value : null;
            if (value <= 0) { isupperrole = false; isupperuser = false; }
        }
    }

    // ผู้ใช้ที่เป็นพนักงานคนนี้ (sc_user ยังผูกพนักงานด้วย EMP_NO — งานแปลงถัดไป)
    private static async Task<long?> UserOfEmployeeAsync(HRMContext db, long hremployeeId, CancellationToken ct)
    {
        var empNo = await db.Hremployee.Where(e => e.id == hremployeeId).Select(e => e.EmpNo).FirstOrDefaultAsync(ct);
        return empNo is null ? null : await db.sc_users
            .Where(u => u.empid == empNo && u.isdisable != true)
            .Select(u => (long?)u.userid).FirstOrDefaultAsync(ct);
    }

    // isApproverSameCostCenter — หัวหน้าตัวจริงที่มีอำนาจทางการเงินของ cost center นั้น
    // (CEO: ตรวจว่าเป็นหัวหน้าจริง ๆ ที่มีผลเรื่องเงิน) หา com_organization ที่
    // CostCenterCode ตรงกับของงาน แล้วเอา approver_hremployee_id ของหน่วยงานนั้น
    private async Task<ApproverSource?> IsApproverSameCostCenterAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        if (!isApproverSameCostCenter) return null;
        if (string.IsNullOrWhiteSpace(job.costcenter))
            return new("isApproverSameCostCenter", "ORG", new(), "งานนี้ไม่มี cost center");

        var org = await db.com_organizations
            .FirstOrDefaultAsync(o => o.CostCenterCode == job.costcenter && o.isActive != false, ct);
        var uid = org?.approver_hremployee_id is long aid ? await UserOfEmployeeAsync(db, aid, ct) : null;
        return new("isApproverSameCostCenter", "ORG",
            uid is long cc ? new List<long> { cc } : new List<long>(),
            org is null ? $"ไม่พบหน่วยงานที่ cost center {job.costcenter}" : $"หน่วยงาน {org.code}");
    }
    // isApproverSameOrg — คนในหน่วยงานเดียวกับผู้ขอ (AD.Workflow ข้อ 16: ไม่ตัดผู้ขอออก)
    // ใช้กับขั้นที่ให้คนในหน่วยงานเดียวกันช่วยกันดู ไม่ใช่สายบังคับบัญชา
    private async Task<ApproverSource?> IsApproverSameOrgAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        if (!isApproverSameOrg) return null;
        if (string.IsNullOrWhiteSpace(job.reqOrg))
            return new("isApproverSameOrg", "ORG", new(), "ผู้ขอไม่มีหน่วยงาน");

        var ids = await db.sc_users
            .Where(u => u.orgcode == job.reqOrg && u.isdisable != true)
            .Select(u => u.userid).ToListAsync(ct);
        return new("isApproverSameOrg", "ORG", ids, $"หน่วยงาน {job.reqOrg}");
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

    // หน่วยงาน (id) ตั้งต้นของการไต่ (AD.Workflow ข้อ 14): หน่วยงานของ "คนที่ส่งงานมาถึงขั้นนี้"
    // = แถวอนุมัติล่าสุดของขั้นก่อนหน้า (wlevel มากกว่า 0 และน้อยกว่าขั้นนี้ ที่กดแล้ว)
    // ยังไม่มี (ขั้นแรก) หรือหาหน่วยงานไม่เจอ → ใช้หน่วยงานของผู้ขอ
    // ไต่ชั้นที่ 2 ขึ้นไปก็เริ่มจากที่เดียวกัน เพราะแถวของขั้นนี้เองไม่ถูกนับ (wlevel น้อยกว่า)
    // หาจาก Hremployee.OrganizationId (มี FK) ก่อน — รหัสหน่วยงานที่งานบันทึกไว้ตอนยื่น (job_user_list.orgcode/job.reqOrg)
    // เป็นหลักฐาน ณ วันนั้น ใช้เป็นตัวสำรองเท่านั้น (22 ก.ย. 2569)
    private async Task<long?> SenderOrgIdAsync(HRMContext db, job_master job, CancellationToken ct)
    {
        var sender = await db.job_user_lists
            .Where(a => a.jobmasterid == job.jobmasterid && a.wlevel > 0 && a.wlevel < wlevel && a.approvedate != null)
            .OrderByDescending(a => a.approvedate).ThenByDescending(a => a.jobseq)
            .Select(a => new { a.userid, a.orgcode })
            .FirstOrDefaultAsync(ct);

        var orgCode = sender?.orgcode ?? job.reqOrg;
        if (sender?.userid is long uid)
        {
            var empNo = await db.sc_users.Where(u => u.userid == uid).Select(u => u.empid).FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(empNo))
            {
                var orgId = await db.Hremployee.Where(e => e.EmpNo == empNo && e.OrganizationId != null)
                    .Select(e => e.OrganizationId).FirstOrDefaultAsync(ct);
                if (orgId is not null) return orgId;
            }
            orgCode ??= await db.sc_users.Where(u => u.userid == uid).Select(u => u.orgcode).FirstOrDefaultAsync(ct);
        }
        if (string.IsNullOrWhiteSpace(orgCode)) return null;
        return await db.com_organizations.Where(o => o.code == orgCode)
            .OrderByDescending(o => o.isActive).Select(o => (long?)o.id).FirstOrDefaultAsync(ct);
    }

    // ผังองค์กร: หน่วยงานตั้งต้น -> ผู้อนุมัติ (approver_hremployee_id) ยังไม่ตั้งก็ไต่ parentID ขึ้นไป (id ล้วน 22 ก.ย. 2569)
    //   climb = ต้องผ่านหัวหน้ากี่ชั้น (1 = หัวหน้าตรง, 2 = หัวหน้าของหัวหน้า)
    //   ไม่ข้ามผู้ขอ (AD.Workflow ข้อ 16) — ผู้ขอที่เป็นหัวหน้าหน่วยงานตัวเองคือผู้อนุมัติชั้นนั้นเอง
    //   ข้ามหน่วยงานที่ยังไม่ตั้งผู้อนุมัติ — ไต่ต่อจนเจอคนจริงหรือสุดผัง
    private static async Task<(long? UserId, string? Note)> ResolveOrgChainAsync(
        HRMContext db, long? startOrg, int climb, CancellationToken ct)
    {
        if (startOrg is null) return (null, "ไม่พบหน่วยงานตั้งต้นของการไต่ (ผู้ส่งงาน/ผู้ขอไม่มีหน่วยงาน)");

        var org = await db.com_organizations.FirstOrDefaultAsync(o => o.id == startOrg, ct);
        var levelsFound = 0;
        for (var hop = 0; org is not null && hop < 20; hop++)   // 20 = กันผังที่วนกลับมาหาตัวเอง
        {
            if (org.approver_hremployee_id is long approverId)
            {
                var userId = await UserOfEmployeeAsync(db, approverId, ct);

                // AD.Workflow ข้อ 16 (CEO 20 ก.ย. 2569): ผู้ขอเป็นผู้อนุมัติเองได้ ไม่มีตัวกัน — นับหัวหน้าตามผังตรง ๆ
                if (userId is long found)
                {
                    levelsFound++;
                    if (levelsFound >= climb)
                        return (found, $"{org.code}" + (climb > 1 ? $" (หัวหน้าชั้นที่ {climb})" : ""));
                }
            }
            if (org.parentID is not long parentOrgId)
                return (null, levelsFound == 0
                    ? "ไต่จนสุดผังแล้วไม่พบผู้อนุมัติ"
                    : $"ผังมีหัวหน้าแค่ {levelsFound} ชั้น แต่ตั้งไว้ {climb} ชั้น");
            org = await db.com_organizations.FirstOrDefaultAsync(o => o.id == parentOrgId, ct);
        }
        return (null, "ไต่จนสุดผังแล้วไม่พบผู้อนุมัติ");
    }
}
