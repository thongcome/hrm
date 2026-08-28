using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Education captured from a candidate — either typed on the public career
// site apply form (one row, the candidate's highest degree) or added by HR
// on CandidateDetail.razor (unlimited rows). Field-for-field mirror of
// Hrd_Education on purpose: RecOfferService.HireAsync copies these rows
// straight across into Hrd_Education once the candidate is hired, so the
// shapes must match 1:1 or that copy becomes lossy.
[Table("Rec_CandidateEducation")]
public class Rec_CandidateEducation
{
    [Key]
    public long Id { get; set; }

    public long CandidateId { get; set; }

    [StringLength(200)]
    public string? Level { get; set; }
    [StringLength(200)]
    public string? Degree { get; set; }
    [StringLength(200)]
    public string? Major { get; set; }
    [StringLength(200)]
    public string? MajorSubject { get; set; }
    [StringLength(200)]
    public string? Faculty { get; set; }

    [StringLength(300)]
    public string? Institute { get; set; }
    [StringLength(100)]
    public string? Country { get; set; }

    public DateOnly? EntryDate { get; set; }
    public DateOnly? FinishedDate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Gpa { get; set; }

    public bool IsHonors { get; set; }
    public bool IsHighestDegree { get; set; }

    [StringLength(1000)]
    public string? Remark { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;
}
