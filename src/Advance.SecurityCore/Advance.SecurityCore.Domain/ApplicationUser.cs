using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Advance.SecurityCore.Domain;

// Copied verbatim from HRM's Data/ApplicationUser.cs (D:\GitWorkspace\HRM\Data\ApplicationUser.cs).
// `userid` is the bridge column to sc_user.userid that ScUserClaimsPrincipalFactory
// joins on — see EXTRACTION-PLAN.md section on the claims factory for why this
// link exists instead of overloading ClaimTypes.NameIdentifier.
public class ApplicationUser : IdentityUser
{
    [MaxLength(250)]
    public string? FirstName { get; set; }

    [MaxLength(250)]
    public string? LastName { get; set; }

    public long userid { get; set; }
}
