using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A person, NOT an Hremployee — mirrors vd_address's "external contact"
// shape (own Id, not the employee master). One candidate can apply to many
// postings over time (a lightweight talent CRM), matched by email on the
// public apply form. Resume is stored via the existing generic doc_center
// registry (doctypecode="CANDIDATE_RESUME", refid=this.Id), not a new table.
[Table("Rec_Candidate")]
public class Rec_Candidate
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(150)]
    public string FirstName { get; set; } = null!;
    [Required, StringLength(150)]
    public string LastName { get; set; } = null!;

    [Required, StringLength(250)]
    public string Email { get; set; } = null!;

    [StringLength(50)]
    public string? Phone { get; set; }

    // Free text: Referral / JobBoard / Agency / Direct / CareerSite
    [StringLength(50)]
    public string? Source { get; set; }

    public long? ResumeDocCenterId { get; set; }

    // Must be true before a public apply-form submission can be saved.
    public bool ConsentGiven { get; set; }
    public DateTime? ConsentDate { get; set; }

    [StringLength(2000)]
    public string? Note { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Null when the candidate applied directly through the public career
    // site (no authenticated actor); set when HR enters a candidate manually.
    public long? CreatedByUserId { get; set; }

    // Career Management: set only for internal-mobility applicants (Source
    // = "Internal") created via RecApplicationService.ApplyInternalAsync —
    // soft link back to the employee record, matching this codebase's
    // convention of not adding a [ForeignKey]/nav property for cross-module
    // references. Null for every external candidate.
    public long? HremployeeId { get; set; }
}
