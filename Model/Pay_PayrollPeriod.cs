using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Master config for payroll "งวด" (period/term) selection — replaces free-text
// Year/Month/TermOfPay entry in the legacy PayrollProcess.razor wizard with a
// dropdown backed by real, admin-managed rows instead of unvalidated input.
[Table("Pay_PayrollPeriod")]
public class Pay_PayrollPeriod
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    // Gregorian only — matches the InvariantCulture convention used across
    // Pay_* to avoid the Buddhist-year bug (Thai ambient culture formats
    // "yyyy" as B.E., e.g. 2569 instead of 2026).
    public int Year { get; set; }

    public int Month { get; set; }

    public int TermNo { get; set; } = 1;

    [Required, StringLength(100)]
    public string Label { get; set; } = null!;

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public bool IsActive { get; set; } = true;
}
