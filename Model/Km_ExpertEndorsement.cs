using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Closes the gap the original plan deliberately left open ("no endorsement/
// rating by others" — see Km_ExpertProfile.cs comment): a colleague vouching
// for someone's expertise, distinct from the self-declared Km_ExpertProfile
// row itself. One endorsement per (ExpertProfileId, EndorsedByHremployeeId)
// pair — enforced in KmExpertEndorsementService, not a DB constraint, same
// convention as every other "no duplicate" rule in this codebase.
[Table("Km_ExpertEndorsement")]
public class Km_ExpertEndorsement
{
    [Key]
    public long Id { get; set; }

    public long ExpertProfileId { get; set; }
    public long EndorsedByHremployeeId { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
