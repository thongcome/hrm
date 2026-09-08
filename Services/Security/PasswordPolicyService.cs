namespace HRM.Services.Security;

using HRM.Models;
using Microsoft.Extensions.Options;

// One place that knows what "this password is no longer acceptable" means, so
// the login endpoint, the claims factory, the force-change handler, the
// self-service reset and the admin reset button can't drift apart — the exact
// failure mode the legacy system had, where AddUserAction.jsp set
// isForceChanged=1 and only doLogin.jsp ever knew what that meant.
//
// Deliberately has no DbContext of its own: every caller already has one open
// (or a UserManager), and this service only computes and stamps.
public sealed class PasswordPolicyService
{
    private readonly IOptionsMonitor<PasswordPolicyOptions> _options;

    public PasswordPolicyService(IOptionsMonitor<PasswordPolicyOptions> options) => _options = options;

    public PasswordPolicyOptions Options => _options.CurrentValue;

    // The gate. True = the user may sign in, but must not be allowed to do
    // anything else until they've set a new password.
    //
    // isforcechanged is the admin-driven half (a new account, or a password an
    // admin reset for them); expiry is the time-driven half. The JSP system
    // collapsed both into the same flag at login time
    // ("if (Calendar.getInstance().after(pwdExpDate)) isForceChanged = 1") —
    // kept here, except we don't write the flag back to the row, since the
    // date already says it and a stale flag would outlive the reason for it.
    public bool MustChangePassword(sc_user user)
        => HasLocalPassword(user) && (user.isforcechanged || IsExpired(user));

    // An AD- or SSO-backed account has no local password for HRM to expire or
    // force a change on — that lifecycle belongs to the customer's directory.
    // Gating them here would pin them to a form that cannot help them.
    public static bool HasLocalPassword(sc_user user)
        => string.IsNullOrEmpty(user.AuthProvider);

    public bool IsExpired(sc_user user)
    {
        var maxAge = Options.MaxAgeDays;
        if (maxAge <= 0) return false;               // expiry disabled for this deployment
        if (user.pwdexpdate is null) return false;   // never stamped (pre-policy account) — don't lock people out retroactively
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
    // use, which is precisely what AddUserAction.jsp did in 2006 by setting
    // the password to the login name with isForceChanged = 1.
    public void StampAdminReset(sc_user user, string? modby = null)
    {
        user.isforcechanged = true;
        user.pwdexpdate = NextExpiryDate();
        user.invalidpwcount = 0;
        user.moddate = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(modby)) user.modby = modby;
    }
}
