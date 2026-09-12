using Advance.SecurityCore.Domain;
using Microsoft.Extensions.Options;

namespace Advance.SecurityCore.Services;

// Copied verbatim (logic-wise) from HRM's Services/Security/PasswordPolicyService.cs.
// Namespace-only change: HRM.Models.sc_user -> Advance.SecurityCore.Domain.sc_user.
// No DbContext of its own — every caller already has one open (or a
// UserManager); this service only computes and stamps.
public sealed class PasswordPolicyService
{
    private readonly IOptionsMonitor<PasswordPolicyOptions> _options;

    public PasswordPolicyService(IOptionsMonitor<PasswordPolicyOptions> options) => _options = options;

    public PasswordPolicyOptions Options => _options.CurrentValue;

    // The gate. True = the user may sign in, but must not be allowed to do
    // anything else until they've set a new password.
    public bool MustChangePassword(sc_user user)
        => HasLocalPassword(user) && (user.isforcechanged || IsExpired(user));

    // An AD- or SSO-backed account has no local password to expire or force
    // a change on.
    public static bool HasLocalPassword(sc_user user)
        => string.IsNullOrEmpty(user.AuthProvider);

    public bool IsExpired(sc_user user)
    {
        var maxAge = Options.MaxAgeDays;
        if (maxAge <= 0) return false;               // expiry disabled for this deployment
        if (user.pwdexpdate is null) return false;   // never stamped — don't lock people out retroactively
        return user.pwdexpdate.Value < DateTime.Now;
    }

    // Days left before the password expires, or null when expiry is off, the
    // date was never stamped, or it's still further out than the warning
    // window. Drives the banner only — never blocks anything.
    public int? DaysUntilExpiryWarning(sc_user user)
    {
        if (Options.MaxAgeDays <= 0 || user.pwdexpdate is null || !HasLocalPassword(user)) return null;
        var days = (int)Math.Ceiling((user.pwdexpdate.Value - DateTime.Now).TotalDays);
        if (days < 0) return null;                            // already expired — MustChangePassword covers it
        return days <= Options.ExpiryWarningDays ? days : null;
    }

    // The new expiry stamp to write whenever a password is actually set.
    public DateTime? NextExpiryDate()
        => Options.MaxAgeDays > 0 ? DateTime.Now.AddDays(Options.MaxAgeDays) : null;

    // Called from every path that legitimately sets a password the *user*
    // chose (force-change, self-service reset). Clears the force flag and
    // restarts the clock. Caller saves.
    public void StampPasswordChanged(sc_user user, string? modby = null)
    {
        user.isforcechanged = false;
        user.pwdexpdate = NextExpiryDate();
        user.invalidpwcount = 0;
        user.moddate = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(modby)) user.modby = modby;
    }

    // Called when an ADMIN sets a password on someone's behalf: the opposite
    // stamp — the password is valid right now but must be replaced on first
    // use.
    public void StampAdminReset(sc_user user, string? modby = null)
    {
        user.isforcechanged = true;
        user.pwdexpdate = NextExpiryDate();
        user.invalidpwcount = 0;
        user.moddate = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(modby)) user.modby = modby;
    }
}
