namespace HRM.Services.Auth;

// One entry per external SSO/OIDC identity provider a customer connects —
// read from ExternalAuth:Sso:Providers at startup (see Program.cs). Empty by
// default (no real customer IdP yet — CEO, 2026-09-07), so zero OpenID
// Connect schemes get registered until a real provider is configured; adding
// one later is a config change, not a code change.
public class SsoProviderConfig
{
    public string Name { get; set; } = null!;       // scheme name + sc_user.AuthProvider value + sc_external_identity.Provider value
    public string Authority { get; set; } = null!;   // e.g. https://login.microsoftonline.com/{tenant}/v2.0
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string? CallbackPath { get; set; }        // defaults to /signin-oidc-{Name} if unset
}
