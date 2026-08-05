namespace HRM.Endpoints;

using HRM.Models;
using HRM.Services.Audit;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

// Receipt download for expense claim line items. Two legitimate viewers:
// the employee who owns the claim (checked via empno, same rule as
// EssFileEndpoints) or HR (checked via the EXP_ADMIN menu claim) — so this
// endpoint only requires plain authentication and does the ownership check
// itself, rather than requiring a single policy the way most other file
// endpoints in this app do.
public static class ExpenseFileEndpoints
{
    public static void MapExpenseFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/exp/files").RequireAuthorization();

        group.MapGet("/receipt/{docCenterId:long}", async (
            long docCenterId, HttpContext httpContext, IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage storage, IAuditLogger auditLogger) =>
        {
            await using var context = await dbFactory.CreateDbContextAsync();
            var line = await context.Exp_ClaimLineItems.FirstOrDefaultAsync(l => l.ReceiptDocCenterId == docCenterId);
            if (line is null) return Results.NotFound();

            var header = await context.Exp_ClaimHeaders.FirstOrDefaultAsync(h => h.Id == line.ClaimHeaderId);
            if (header is null) return Results.NotFound();

            var isAdmin = httpContext.User.HasClaim("menu", "EXP_ADMIN");
            var empno = httpContext.User.FindFirst("empno")?.Value;
            var isOwner = !string.IsNullOrWhiteSpace(empno) && header.EmpNo == empno;
            if (!isAdmin && !isOwner) return Results.Forbid();

            var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docCenterId);
            if (doc?.path is null) return Results.NotFound();

            await auditLogger.LogAccessAsync("Exp_ClaimLineItem", line.Id.ToString(), isSensitive: true,
                note: isAdmin ? "HR receipt view" : "self receipt view");

            var bytes = await storage.ReadAsync(doc.path);
            return Results.File(bytes, "application/octet-stream", doc.files ?? "receipt");
        });
    }
}
