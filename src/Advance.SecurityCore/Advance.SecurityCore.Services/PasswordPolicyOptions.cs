namespace Advance.SecurityCore.Services;

// Copied verbatim from HRM's Services/Security/PasswordPolicyOptions.cs —
// already fully generic (bound from an appsettings "PasswordPolicy" section,
// no HRM-specific concept anywhere in it).
public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    // 0 = passwords never expire.
    public int MaxAgeDays { get; set; }

    // Days before pwdexpdate that the user starts being warned.
    public int ExpiryWarningDays { get; set; } = 7;

    // Fed into IdentityOptions.Lockout.
    public int MaxFailedAttempts { get; set; } = 5;

    // How long an account stays locked once MaxFailedAttempts is hit.
    public int LockoutMinutes { get; set; } = 15;
}
