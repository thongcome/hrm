namespace HRM.Services.Workflow;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Ensures the WF_STATE_CHANGE approval workflow exists so a request to
// deactivate/reactivate a business workflow can route through the generic
// engine (StartJobAsync) like any other approval. Required config (not demo
// data), idempotent by workflowcode. Seeds a single level resolved by custom
// ROLE = Admin — the most reliable approver for a fresh install (no dependency
// on the vertical org chain). Admins can reconfigure the approver chain later
// through the /wf/* screens (config-first): "create a workflow, then approvals
// follow whatever workflow was configured", per the owner's instruction. Same
// shape as WelfareWorkflowSeeder.
public static class WorkflowStateChangeSeeder
{
    public const string WorkflowCode = "WF_STATE_CHANGE";

    public static async Task EnsureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("WorkflowStateChangeSeeder");
        await using var ctx = await dbFactory.CreateDbContextAsync();

        if (await ctx.wf_workflows.AnyAsync(w => w.workflowcode == WorkflowCode))
            return; // one-shot by code

        var adminRoleId = await ctx.sc_roles
            .Where(r => r.isactive && r.name != null && r.name.ToLower() == "admin")
            .Select(r => (long?)r.roleid)
            .FirstOrDefaultAsync();
        if (adminRoleId is null)
        {
            logger.LogWarning("WF_STATE_CHANGE workflow not seeded: no active 'Admin' role found.");
            return;
        }

        var wf = new wf_workflow
        {
            workflowcode = WorkflowCode,
            wname = "อนุมัติเปลี่ยนสถานะ Workflow",
            wname_en = "Workflow State-Change Approval",
            wstatus = "ACTIVE",
            isactive = true,
            isshow = true,
        };
        ctx.wf_workflows.Add(wf);
        await ctx.SaveChangesAsync();

        var level = new wf_sub_workflow_master
        {
            workflowid = wf.workflowid,
            wlevel = 1,
            subject = "อนุมัติเปลี่ยนสถานะ Workflow",
            iscustomRole = true,
            forwardstatus = "COMPLETED",
            standstatus = "PENDING",
            backwardstatus = "RETURNED",
            istop = true,
            isshow = true,
        };
        ctx.wf_sub_workflow_masters.Add(level);
        await ctx.SaveChangesAsync();

        ctx.wf_custom_roles.Add(new wf_custom_role
        {
            subworkflowid = level.subworkflowid,
            workflowid = wf.workflowid,
            wlevel = 1,
            roleid = adminRoleId.Value,
            isactive = true,
        });
        await ctx.SaveChangesAsync();

        logger.LogInformation("WF_STATE_CHANGE workflow seeded (1 level, custom role Admin={RoleId}).", adminRoleId);
    }
}
