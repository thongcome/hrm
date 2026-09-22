namespace HRM.Services.Shared;

using System.Security.Claims;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

// "ฉันเป็นหัวหน้าของใคร" ตัดสินจากผังองค์กร ไม่ใช่จากบทบาท (CEO, 21 ก.ย. 2569):
//   "MSS เช็คเลยว่าใน orgtree มีเขาเป็นหัวหน้าไหม ถ้าเป็น เป็นที่ node ไหน เก็บเข้า session แล้วไปดึงข้อมูลโครงสร้างเขามา"
//
// หัวหน้าของ node = com_organization.approver_empid (รหัสพนักงาน) — ฟิลด์เดียวกับที่ workflow engine ใช้ไต่สายอนุมัติ
// จึงไม่มีทางที่ MSS กับเส้นทางอนุมัติจะเห็นหัวหน้าคนละคน
//   · ตอน login: ScUserClaimsPrincipalFactory เรียก FindHeadedNodeIdsAsync แล้วเก็บเป็น claim "mss_org" (หนึ่ง claim ต่อ node)
//     = "session" ของระบบนี้ (cookie auth) — ย้ายหัวหน้าในผังแล้วมีผลตอน login ครั้งถัดไป เหมือนสิทธิ์เมนู
//   · ตอนเปิดหน้า MSS: ResolveAsync อ่าน node จาก claim แล้วดึงโครงสร้างใต้ node นั้นทั้งสาย (ลูก หลาน …) กับพนักงานในนั้น
public static class ManagerScopeService
{
    public const string HeadedOrgClaim = "mss_org";
    public const string MssMenuCode = "MSS_ACCESS";

    public sealed record OrgNode(long Id, string? Code, string? Name, long? ParentId, bool IsHeadedByMe, int Depth);

    public sealed record Scope(IReadOnlyList<OrgNode> Nodes, IReadOnlyList<Hremployee> Team)
    {
        public static readonly Scope Empty = new(Array.Empty<OrgNode>(), Array.Empty<Hremployee>());
        public bool IsManager => Nodes.Any(n => n.IsHeadedByMe);
        public IEnumerable<OrgNode> HeadedNodes => Nodes.Where(n => n.IsHeadedByMe);
    }

    /// <summary>node ที่พนักงานคนนี้เป็นหัวหน้า (approver_hremployee_id) — เฉพาะหน่วยงานที่ยังใช้งาน ในบริษัทของเขา</summary>
    public static async Task<List<long>> FindHeadedNodeIdsAsync(HRMContext context, string? empNo, string? companyCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(empNo)) return new();
        // รหัสพนักงาน (จาก login) แปลงเป็น id ครั้งเดียวที่ขอบระบบ แล้วเทียบกับผู้อนุมัติของหน่วยงานด้วย id
        var employeeId = await context.Hremployee
            .Where(e => e.EmpNo == empNo && (companyCode == null || e.companyid == companyCode))
            .Select(e => (long?)e.id).FirstOrDefaultAsync(ct);
        if (employeeId is null) return new();
        return await context.com_organizations
            .Where(o => o.isActive && o.approver_hremployee_id == employeeId)
            .Select(o => o.id)
            .ToListAsync(ct);
    }

    public static IReadOnlyList<long> HeadedNodeIds(ClaimsPrincipal user)
        => user.FindAll(HeadedOrgClaim).Select(c => long.TryParse(c.Value, out var id) ? id : 0).Where(id => id > 0).Distinct().ToList();

    /// <summary>
    /// โครงสร้างใต้ node ที่ผู้ใช้เป็นหัวหน้า (ทั้งสาย) + พนักงานที่ยังทำงานอยู่ในโครงสร้างนั้น ไม่รวมตัวเขาเอง
    /// </summary>
    public static async Task<Scope> ResolveAsync(HRMContext context, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var headed = HeadedNodeIds(user).ToHashSet();
        if (headed.Count == 0) return Scope.Empty;

        // ผังของบริษัทเดียวกับ node ที่เป็นหัวหน้า — โหลดครั้งเดียวแล้วไล่ลูกในหน่วยความจำ (ผังองค์กรไม่กี่พันแถว)
        var companyKeys = await context.com_organizations.Where(o => headed.Contains(o.id)).Select(o => o.companyid).Distinct().ToListAsync(ct);
        // ผังต่อกันด้วย parentID (migration 20260921140000_OrgParentById เติมครบทุกแถว + FK ชี้ตัวเอง)
        var all = await context.com_organizations
            .Where(o => o.isActive && companyKeys.Contains(o.companyid))
            .Select(o => new { o.id, o.code, o.name, o.parentID })
            .ToListAsync(ct);
        var childrenOf = all.Where(o => o.parentID != null).GroupBy(o => o.parentID!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var nodes = new List<OrgNode>();
        var seen = new HashSet<long>();
        // เริ่มจาก node ที่เป็นหัวหน้าซึ่งไม่ได้อยู่ใต้ node ที่เป็นหัวหน้าอีกตัว (กันนับซ้ำ) แล้วไล่ลงทั้งสาย
        var byId = all.ToDictionary(o => o.id);
        bool HasHeadedAncestor(long id)
        {
            var guard = 0;
            var current = byId.TryGetValue(id, out var o) ? o.parentID : null;
            while (current is long p && guard++ < 50)
            {
                if (headed.Contains(p)) return true;
                current = byId.TryGetValue(p, out var po) ? po.parentID : null;
            }
            return false;
        }
        void Walk(long id, int depth)
        {
            if (!seen.Add(id) || !byId.TryGetValue(id, out var o) || depth > 50) return;
            nodes.Add(new OrgNode(o.id, o.code, o.name, o.parentID, headed.Contains(o.id), depth));
            if (childrenOf.TryGetValue(id, out var kids))
                foreach (var k in kids.OrderBy(k => k.code)) Walk(k.id, depth + 1);
        }
        foreach (var root in headed.Where(id => byId.ContainsKey(id) && !HasHeadedAncestor(id)).OrderBy(id => byId[id].code))
            Walk(root, 0);

        var orgIds = nodes.Select(n => n.Id).ToList();
        var myEmpNo = user.FindFirst("empno")?.Value;
        var team = await context.Hremployee
            .Where(e => e.ResignDate == null && e.IsActive && e.OrganizationId != null && orgIds.Contains(e.OrganizationId!.Value) && e.EmpNo != myEmpNo)
            .OrderBy(e => e.EmpNo)
            .ToListAsync(ct);
        return new Scope(nodes, team);
    }
}
