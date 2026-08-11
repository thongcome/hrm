namespace HRM.Services.Login;

using Microsoft.AspNetCore.Authorization;

// Enforces the "two identities, always" rule for the external (ecosystem /
// central chatbot) API surface, per explicit requirement: every request
// must carry BOTH a client/service identity (which application is calling
// — "client_id" claim) AND a delegated employee identity (which person
// it's acting on behalf of — "empno" claim, matching the same claim name
// ScUserClaimsPrincipalFactory/EssEmployeeResolver already use internally
// for the same purpose). A token asserting only one of the two fails this
// policy even if the JWT signature itself is perfectly valid — a bare
// service token with no employee context, or a bare user token with no
// calling-app context, is not enough on its own.
public class ExternalApiCallerRequirement : IAuthorizationRequirement
{
}

public class ExternalApiCallerHandler : AuthorizationHandler<ExternalApiCallerRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ExternalApiCallerRequirement requirement)
    {
        var clientId = context.User.FindFirst("client_id")?.Value;
        var empNo = context.User.FindFirst("empno")?.Value;

        if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(empNo))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
