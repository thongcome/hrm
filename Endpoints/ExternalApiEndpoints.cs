namespace HRM.Endpoints;

using System.Security.Claims;
using System.Text;
using HRM.Models;
using HRM.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Resource-server surface for the ecosystem-wide central AI chatbot (and any
// future HumanOk module) once the central OAuth2/OIDC authorization server
// exists — see Program.cs's "ExternalApi" JWT bearer scheme. Every route
// here is gated by the "ExternalApiCaller" policy, which requires BOTH a
// client identity and a delegated employee identity on the bearer token
// (Services/Login/ExternalApiCallerRequirement.cs) — there is no anonymous
// or service-only path into this surface.
//
// Only a read-only diagnostic endpoint exists so far (whoami), to prove the
// JWT-bearer plumbing + employee-claim resolution end-to-end. Real data
// endpoints (leave balance, payslip status, etc.) are a deliberate follow-up
// once this wiring is confirmed working and the chatbot team has an actual
// caller/token model to test against — see the conversation that led here.
public static class ExternalApiEndpoints
{
    public static void MapExternalApiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/external/v1")
            .RequireAuthorization("ExternalApiCaller");

        group.MapGet("/whoami", async (
            HttpContext httpContext,
            IDbContextFactory<HRMContext> dbFactory,
            IAuditLogger auditLogger,
            CancellationToken ct) =>
        {
            var clientId = httpContext.User.FindFirst("client_id")!.Value;
            var empNo = httpContext.User.FindFirst("empno")!.Value;

            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == empNo, ct);
            if (employee is null)
                return Results.NotFound(new { error = $"No employee found for empno claim '{empNo}'" });

            // AuditLogger.ResolveActor() reads the "sc_userid" claim, which
            // a JWT-authenticated request never carries — so ActorUserId
            // will be null here. Put both identities in the note text
            // explicitly so the audit trail stays meaningful for this
            // caller shape (known limitation, same trade-off already
            // accepted elsewhere for the ChangeTracker auto-hook).
            await auditLogger.LogAccessAsync("Hremployee", employee.id.ToString(), isSensitive: true,
                note: $"External API whoami — client_id={clientId}, empno={empNo}", ct);

            return Results.Ok(new
            {
                clientId,
                empNo,
                employeeId = employee.id,
                name = $"{employee.EmpName} {employee.EmpSurname}",
                companyId = employee.companyid,
            });
        });
    }

    // Development-only: mints a short-lived test JWT signed with the local
    // ExternalApiAuth:DevSigningKey user-secret, so /api/external/v1/* can
    // be exercised end-to-end before the real central auth server exists.
    // Deliberately unauthenticated (its entire job is bootstrapping test
    // tokens) — safe ONLY because Program.cs never even maps this method
    // outside Development.
    public static void MapExternalApiDevEndpoints(this WebApplication app)
    {
        app.MapPost("/api/external/v1/dev/mint-test-token", (
            string clientId,
            string empNo,
            IConfiguration configuration) =>
        {
            var devKey = configuration["ExternalApiAuth:DevSigningKey"];
            if (string.IsNullOrWhiteSpace(devKey))
                return Results.Problem(
                    "ExternalApiAuth:DevSigningKey is not set — run: dotnet user-secrets set \"ExternalApiAuth:DevSigningKey\" \"<32+ char random string>\"",
                    statusCode: StatusCodes.Status500InternalServerError);

            var audience = configuration["ExternalApiAuth:Audience"] ?? "humanok-hrm";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(devKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "humanok-dev-issuer",
                audience: audience,
                claims: new[]
                {
                    new Claim("client_id", clientId),
                    new Claim("empno", empNo),
                },
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials);

            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
            return Results.Ok(new { access_token = jwt, token_type = "Bearer", expires_in = 1800 });
        });
    }
}
