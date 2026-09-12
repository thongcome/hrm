namespace Advance.SecurityCore.Services;

using System.Security.Claims;
using Advance.SecurityCore.Domain;

// The seam that keeps ScUserClaimsPrincipalFactory generic. HRM today stamps
// two claims that only make sense when the host has an employee/payroll
// concept at all — "empno" (sc_user.empid, resolved against Hremployee) and
// "payroll_company" (resolved via HRM's own PayrollCompanyResolver against
// Hremployee.companyid). A standalone Payroll/Workflow product built on
// SecurityCore alone has no Hremployee table, so those two claims cannot
// live in SecurityCore itself.
//
// A host registers ONE implementation (services.AddScoped<IHostClaimsEnricher, X>())
// — HRM's would look roughly like:
//
//   public class HrmClaimsEnricher : IHostClaimsEnricher
//   {
//       public async Task EnrichAsync(ClaimsIdentity identity, sc_user scUser, CancellationToken ct)
//       {
//           if (string.IsNullOrWhiteSpace(scUser.empid)) return;
//           identity.AddClaim(new Claim("empno", scUser.empid));
//           var companyId = await PayrollCompanyResolver.ResolveAsync(hrmContext, scUser); // HRM's own context
//           if (!string.IsNullOrWhiteSpace(companyId))
//               identity.AddClaim(new Claim("payroll_company", companyId));
//       }
//   }
//
// A standalone Payroll/Workflow product with no Hremployee table registers
// a no-op enricher (or its own equivalent against pay_employee /
// Workflow.Org's own employee table) instead.
public interface IHostClaimsEnricher
{
    Task EnrichAsync(ClaimsIdentity identity, sc_user scUser, CancellationToken ct = default);
}

// Default no-op so a host that hasn't wired a real enricher yet (or a
// standalone product with nothing extra to add) doesn't have to register
// anything — AddAdvanceSecurityCore registers this unless the host adds its
// own registration afterward (last registration of an interface wins with
// TryAdd-style DI ordering; see AddAdvanceSecurityCore's comment).
public sealed class NullHostClaimsEnricher : IHostClaimsEnricher
{
    public Task EnrichAsync(ClaimsIdentity identity, sc_user scUser, CancellationToken ct = default)
        => Task.CompletedTask;
}
