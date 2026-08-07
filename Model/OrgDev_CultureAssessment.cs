using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A manual HR-entered "org health" snapshot — 4 fixed dimensions scored 1-5,
// taken periodically. Deliberately NOT a survey/response engine with a
// question bank and per-employee responses — that's Employee Engagement
// (HRD Phase 3: pulse survey, eNPS), a separate future module. This is one
// point-in-time judgment call HR records, the same way
// Talent_PotentialRating is a judgment call, not a computed value.
[Table("OrgDev_CultureAssessment")]
public class OrgDev_CultureAssessment
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    // Null = whole-company assessment; set = scoped to one org unit.
    public long? OrganizationId { get; set; }

    public DateOnly AssessmentDate { get; set; }

    [Range(1, 5)]
    public int CommunicationScore { get; set; }
    [Range(1, 5)]
    public int TrustScore { get; set; }
    [Range(1, 5)]
    public int CollaborationScore { get; set; }
    [Range(1, 5)]
    public int LeadershipScore { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public long ConductedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
