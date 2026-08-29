using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

public class Hr_Grievance
{
    public long Id { get; set; }

    // Stable human-facing case number for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(30)]
    public string? CaseNo { get; set; }

    // Deliberately left null when IsAnonymous is true, even though the submitter is
    // authenticated when they file — the record itself never retains the identity link
    // for anonymous reports, matching how anonymous-reporting channels are expected to work.
    public long? ReporterHremployeeId { get; set; }
    public bool IsAnonymous { get; set; }
    [StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public GrievanceCategory Category { get; set; }
    [StringLength(200)]
    public string Subject { get; set; } = null!;
    [Column(TypeName = "nvarchar(max)")]
    public string Description { get; set; } = null!;
    public DateOnly? IncidentDate { get; set; }
    [StringLength(500)]
    public string? InvolvedPersons { get; set; }

    public GrievanceStatus Status { get; set; } = GrievanceStatus.Submitted;
    public long? AssignedToUserId { get; set; }
    [Column(TypeName = "nvarchar(max)")]
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(ReporterHremployeeId))]
    public virtual Hremployee? Hremployee { get; set; }
}
