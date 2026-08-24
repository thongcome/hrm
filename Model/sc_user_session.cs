using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// One row per signed-in browser session (created in
// ScUserClaimsPrincipalFactory.CreateAsync, once per actual sign-in — not
// per page load). sessiontoken is a random opaque value baked into a
// "sessionid" claim on the auth cookie; it is NOT the cookie itself and
// carries no secret meaning on its own, it's just a lookup key. Revoking a
// session (isrevoked=true) doesn't invalidate the browser's cookie
// directly — it relies on IdentityRevalidatingAuthenticationStateProvider
// checking this table on its next revalidation pass and forcing a
// sign-out when it finds isrevoked=true or the row missing entirely.
[Table("sc_user_session")]
public partial class sc_user_session
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long sessionid { get; set; }

    public long userid { get; set; }

    [Required, StringLength(100)]
    public string sessiontoken { get; set; } = null!;

    [StringLength(50)]
    public string? ipaddress { get; set; }

    [StringLength(500)]
    public string? useragent { get; set; }

    public DateTime createddate { get; set; } = DateTime.Now;
    public DateTime? lastseendate { get; set; }

    [Required]
    public bool isrevoked { get; set; } = false;
    public DateTime? revokeddate { get; set; }
    [StringLength(250)]
    public string? revokedby { get; set; }

    [ForeignKey("userid")]
    [InverseProperty("sc_user_sessions")]
    public virtual sc_user user { get; set; } = null!;
}
