using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Master-data mapping: "whoever holds Position posid, within Organization
// orgid, is granted Role roleid for approval-authority purposes." Confirmed
// new — investigated first and found no existing 3-way Role<->Position<->Org
// relationship anywhere (wf_custom_role.poscode/orgcode are dead columns
// never read by WorkflowEngineService; PosRoleAssociate is unused legacy,
// 0 rows, no org column, and its poscode column is a corrupted varbinary).
//
// orgid/posid reference com_organization.id / com_position.id (the numeric
// PKs), not the code string columns — com_organization.code is confirmed
// non-unique in real data (see com_organization.cs), so code-based lookup
// isn't a safe FK target here.
//
// Deliberately NOT wired into WorkflowEngineService's approver resolution
// yet — this is the master-data CRUD only; consuming it in
// ResolveCandidatesAsync is a separate follow-up.
[Table("wf_role_authority")]
public class wf_role_authority
{
    [Key]
    public long id { get; set; }

    public long roleid { get; set; }

    public long orgid { get; set; }

    public long posid { get; set; }

    public bool isactive { get; set; } = true;

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    [StringLength(500)]
    public string? remark { get; set; }

    public DateTime moddate { get; set; } = DateTime.Now;

    [StringLength(250)]
    public string? modby { get; set; }
}
