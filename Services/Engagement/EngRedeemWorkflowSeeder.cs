namespace HRM.Services.Engagement;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Ensures the ENG_REDEEM approval workflow exists so a points-redeem request
// routes through the generic engine like leave/OT/welfare instead of being
// approved by a direct admin click. Required config, idempotent by
// workflowcode, one level resolved by custom role = Admin (reconfigurable in
// /wf/*). Same shape as WelfareWorkflowSeeder / WorkflowStateChangeSeeder.
public static class EngRedeemWorkflowSeeder
{
    public const string WorkflowCode = "ENG_REDEEM";

    public static async Task EnsureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("EngRedeemWorkflowSeeder");
        await using var ctx = await dbFactory.CreateDbContextAsync();

        if (await ctx.wf_workflows.AnyAsync(w => w.workflowcode == WorkflowCode))
            return;

        var adminRoleId = await ctx.sc_roles
            .Where(r => r.isactive && r.name != null && r.name.ToLower() == "admin")
            .Select(r => (long?)r.roleid)
            .FirstOrDefaultAsync();
        if (adminRoleId is null)
        {
            logger.LogWarning("ENG_REDEEM workflow not seeded: no active 'Admin' role found.");
            return;
        }

        var wf = new wf_workflow
        {
            workflowcode = WorkflowCode,
            wname = "อนุมัติการแลกของรางวัล",
            wname_en = "Reward Redeem Approval",
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
            subject = "อนุมัติการแลกของรางวัล",
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

        logger.LogInformation("ENG_REDEEM workflow seeded (1 level, custom role Admin={RoleId}).", adminRoleId);
    }
}
