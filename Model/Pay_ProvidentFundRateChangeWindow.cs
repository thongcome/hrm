using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Recurring annual window (month/day, not DateOnly) during which employees
// are allowed to submit a rate-change request — mirrors real fund
// regulations that set fixed yearly cutoffs (e.g. "request by 31 May,
// effective 1 July") rather than a fixed calendar date HR would need to
// re-enter every year. A policy can have several windows (the reference
// regulation has two: a mid-year and year-end cutoff).
[Table("Pay_ProvidentFundRateChangeWindow")]
public class Pay_ProvidentFundRateChangeWindow
{
    [Key]
    public long Id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? Code { get; set; }

    public long PolicyId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = null!;

    public int OpenFromMonth { get; set; }
    public int OpenFromDay { get; set; }
    public int OpenToMonth { get; set; }
    public int OpenToDay { get; set; }

    public int EffectiveMonth { get; set; }
    public int EffectiveDay { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Pay_ProvidentFundPolicy Policy { get; set; } = null!;
}
