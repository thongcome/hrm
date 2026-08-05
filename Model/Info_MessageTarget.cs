using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Who should see a given info_message. A single announcement can have several
// of these rows at once (e.g. two organizations + one specific employee).
// TargetType=All needs no companion id — and, separately, an announcement
// with zero target rows at all is also treated as "visible to everyone" by
// InfoMessageService (a deliberate fallback, not an error state).
[Table("Info_MessageTarget")]
public class Info_MessageTarget
{
    public long Id { get; set; }

    public long InfoMessageId { get; set; }

    public InfoMessageTargetType TargetType { get; set; }

    // Soft-link -> com_organization.id. Matching includes the org's subtree
    // (Hremployee.orgcodefull starting with the target org's orgcodefull).
    public long? TargetOrganizationId { get; set; }

    // Soft-link -> Hremployee.id
    public long? TargetHremployeeId { get; set; }

    // UI-only display labels (org name / employee name) set when a target is
    // freshly added in the admin form, so the chip list can render a friendly
    // name without a round-trip. Not persisted.
    [NotMapped]
    public string? TargetOrgCache { get; set; }

    [NotMapped]
    public string? TargetEmpCache { get; set; }
}
