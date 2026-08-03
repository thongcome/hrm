using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ครอบครัว (family member) — ported from legacy PIS "family" table
// (personal/information/Family.jsp). Multi-row per employee. Kept separate
// from Hrd_Marriage (legacy also splits these) since a family record can be
// a spouse, child, or parent — the childedu/childdeducttype/edurefund fields
// only apply to dependents and directly feed PIT deduction calculations, so
// they're kept on this table rather than split out further.
[Table("Hrd_Family")]
public class Hrd_Family
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(13)]
    public string? CardId { get; set; }

    // ความสัมพันธ์ (spouse/child/parent/etc.) — soft-link by name, no
    // separate lookup table built yet (legacy familytype has only ~5 rows).
    [StringLength(100)]
    public string? FamilyType { get; set; }

    [StringLength(20)]
    public string? PrenameCode { get; set; }
    [StringLength(250)]
    public string? FirstName { get; set; }
    [StringLength(250)]
    public string? LastName { get; set; }

    public DateOnly? BirthDate { get; set; }

    [StringLength(100)]
    public string? Nationality { get; set; }
    [StringLength(100)]
    public string? Religion { get; set; }

    // มีชีวิตอยู่/เสียชีวิต
    public bool IsAlive { get; set; } = true;
    public DateOnly? DeceasedDate { get; set; }

    [StringLength(200)]
    public string? Career { get; set; }

    // ลดหย่อนบุตร — directly feeds PIT calculation; kept explicit rather
    // than inferred, since the legacy system treats these as data entry, not
    // computed values.
    public bool IsChild { get; set; }
    public bool ChildStudying { get; set; }
    public bool ChildDeductionEligible { get; set; }
    public bool EduRefundEligible { get; set; }

    [StringLength(1000)]
    public string? Address { get; set; }

    public DateTime? ModDate { get; set; }
    [StringLength(50)]
    public string? ModBy { get; set; }
}
