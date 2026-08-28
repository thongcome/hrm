using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// สมาชิกคณะกรรมการ/อนุกรรมการ ต่อพนักงาน — ผูกกับ Hrd_Committee. Multiple active rows per
// employee are allowed (one person can sit on several committees at once); old memberships
// are kept (IsActive=false, EndDate set) rather than deleted, same soft-delete convention
// as the rest of Hrd_*. The Employee-module "committee" section should query this table
// filtered to IsActive=true for the employee being viewed and render nothing at all when
// empty — the conditional-display rule the user asked for.
[Table("Hrd_CommitteeMembership")]
public class Hrd_CommitteeMembership
{
    [Key]
    public long Id { get; set; }

    public long CommitteeId { get; set; }

    public long HremployeeId { get; set; }

    public CommitteeRole Role { get; set; } = CommitteeRole.Member;

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Remark { get; set; }

    public DateTime? ModDate { get; set; }
    [StringLength(50)]
    public string? ModBy { get; set; }
}
