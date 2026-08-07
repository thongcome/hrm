using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// A location an employee may GPS-check-in from (HQ, branch office, site
// office, etc.) — a company can have several. Att_PunchLog rows created via
// the ESS GPS check-in flow are only accepted within RadiusMeters of at
// least one active row here.
[Table("Att_GeofenceLocation")]
public class Att_GeofenceLocation
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [Column(TypeName = "decimal(9,6)")]
    public decimal Latitude { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal Longitude { get; set; }

    public int RadiusMeters { get; set; } = 100;

    public bool IsActive { get; set; } = true;
}
