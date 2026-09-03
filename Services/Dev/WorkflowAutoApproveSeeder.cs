namespace HRM.Services.Dev;

using HRM.Data;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Dev-only: turns wf_workflow.isautoapprove ON for a fixed set of demo/testing
// workflows so their jobs complete immediately at submit — no human approver
// needed to demo or test the end-to-end flows (separation, leave, IDP, welfare…).
// Idempotent. Never runs outside Development (see the IsDevelopment() gate around
// the call in Program.cs); a production workflow must never silently auto-approve,
// which is exactly why this is a dev seeder and not a default column value.
public static class WorkflowAutoApproveSeeder
{
    private static readonly string[] AutoApproveCodes =
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

        var rows = await ctx.wf_workflows.Where(w => AutoApproveCodes.Contains(w.workflowcode)).ToListAsync();
        var changed = false;
        foreach (var w in rows)
        {
            if (w.isautoapprove != true) { w.isautoapprove = true; changed = true; }
        }
        if (changed) await ctx.SaveChangesAsync();
    }
}
