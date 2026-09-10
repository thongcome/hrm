namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Block 8: attachments per approval level, stored as doc_center rows
// (the existing generic document-registry table — see plan file) keyed by
// refid = job_user_list.jobapproverid, which already uniquely identifies a
// specific (job, level, approver-row), so no reftable discriminator column
// is needed. Upload happens in-process inside WfMyInbox.razor (Blazor
// Server — no HTTP endpoint required for that direction); this endpoint is
// only the download side, following the same PrivateFileStorage +
// IAuditLogger pattern as every other file route in this app.
public static class WorkflowFileEndpoints
{
    public static void MapWorkflowFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/wf/files").RequireAuthorization("Menu:WF_WORKFLOW_ADMIN");

        group.MapGet("/attachment/{docId:long}", async (
            long docId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.doctypecode == "WF_ATTACHMENT");
            if (doc is null || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            await auditLogger.LogAccessAsync("doc_center", docId.ToString(), isSensitive: false,
                note: $"workflow attachment download ({doc.files})");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        });

        // ── ไฟล์แนบของงานหนึ่งงาน ────────────────────────────────────────
        // เส้นทางบนใช้ไม่ได้กับหน้า workflow ตัวใหม่สองเรื่อง: มันให้เฉพาะ
        // WF_ATTACHMENT (เอกสารต้นทางอย่างใบรับรองแพทย์จึงโหลดไม่ได้) และ
        // บังคับสิทธิ์ admin (ผู้อนุมัติทั่วไปเข้าไม่ถึงไฟล์ของงานตัวเอง)
        //
        // เส้นทางนี้ตรวจสิทธิ์จากสิ่งที่ถูกต้องกว่า: "คุณเกี่ยวข้องกับงานนี้ไหม"
        // — เป็นผู้ขอ หรือมีชื่อใน job_user_list ของงานนั้น — และไฟล์ที่ขอ
        // ต้องเป็นของงานนั้นจริง ไม่ใช่ id อะไรก็ได้
        var jobFiles = app.MapGroup("/wf/files/job").RequireAuthorization();

        jobFiles.MapGet("/{jobMasterId:long}/{docId:long}", async (
            long jobMasterId, long docId, HttpContext http,
            IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            var userIdText = http.User.FindFirst("sc_userid")?.Value;
            if (!long.TryParse(userIdText, out var userId)) return Results.Forbid();

            await using var context = await dbFactory.CreateDbContextAsync();

            var job = await context.job_masters
                .Where(j => j.jobmasterid == jobMasterId)
                .Select(j => new { j.jobmasterid, j.createuserid, j.refid, j.workflow.doctypecode })
                .FirstOrDefaultAsync();
            if (job is null) return Results.NotFound();

            var involved = job.createuserid == userId
                || await context.job_user_lists.AnyAsync(a => a.jobmasterid == jobMasterId && a.userid == userId);
            if (!involved) return Results.Forbid();

            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.isActive != false);
            if (doc is null || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            // ไฟล์นี้เป็นของงานนี้จริงไหม — เอกสารต้นทาง หรือไฟล์ที่ผู้อนุมัติแนบ
            var isSourceDoc = doc.doctypecode == job.doctypecode
                && long.TryParse(job.refid, out var refid) && doc.refid == refid;
            var isApproverDoc = doc.doctypecode == "WF_ATTACHMENT" && doc.refid != null
                && await context.job_user_lists.AnyAsync(a => a.jobmasterid == jobMasterId && a.jobapproverid == doc.refid);
            if (!isSourceDoc && !isApproverDoc) return Results.NotFound();

            await auditLogger.LogAccessAsync("doc_center", docId.ToString(), isSensitive: false,
                note: $"workflow job {jobMasterId} attachment download ({doc.files})");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        });
    }
}
