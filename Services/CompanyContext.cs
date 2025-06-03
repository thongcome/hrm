namespace HRM.Services
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;

    public class CompanyContext : ICompanyContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CompanyContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long CompanyId =>
            long.Parse(_httpContextAccessor.HttpContext?.User.FindFirst("CompanyId")?.Value ?? "0");
    }

}
