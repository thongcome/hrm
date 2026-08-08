namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Lms;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Course-material download mirrors WorkflowFileEndpoints.cs's PrivateFileStorage
// + IAuditLogger pattern exactly. Certificate download generates the PDF fresh
// on every request (LmsCertificatePdfService is static, no stored file) and is
// gated to either the enrollment's own employee (via the "empno" claim) or an
// LMS_ADMIN-holding HR user — Menu:LMS_ACCESS alone is not enough since that
// policy is granted to both roles.
public static class LmsFileEndpoints
{
    public static void MapLmsFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/lms/files");

        group.MapGet("/material/{docId:long}", async (
            long docId, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docId && d.doctypecode == "LMS_MATERIAL");
            if (doc is null || string.IsNullOrWhiteSpace(doc.path) || string.IsNullOrWhiteSpace(doc.files))
                return Results.NotFound();

            await auditLogger.LogAccessAsync("doc_center", docId.ToString(), isSensitive: false,
                note: $"lms material download ({doc.files})");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files);
        }).RequireAuthorization("Menu:LMS_ADMIN");

        group.MapGet("/certificate/{enrollmentId:long}", async (
            long enrollmentId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();

            var enrollment = await context.Lms_Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId);
            if (enrollment is null || enrollment.Status != EnrollmentStatus.Completed)
                return Results.NotFound();

            var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.id == enrollment.HremployeeId);
            if (employee is null)
                return Results.NotFound();

            var isOwner = httpContext.User.FindFirst("empno")?.Value == employee.EmpNo;
            var isAdmin = httpContext.User.HasClaim("menu", "LMS_ADMIN");
            if (!isOwner && !isAdmin)
                return Results.Forbid();

            var session = await context.Lms_CourseSessions.FirstOrDefaultAsync(s => s.Id == enrollment.CourseSessionId);
            var course = session is null ? null : await context.Lms_Courses.FirstOrDefaultAsync(c => c.Id == session.CourseId);
            if (session is null || course is null || enrollment.CompletedDate is null)
                return Results.NotFound();

            await auditLogger.LogAccessAsync("Lms_Enrollment", enrollmentId.ToString(), isSensitive: false,
                note: "certificate download");

            var pdf = LmsCertificatePdfService.Generate(employee, course, session, enrollment.CompletedDate.Value);
            return Results.File(pdf, "application/pdf", $"certificate_{enrollmentId}.pdf");
        }).RequireAuthorization("Menu:LMS_ACCESS");
    }
}
