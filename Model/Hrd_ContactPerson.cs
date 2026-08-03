using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// บุคคลติดต่อในยามฉุกเฉิน — ported from legacy PIS "contactperson" table
// (personal/contactperson/PersonContactPersonCreate.jsp). The legacy JSP form
// only exposed a single free-text address field; extended here with name/
// phone/relation since an emergency contact without a name or phone number
// isn't actually usable — the legacy field set was incomplete for this
// purpose, not a deliberate design worth preserving as-is.
[Table("Hrd_ContactPerson")]
public class Hrd_ContactPerson
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(250)]
    public string? Name { get; set; }
    [StringLength(200)]
    public string? Relation { get; set; }
    [StringLength(50)]
    public string? Phone { get; set; }
    [StringLength(1000)]
    public string? Address { get; set; }

    [StringLength(1000)]
    public string? Remark { get; set; }

    public DateTime? ModDate { get; set; }
    [StringLength(50)]
    public string? ModBy { get; set; }
}
