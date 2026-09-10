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
    /// ปุ่มของ "จุดนี้" ของงาน — method เดียวที่ทุกหน้าใช้
    ///
    /// CEO, 10 ก.ย. 2569: "ทำ method ส่ง parameter สร้างปุ่ม ไว้ใช้ในทุกที่ที่ต้องสร้างปุ่ม
    /// ส่ง jobmasterid, level, workflowid, subworkflowid"
    ///
    /// ตอบจาก config ล้วน ๆ ตาม epms GetButtonList(isStart, istop, isAndCondition):
    ///   - level 0 (ร่าง) ไม่มีใน wf_sub_workflow_master -> ปุ่มของ "ผู้เริ่มเรื่อง" = แถว isStart
    ///   - level อื่น อ่าน istop/isandcondition ของ node (subworkflowid) แล้วเลือกแถว wf_button
    ///     ที่ตรง ลำดับความจำเพาะ: ผูกกับ node นี้ > ผูกกับ workflow+level > ชุดกลาง
    ///   - งานปิดแล้ว = ไม่มีปุ่ม
    /// ไม่เคยคืนลิสต์ว่างสำหรับงานที่ยังเปิด — ถ้ายังไม่ได้ config ก็คืนชุดมาตรฐาน
    /// เพื่อให้ทุกหน้าเห็นปุ่มชุดเดียวกันเสมอ ไม่มีหน้าไหนต้องมีปุ่ม fallback ของตัวเอง
    /// </summary>
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

        // subworkflowid = ตัวตนของ node (กฎสองไอดี) — ถ้าไม่มีก็หาจาก workflow+level
        var step = await ctx.wf_sub_workflow_masters.FirstOrDefaultAsync(s => s.subworkflowid == subWorkflowId, ct)
                ?? await ctx.wf_sub_workflow_masters.FirstOrDefaultAsync(s => s.workflowid == workflowId && s.wlevel == level, ct);
        var isTop = step?.istop ?? false;
        var isAnd = step?.isandcondition ?? false;

        var rows = await PickAsync(
            active.Where(b => b.isStart != true && (b.istop ?? false) == isTop && b.isAndCondition == isAnd),
            workflowId, level, subWorkflowId, ct);
        var result = Map(rows);
        if (result.Count > 0) return result;

        // ยังไม่ได้ config เลย — ชุดมาตรฐานเดิม (ส่งต่อ/อนุมัติ, ส่งกลับ, ไม่อนุมัติเฉพาะขั้นสุดท้าย)
        result.Add(isTop
            ? new WorkflowButtonDescriptor("approve", step?.displayName ?? "อนุมัติ", ColorSuccess, ActionApprove)
            : new WorkflowButtonDescriptor("submit", step?.displayName ?? "ส่งต่อ", ColorPrimary, ActionApprove));
        result.Add(new WorkflowButtonDescriptor("reject", "ส่งกลับ", ColorWarning, ActionSendBack));
        if (isTop) result.Add(new WorkflowButtonDescriptor("decline", "ไม่อนุมัติ", ColorError, ActionDecline));
        return result;
    }

    // ลำดับความจำเพาะของแถว wf_button: ผูกกับ node นี้ > ผูกกับ workflow+level > ชุดกลาง (workflowid null)
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
