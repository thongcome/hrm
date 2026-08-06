using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One row per company — splits Perf_EvaluationInstance.FinalScorePercent
// (0-100) into 3 Performance-axis zones for the 9-box grid: below
// PerformanceLowMaxPercent = Low, at/above PerformanceHighMinPercent =
// High, otherwise Medium. Deliberately no seeded row — TalentGridService
// returns an in-memory default (60/85) when none exists yet; a real row is
// only written once HR saves the settings page.
[Table("Talent_NineBoxSettings")]
public class Talent_NineBoxSettings
{
    [Key]
    public long Id { get; set; }

    [StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Column(TypeName = "decimal(5,2)")]
    public decimal PerformanceLowMaxPercent { get; set; } = 60m;

    [Column(TypeName = "decimal(5,2)")]
    public decimal PerformanceHighMinPercent { get; set; } = 85m;
}
