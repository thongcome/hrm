namespace HRM.Services.Dev;

using HRM.Data;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Dev-only demo-configuration seeder for the approval workflows (owner request
// 2026-09-03, revised same day). ORIGINALLY this turned isautoapprove ON so the
// demo flows completed with no human approver — but that HID the workflow engine,
// which is the product's headline feature, and would have sunk the presentation
// ("ไม่แสดง workflow อาจจะตกการ present เลย"). So it now does the opposite: it makes
// the business workflows RUN the real approval routing, and cleans up their
// approver config so nothing named "test_..." shows up in front of the client.
//
// Two things, both idempotent, both through EF (audit-logged, no raw SQL):
//   1. Force isautoapprove = false on every business workflow, so a submitted
//      request routes to its configured approvers instead of self-completing.
//   2. Repoint any custom-user approver that still points at the leftover
//      "test_payroll" placeholder account to the real ADVD demo admin
//      ("advadmin") — resolved by loginname, not a hardcoded userid, so it
//      survives a fresh DB where the identity ids differ.
//
// Never runs outside Development (IsDevelopment() gate in Program.cs). A
// production workflow must never be auto-approved or point at a demo account —
// which is exactly why this lives in Services/Dev and not in a migration.
public static class WorkflowAutoApproveSeeder
{
    // The business (non-DEMO_) workflows whose routing we want to actually
    // demonstrate. Kept as a fixed list so the seeder never touches the
    // purpose-built DEMO_* / TEST_* engine fixtures.
    private static readonly string[] BusinessWorkflowCodes =
    {
        "EMPLOYEE_SEPARATION_APPROVAL",
        "LEAVE_APPROVAL",
        "OT_APPROVAL",
        "EXPENSE_CLAIM_APPROVAL",
        "IDP_APPROVAL",
        "WELFARE_CLAIM",
        "PERF_EVAL_APPROVAL",
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var ctx = await dbFactory.CreateDbContextAsync();

        var workflows = await ctx.wf_workflows
            .Where(w => BusinessWorkflowCodes.Contains(w.workflowcode))
            .ToListAsync();
        if (workflows.Count == 0) return;

        var changed = false;

        // 1) Turn auto-approve OFF so the workflow actually runs, and force the
        // workflow back ON. The business workflows are load-bearing for the demo
        // (leave/OT/expense/... all route through them); a single stray click on
        // the admin "ยกเลิกการใช้งาน" button sets isactive=false and then EVERY
        // new request across the company fails to submit ("workflow ... ปิดใช้งาน
        // อยู่"). Re-asserting isactive=true here is a dev-only self-heal so a
        // restart always restores a demonstrable state — the real fix for the
        // easy-misclick is the type-to-confirm guard on the deactivate button.
        foreach (var w in workflows)
        {
            if (w.isautoapprove == true) { w.isautoapprove = false; changed = true; }
            if (w.isactive != true) { w.isactive = true; changed = true; }
        }

        // 2) Repoint the "test_payroll" placeholder approver to "advadmin".
        var placeholder = await ctx.sc_users.FirstOrDefaultAsync(u => u.loginname == "test_payroll");
        var advadmin = await ctx.sc_users.FirstOrDefaultAsync(u => u.loginname == "advadmin");
        if (placeholder is not null && advadmin is not null && placeholder.userid != advadmin.userid)
        {
            var workflowIds = workflows.Select(w => w.workflowid).ToList();
            // Also repoint DEMO_BOUNCE (the reject/send-back demo) so its
            // approver is a login that actually exists (advadmin) — lets the
            // "ส่งกลับแก้ไข" path be driven and demonstrated end-to-end.
            var demoBounceId = await ctx.wf_workflows
                .Where(w => w.workflowcode == "DEMO_BOUNCE")
                .Select(w => w.workflowid)
                .FirstOrDefaultAsync();
            if (demoBounceId != 0) workflowIds.Add(demoBounceId);

            var subIds = await ctx.wf_sub_workflow_masters
                .Where(s => workflowIds.Contains(s.workflowid))
                .Select(s => s.subworkflowid)
                .ToListAsync();

            var rows = await ctx.wf_custom_users
                .Where(cu => subIds.Contains(cu.subworkflowid) && cu.userid == placeholder.userid)
                .ToListAsync();
            foreach (var cu in rows)
            {
                cu.userid = advadmin.userid;
                cu.empid = advadmin.empid;
                changed = true;
            }
        }

        if (changed) await ctx.SaveChangesAsync();
    }
}
