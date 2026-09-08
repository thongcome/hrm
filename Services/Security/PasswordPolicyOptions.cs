namespace HRM.Services.Security;

// The three password rules the original JSP system enforced inside
// solar.SecurityBean (studied against D:\Tomcat5HRPack 8 ก.ย. 2569) and that
// HRM had lost along the way: force-change-on-first-login, password expiry,
// and failed-attempt lockout. The legacy system hardcoded two of them and
// read the third out of its `Parameter` table per office; here all three are
// ordinary configuration bound from appsettings' "PasswordPolicy" section, so
// a customer deployment tunes them without a code change.
//
// Defaults are deliberately the *old* behaviour for expiry (MaxAgeDays = 0 =
// never expires) so switching this file in changes nothing for the existing
// 7,000 sc_user rows — a site turns expiry on explicitly.
public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    // 0 = passwords never expire. Any positive value is the number of days a
    // password stays valid; sc_user.pwdexpdate is stamped from it every time
    // a password is set, which is exactly what the JSP system compared
    // against on login ("if (now after pwdExpDate) isForceChanged = 1").
    public int MaxAgeDays { get; set; }

    // Days before pwdexpdate that the user starts being warned (banner in
    // MainLayout). Purely informational — nothing is blocked until expiry.
    public int ExpiryWarningDays { get; set; } = 7;

    // Fed into IdentityOptions.Lockout. The legacy equivalent was
    // users.InvalidPWCount vs the "invalid_password_count" parameter; here
    // ASP.NET Core Identity owns the counter (AspNetUsers.AccessFailedCount /
    // LockoutEnd) and sc_user.invalidpwcount is kept as a mirror so the
    // legacy field and the admin screen still tell the truth.
    public int MaxFailedAttempts { get; set; } = 5;

    // How long an account stays locked once MaxFailedAttempts is hit.
    // The JSP system had no timed lockout at all — it disabled the account
    // permanently (isDisable = 1), which needed an admin to undo. A timed
    // lockout is the modern default because it stops brute force without
    // handing an attacker a way to lock every real user out for good.
    public int LockoutMinutes { get; set; } = 15;
}
