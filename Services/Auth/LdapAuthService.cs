namespace HRM.Services.Auth;

using System.DirectoryServices.Protocols;
using System.Net;

// Thin LDAP BIND wrapper — "does this username+password authenticate against
// the configured AD/LDAP server" is the only question this answers; it never
// resolves or provisions an sc_user (LoginEndpoints.cs already found the
// sc_user by the loginname typed into the form, same as local password auth
// — this service only replaces the "is the password right" step for accounts
// with sc_user.AuthProvider == "AD").
//
// AD/SSO scaffold (CEO, 2026-09-07): "design for both, prepare structure
// first — no real customer AD yet". Off by default
// (ExternalAuth:Ldap:Enabled=false) and every value comes from config, so
// turning this on for a real deployment is a config change, not a code
// change. System.DirectoryServices.Protocols (not the older, Windows-only
// System.DirectoryServices/ADSI) is cross-platform since .NET 5+, so this
// works whatever OS the app ends up deployed on.
public class LdapAuthService(IConfiguration configuration, ILogger<LdapAuthService> logger)
{
    public record LdapBindResult(bool Succeeded, string? Upn, string? Error);

    public bool IsEnabled => configuration.GetValue("ExternalAuth:Ldap:Enabled", false);

    public Task<LdapBindResult> TryBindAsync(string username, string password, CancellationToken ct = default)
    {
        if (!IsEnabled)
            return Task.FromResult(new LdapBindResult(false, null, "ยังไม่ได้เปิดใช้งาน AD/LDAP (ExternalAuth:Ldap:Enabled=false)"));

        var section = configuration.GetSection("ExternalAuth:Ldap");
        var server = section["Server"];
        if (string.IsNullOrWhiteSpace(server))
            return Task.FromResult(new LdapBindResult(false, null, "ยังไม่ได้ตั้งค่า ExternalAuth:Ldap:Server"));

        var port = section.GetValue("Port", 389);
        var domain = section["Domain"]; // e.g. "corp.customer.local" — appended if username has no '@'/'\'
        var useSsl = section.GetValue("UseSsl", false);

        var upn = username.Contains('@') || username.Contains('\\') || string.IsNullOrWhiteSpace(domain)
            ? username
            : $"{username}@{domain}";

        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(server, port))
            {
                AuthType = AuthType.Basic,
            };
            connection.SessionOptions.SecureSocketLayer = useSsl;
            connection.Credential = new NetworkCredential(upn, password);
            connection.Bind(); // throws LdapException on bad credentials / unreachable server

            return Task.FromResult(new LdapBindResult(true, upn, null));
        }
        catch (LdapException ex)
        {
            logger.LogWarning(ex, "LDAP bind failed for {Upn} against {Server}", upn, server);
            return Task.FromResult(new LdapBindResult(false, null, "ชื่อผู้ใช้หรือรหัสผ่าน AD ไม่ถูกต้อง"));
        }
    }
}
