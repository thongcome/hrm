namespace Advance.SecurityCore.Web;

using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Copied verbatim from HRM's Services/Auth/LdapAuthService.cs — NOT in the
// task's original requested copy list, but LoginEndpoints.cs (which WAS
// requested) depends on it for the AD/LDAP bind branch, and it's already
// fully generic (reads everything from config, no HRM-specific concept).
// See EXTRACTION-PLAN.md's "additions beyond the requested list" note.
public class LdapAuthService(IConfiguration configuration, ILogger<LdapAuthService> logger)
{
    public record LdapBindResult(bool Succeeded, string? Upn, string? Error);

    public bool IsEnabled => configuration.GetValue("ExternalAuth:Ldap:Enabled", false);

    public Task<LdapBindResult> TryBindAsync(string username, string password, CancellationToken ct = default)
    {
        if (!IsEnabled)
            return Task.FromResult(new LdapBindResult(false, null, "AD/LDAP is not enabled (ExternalAuth:Ldap:Enabled=false)"));

        var section = configuration.GetSection("ExternalAuth:Ldap");
        var server = section["Server"];
        if (string.IsNullOrWhiteSpace(server))
            return Task.FromResult(new LdapBindResult(false, null, "ExternalAuth:Ldap:Server is not configured"));

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
            return Task.FromResult(new LdapBindResult(false, null, "Invalid AD username or password"));
        }
    }
}
