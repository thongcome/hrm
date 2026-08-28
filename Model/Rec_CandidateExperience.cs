using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Prior work experience captured from a candidate — mirrors Hrd_Experience
// field-for-field so RecOfferService.HireAsync can copy rows straight
// across on hire. See Rec_CandidateEducation.cs for the same rationale.
[Table("Rec_CandidateExperience")]
public class Rec_CandidateExperience
{
    [Key]
    public long Id { get; set; }

    public long CandidateId { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    [StringLength(300)]
    public string? Position { get; set; }
    [StringLength(300)]
    public string? Company { get; set; }

    [StringLength(1000)]
    public string? Remark { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;
}
