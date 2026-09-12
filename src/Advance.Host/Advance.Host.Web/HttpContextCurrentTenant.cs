using Advance.Host.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Advance.Host.Web;

/// <summary>
/// Default <see cref="ICurrentTenant"/> implementation registered by
/// <see cref="ServiceCollectionExtensions.AddAdvanceHost"/>. Resolves lazily
/// from the ambient <see cref="IHttpContextAccessor"/> per
/// <see cref="AdvanceHostOptions.TenantResolution"/> — no middleware is
/// required to populate this (unlike HRM's own claims-to-menu bridging in
/// ScUserClaimsPrincipalFactory, tenant resolution here needs nothing more
/// than reading the current request/user, which is already available at the
/// point anything asks for TenantId).
/// </summary>
public class HttpContextCurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AdvanceHostOptions _options;

    public HttpContextCurrentTenant(IHttpContextAccessor httpContextAccessor, IOptions<AdvanceHostOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public string? TenantId => _options.TenantResolution switch
    {
        TenantResolutionMode.Fixed => _options.FixedTenantId,
        TenantResolutionMode.Header => _httpContextAccessor.HttpContext?.Request.Headers[_options.TenantHeaderName].FirstOrDefault(),
        TenantResolutionMode.Claim => _httpContextAccessor.HttpContext?.User.FindFirst(_options.TenantClaimType)?.Value,
        _ => null,
    };
}
