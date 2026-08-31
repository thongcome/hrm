using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Config for the retention/flight-risk indicator (one row per company,
// upserted by RetentionRiskService). The scoring model is deliberately a
// transparent additive checklist — each enabled signal that triggers adds
// its weight to the employee's score, and the reasons are always shown
// alongside the number — NOT a black-box prediction. Risk labels about
// real people are sensitive, so everything here is HR-tunable and the
// whole feature is opt-in (IsEnabled defaults to false); nobody gets
// labeled by hardcoded criteria the company never chose. Signals draw only
// on tenure, position history, and performance grade — deliberately no
// compensation data (salary lives under /pay/* only) and nothing that
// could pierce survey anonymity.
[Table("Talent_RetentionRiskSettings")]
public class Talent_RetentionRiskSettings
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public bool IsEnabled { get; set; } // opt-in — see header

    // Signal 1: new-hire window — employees within their first N months
    // are statistically the most likely to leave (onboarding flight risk).
    public int NewHireMonthsThreshold { get; set; } = 12;
    public int NewHireWeight { get; set; } = 30;

    // Signal 2: stagnation — no position change in N months (from
    // Pos_PositionSlot_his; falls back to WorkDate when no history row).
    public int StagnationMonthsThreshold { get; set; } = 36;
    public int StagnationWeight { get; set; } = 40;

    // Signal 3: high performer (regrettable-attrition lens) — latest
    // approved evaluation at or above this percent. On its own this just
    // marks top talent; combined with signal 2 it surfaces the classic
    // "star going stale" profile that retention programs exist for.
    public int HighPerformerScorePercent { get; set; } = 85;
    public int HighPerformerWeight { get; set; } = 30;

    // Total score at or above this = "เสี่ยงสูง" in the UI.
    public int HighRiskScoreThreshold { get; set; } = 50;
}
