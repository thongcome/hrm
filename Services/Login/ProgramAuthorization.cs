namespace HRM.Services.Login;

using Microsoft.AspNetCore.Authorization;

// Per-action permission check (Create/Edit/Delete on a specific entity),
// sitting alongside MenuAuthorization's whole-page "Menu:XXX" gate — a page
// can be visible (Menu:XXX granted) while a specific action inside it is
// not. Reuses sc_program/sc_role_program — scaffolded from the legacy JSP
// dispatch-by-ProgramID system (same category as sc_menu was before it got
// wired up earlier this session: correct shape, just never connected to
// anything) — repurposed here: one sc_program row per (entity, action), e.g.
// progcode="EMPLOYEE_CREATE"/"EMPLOYEE_EDIT"/"EMPLOYEE_DELETE", granted to a
// role via sc_role_program.isactive — the exact same binary-grant shape
// sc_role_menu already uses for Menu:XXX. sc_program's legacy JSP fields
// (templatename/filename/progmastercode) are simply left null; only
// progcode/progname/isactive matter for this use.
//
// A CRUD page uses this for button-level gating, same "both layers, always"
// rule as the page-level Menu: gate:
//   <AuthorizeView Policy="Program:EMPLOYEE_CREATE"><Authorized>
//       <MudButton>เพิ่ม</MudButton>
//   </Authorized></AuthorizeView>
// and the server-side handler re-checks the identical policy via
// IAuthorizationService.AuthorizeAsync before writing — see the CRUD skill
// (.claude/skills/CRUD/SKILL.md) for the full per-page convention.
public class ProgramRequirement : IAuthorizationRequirement
{
    public string ProgramCode { get; }
    public ProgramRequirement(string programCode) => ProgramCode = programCode;
}

public class ProgramAuthorizationHandler : AuthorizationHandler<ProgramRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProgramRequirement requirement)
    {
        if (context.User.HasClaim("program", requirement.ProgramCode))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
