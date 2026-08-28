using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// คณะกรรมการ/อนุกรรมการ — master ต่อบริษัท (เช่น คณะกรรมการบริษัท, คณะกรรมการสวัสดิการ,
// อนุกรรมการตรวจสอบ). This is a genuine gap ported from nowhere — the legacy JSP system
// had a committee module but only as compiled .class files with source unavailable, so
// this is a fresh design rather than a port like the rest of Hrd_*.
//
// Membership deliberately reuses Hremployee (via Hrd_CommitteeMembership) rather than a
// separate person table — per the user's own instruction: "ข้อมูลกรรมการ ก็ใช้พนักงานได้
// แต่ประเภทเป็น กรรมการ ถ้าในหน้าจอ เขาไม่ได้เป็นกรรมการไม่ต้องแสดง" (committee data can use
// the employee table, just tagged as committee-member type; if someone isn't on a
// committee, don't show the section at all).
[Table("Hrd_Committee")]
public class Hrd_Committee
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? NameEn { get; set; }

    public CommitteeType CommitteeType { get; set; } = CommitteeType.Committee;

    // Soft-linked to another Hrd_Committee.Id — used when this row is an
    // อนุกรรมการ (subcommittee) reporting up to a parent คณะกรรมการ. Null for
    // a top-level committee.
    public long? ParentCommitteeId { get; set; }

    [StringLength(1000)]
    public string? Purpose { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
