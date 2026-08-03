using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประวัติการเปลี่ยนชื่อ-นามสกุล — ported from legacy PIS "changename" table
// (personal/changename/PersonChangeCreate.jsp). Multi-row per employee;
// records both the old and new name plus the supporting document reference,
// since a legal name change needs an audit trail, not just an overwrite.
[Table("Hrd_NameChangeHistory")]
public class Hrd_NameChangeHistory
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    [StringLength(20)]
    public string? OldPrenameCode { get; set; }
    [StringLength(250)]
    public string? OldFirstName { get; set; }
    [StringLength(250)]
    public string? OldLastName { get; set; }

    [StringLength(20)]
    public string? NewPrenameCode { get; set; }
    [StringLength(250)]
    public string? NewFirstName { get; set; }
    [StringLength(250)]
    public string? NewLastName { get; set; }

    [StringLength(100)]
    public string? DocNo { get; set; }
    public DateOnly? DocDate { get; set; }
    [StringLength(300)]
    public string? IssuedBy { get; set; }
    public DateOnly? RegisterDate { get; set; }

    [StringLength(1000)]
    public string? Remark { get; set; }

    public DateTime? ModDate { get; set; }
    [StringLength(50)]
    public string? ModBy { get; set; }
}
