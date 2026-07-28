using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("Pay_PayItemType")]
public class Pay_PayItemType
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string NameTh { get; set; } = null!;

    [Required, StringLength(200)]
    public string NameEn { get; set; } = null!;

    public PayItemCategory Category { get; set; }

    // +1 = adds to gross/earnings, -1 = deducts from net
    public int DefaultSignFlag { get; set; } = 1;

    // system-reserved codes (BASE/OT/SSO/PF/TAX/LOAN) can't be deleted from admin UI
    public bool IsSystemReserved { get; set; }

    [StringLength(20)]
    public string? GLAccountCode { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public virtual ICollection<Pay_PayrollLineItem> Pay_PayrollLineItems { get; set; } = new List<Pay_PayrollLineItem>();
}
