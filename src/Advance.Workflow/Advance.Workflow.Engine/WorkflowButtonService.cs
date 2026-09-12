using Advance.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Advance.Workflow.Engine;

// ============================================================================
//  WorkflowButtonService — ported from HRM's Services/Workflow/
//  WorkflowButtonService.cs. No seams needed: every table this reads
//  (wf_button, wf_button_master, job_master) is already in
//  Advance.Workflow.Domain. Logic unchanged from the original — only
//  HRMContext -> WorkflowDbContext.
// ============================================================================
public sealed class WorkflowButtonService
{
    private readonly IDbContextFactory<WorkflowDbContext> _dbFactory;

    public WorkflowButtonService(IDbContextFactory<WorkflowDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public const string ActionApprove = "approve";
    public const string ActionSendBack = "sendback";
    public const string ActionDecline = "decline";

    public const string ColorSuccess = "Success";
    public const string ColorWarning = "Warning";
    public const string ColorError = "Error";
    public const string ColorPrimary = "Primary";

    public record WorkflowButtonDescriptor(string Code, string Label, string ColorOrClass, string ActionKind);

    public async Task<List<WorkflowButtonDescriptor>> GetButtonsAsync(
        long jobMasterId, int level, long workflowId, long subWorkflowId, CancellationToken ct = default)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync(ct);

        var closed = await ctx.job_masters.Where(j => j.jobmasterid == jobMasterId)
            .Select(j => j.isJobClosed).FirstOrDefaultAsync(ct);
        if (closed == true) return new();

        var active = ctx.wf_buttons.Include(b => b.button_master).Where(b => b.isactive);

        if (level <= 0)
        {
            var startRows = await PickAsync(active.Where(b => b.isStart == true), workflowId, level, subWorkflowId, ct);
            var start = Map(startRows);
            if (start.Count > 0) return start;
            return new() { new WorkflowButtonDescriptor("submit", "ส่งต่อ", ColorPrimary, ActionApprove) };
        }

        var step = await ctx.wf_sub_workflow_masters.FirstOrDefaultAsync(s => s.subworkflowid == subWorkflowId, ct)
                ?? await ctx.wf_sub_workflow_masters.FirstOrDefaultAsync(s => s.workflowid == workflowId && s.wlevel == level, ct);
        var isTop = step?.istop ?? false;
        var isAnd = step?.isandcondition ?? false;

        var rows = await PickAsync(
            active.Where(b => b.isStart != true && (b.istop ?? false) == isTop && b.isAndCondition == isAnd),
            workflowId, level, subWorkflowId, ct);
        var result = Map(rows);
        if (result.Count > 0) return result;

        result.Add(isTop
            ? new WorkflowButtonDescriptor("approve", step?.displayName ?? "อนุมัติ", ColorSuccess, ActionApprove)
            : new WorkflowButtonDescriptor("submit", step?.displayName ?? "ส่งต่อ", ColorPrimary, ActionApprove));
        result.Add(new WorkflowButtonDescriptor("reject", "ส่งกลับ", ColorWarning, ActionSendBack));
        if (isTop) result.Add(new WorkflowButtonDescriptor("decline", "ไม่อนุมัติ", ColorError, ActionDecline));
        return result;
    }

    private static async Task<List<wf_button>> PickAsync(
        IQueryable<wf_button> q, long workflowId, int level, long subWorkflowId, CancellationToken ct)
    {
        if (subWorkflowId > 0)
        {
            var byNode = await q.Where(b => b.subworkflowid == subWorkflowId)
                .OrderBy(b => b.button_master.orderth).ToListAsync(ct);
            if (byNode.Count > 0) return byNode;
        }
        var byLevel = await q.Where(b => b.workflowid == workflowId && b.wlevel == level)
            .OrderBy(b => b.button_master.orderth).ToListAsync(ct);
        if (byLevel.Count > 0) return byLevel;
        return await q.Where(b => b.workflowid == null)
            .OrderBy(b => b.button_master.orderth).ToListAsync(ct);
    }

    private static List<WorkflowButtonDescriptor> Map(List<wf_button> rows)
    {
        var result = new List<WorkflowButtonDescriptor>(rows.Count);
        foreach (var b in rows)
        {
            var m = b.button_master;
            if (m is null) continue;

            var actionKind = MapActionKind(m.actiontypecode, m.code);
            if (actionKind is null) continue;

            var code = FirstNonEmpty(m.code, m.actiontypecode, b.btname) ?? actionKind;
            var label = FirstNonEmpty(m.value, m.name, code) ?? code;
            var color = MapColor(b.class_style ?? m.class_style, actionKind);

            result.Add(new WorkflowButtonDescriptor(code, label, color, actionKind));
        }

        return result;
    }

    private static string? MapActionKind(string? actionTypeCode, string? code)
    {
        var key = FirstNonEmpty(actionTypeCode, code)?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(key)) return null;

        return key switch
        {
            "approve" or "submit" or "recommend" or "approvepartial" or "memberapprove" => ActionApprove,
            "reject" or "notrecommend" => ActionSendBack,
            "decline" or "declinepartial" or "memberdecline" => ActionDecline,
            _ => null,
        };
    }

    private static string MapColor(string? classStyle, string actionKind)
    {
        var c = classStyle?.ToLowerInvariant() ?? string.Empty;

        if (c.Contains("danger")) return ColorError;
        if (c.Contains("warning")) return ColorWarning;
        if (c.Contains("success")) return ColorSuccess;
        if (c.Contains("primary") || c.Contains("info")) return ColorSuccess;

        return actionKind switch
        {
            ActionApprove => ColorSuccess,
            ActionSendBack => ColorWarning,
            ActionDecline => ColorError,
            _ => ColorPrimary,
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        return null;
    }
}
