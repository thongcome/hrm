namespace HRM.Services.Workflow;

using HRM.Data;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Groundwork for CONFIG-DRIVEN workflow action buttons (owner request 2026-09-03).
//
// Today the approval action buttons (Approve / Send back / Decline) are HARDCODED
// in the Blazor UI (Components/Shared/WorkflowActionPanel.razor). This service makes
// them come from the legacy epms button tables instead:
//
//   wf_button_master  — the reusable button DEFINITIONS (label, css class, action
//                       type, order) — HRM model `wf_button_master` (DbSet wf_button_masters).
//   wf_button         — MAPS a button_master to a workflow / level / subworkflow, with
//                       the selection flags istop, isStart, isAndCondition, isactive,
//                       showwhenstatus — HRM model `wf_button` (DbSet wf_buttons).
//
// The epms engine picked the button set for a level with
// WorkflowServices.GetButtonList(isStart, istop, isAndCondition):
//     db.wf_button.Where(x => x.isStart == isStart && x.istop == istop
//                          && x.isAndCondition == isAndCondition && x.isactive == true)
//                 .OrderBy(m => m.wf_button_master.orderth)
// i.e. the flags on wf_button choose WHICH mapped buttons apply at the current level,
// and wf_button_master.orderth orders them. We mirror that intent here, additionally
// preferring rows scoped to (workflowid, wlevel) when present and falling back to the
// global (workflowid == null) rows — matching how epms seeds mostly-global buttons but
// allows per-workflow overrides (see workflowid=5/wlevel=7 rows in ttmepms).
//
// SAFE DEFAULT: if no rows match (tables empty / this level not configured yet), returns
// an EMPTY list so the caller keeps using its built-in buttons — nothing breaks before
// wf_button is seeded/mapped by an admin.
public sealed class WorkflowButtonService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public WorkflowButtonService(IDbContextFactory<HRMContext> dbFactory)
        => _dbFactory = dbFactory;

    // The small, closed set of action kinds the Blazor UI knows how to execute.
    // epms has many finer button types (submit/recommend/approvePartial/...); we
    // collapse them onto these three so the panel logic stays simple.
    public const string ActionApprove = "approve";
    public const string ActionSendBack = "sendback";
    public const string ActionDecline = "decline";

    // Color hints the Blazor caller maps to MudBlazor Color.* (Success/Warning/Error/Primary).
    public const string ColorSuccess = "Success";
    public const string ColorWarning = "Warning";
    public const string ColorError = "Error";
    public const string ColorPrimary = "Primary";

    /// <param name="Code">Stable button code (wf_button_master.code, falls back to actiontypecode).</param>
    /// <param name="Label">Display text (wf_button_master.value).</param>
    /// <param name="ColorOrClass">Color hint: "Success" / "Warning" / "Error" / "Primary".</param>
    /// <param name="ActionKind">One of "approve" / "sendback" / "decline".</param>
    public record WorkflowButtonDescriptor(string Code, string Label, string ColorOrClass, string ActionKind);

    /// <summary>
    /// Config-driven button set for a workflow level, mirroring epms GetButtonList selection.
    /// Returns an EMPTY list when nothing is configured (caller falls back to built-in buttons).
    /// </summary>
    public async Task<List<WorkflowButtonDescriptor>> GetButtonsForLevelAsync(
        long workflowId,
        int wlevel,
        bool isTop,
        bool isAndCondition,
        CancellationToken ct = default)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync(ct);

        // Base predicate: active buttons matching this level's istop / isAndCondition
        // semantics (the epms flag-based selection), newest join to the master for label/style/order.
        // NOTE: the epms overload actually used also filtered on isStart; that flag is not part
        // of this method's signature, so we don't filter by it here — a level's start-ness is a
        // caller concern. If isStart filtering is later required, add it as a parameter.
        var baseQuery = ctx.wf_buttons
            .Include(b => b.button_master)
            .Where(b => b.isactive
                        && (b.istop ?? false) == isTop
                        && b.isAndCondition == isAndCondition);

        // Prefer rows scoped to this exact (workflowid, wlevel); if none, fall back to the
        // global rows (workflowid == null) — the shared default button set.
        var scoped = await baseQuery
            .Where(b => b.workflowid == workflowId && b.wlevel == wlevel)
            .OrderBy(b => b.button_master.orderth)
            .ToListAsync(ct);

        var rows = scoped.Count > 0
            ? scoped
            : await baseQuery
                .Where(b => b.workflowid == null)
                .OrderBy(b => b.button_master.orderth)
                .ToListAsync(ct);

        var result = new List<WorkflowButtonDescriptor>(rows.Count);
        foreach (var b in rows)
        {
            var m = b.button_master;
            if (m is null) continue;

            var actionKind = MapActionKind(m.actiontypecode, m.code);
            if (actionKind is null) continue; // unknown/unsupported action type — skip, don't guess.

            var code = FirstNonEmpty(m.code, m.actiontypecode, b.btname) ?? actionKind;
            var label = FirstNonEmpty(m.value, m.name, code) ?? code;
            var color = MapColor(b.class_style ?? m.class_style, actionKind);

            result.Add(new WorkflowButtonDescriptor(code, label, color, actionKind));
        }

        return result;
    }

    // Map epms actiontypecode / code onto the 3 UI action kinds.
    //   Approve family:  approve, submit, recommend, approvePartial, MemberApprove
    //   Send-back family: reject, notrecommend  (backward / return to requester)
    //   Decline family:   decline, declinePartial, MemberDecline
    // Returns null for anything we don't recognise so the caller never renders a
    // button it can't act on.
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

    // Map a Bootstrap-era css class ("btn btn-success" / "btn-danger" / "btn-warning" ...)
    // to a color hint the Blazor panel converts to MudBlazor Color.*.
    // Falls back to a sensible color for the action kind when the class is missing/unknown.
    private static string MapColor(string? classStyle, string actionKind)
    {
        var c = classStyle?.ToLowerInvariant() ?? string.Empty;

        if (c.Contains("danger")) return ColorError;
        if (c.Contains("warning")) return ColorWarning;
        if (c.Contains("success")) return ColorSuccess;
        if (c.Contains("primary") || c.Contains("info")) return ColorSuccess;

        // No usable class — derive from the semantic action.
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
