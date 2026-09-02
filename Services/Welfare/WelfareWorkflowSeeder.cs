namespace HRM.Services.Welfare;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Ensures the WELFARE_CLAIM approval workflow exists so welfare claims can route
// through the generic engine (StartJobAsync) like leave/OT. This is REQUIRED
// config (not demo data), so it runs in every environment, idempotent by
// workflowcode — not inside the Development-only seeder block.
//
// Seeds a single approval level resolved by custom ROLE = Admin (HR), the most
// reliable approver for a fresh install: it doesn't depend on the vertical org
// chain (com_organization.approver_*) being populated. HR can later reconfigure
// this workflow through the existing /wf/* admin screens to add a manager
// (vertical) level — config-first, no code change. The wf_* ids are real
// IDENTITY columns, so EF assigns them.
public static class WelfareWorkflowSeeder
{
    public const string WorkflowCode = "WELFARE_CLAIM";

    public static async Task EnsureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("WelfareWorkflowSeeder");
        await using var ctx = await dbFactory.CreateDbContextAsync();

        if (await ctx.wf_workflows.AnyAsync(w => w.workflowcode == WorkflowCode))
            return; // one-shot by code

        var adminRoleId = await ctx.sc_roles
            .Where(r => r.isactive && r.name != null && r.name.ToLower() == "admin")
            .Select(r => (long?)r.roleid)
            .FirstOrDefaultAsync();
        if (adminRoleId is null)
        {
            logger.LogWarning("WELFARE_CLAIM workflow not seeded: no active 'Admin' role found.");
            return;
        }

        var wf = new wf_workflow
        {
            workflowcode = WorkflowCode,
            wname = "อนุมัติเบิกสวัสดิการ",
            wname_en = "Welfare Claim Approval",
            isactive = true,
            isshow = true,
        };
        ctx.wf_workflows.Add(wf);
        await ctx.SaveChangesAsync();

        var level = new wf_sub_workflow_master
        {
            workflowid = wf.workflowid,
            wlevel = 1,
            subject = "อนุมัติเบิกสวัสดิการ",
            iscustomRole = true,
            forwardstatus = "COMPLETED", // single level → approving completes the job
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

        logger.LogInformation("WELFARE_CLAIM workflow seeded (1 level, custom role Admin={RoleId}).", adminRoleId);
    }
}
