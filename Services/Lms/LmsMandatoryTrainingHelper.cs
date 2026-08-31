namespace HRM.Services.Lms;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Turns Lms_CourseRequirement rules into per-employee Lms_MandatoryAssignment
// rows. Static, not a DI service, and takes an already-open context rather
// than creating its own — same convention as EmployeePositionSync.cs, and
// for the same reason: this needs to run INSIDE two different callers'
// existing transactions (LifecycleTaskService.StartOnboardingAsync, and
// EmployeePositionSync.SyncAsync when a slot's occupant changes) rather than
// opening a competing one. Does not call SaveChangesAsync — the caller does,
// atomically alongside whatever else it's writing.
public static class LmsMandatoryTrainingHelper
{
    // Resolves the employee's current position from the DB — correct when
    // the caller runs AFTER any position assignment is already committed
    // (LifecycleTaskService.StartOnboardingAsync).
    public static Task SyncAssignmentsAsync(HRMContext context, long hremployeeId, CancellationToken ct = default)
        => SyncCoreAsync(context, hremployeeId, resolvePositionFromDb: true, knownPosExecTypeId: null, ct);

    // Takes the employee's position as given rather than querying for it.
    // Required when the caller is mid-transaction and the position change
    // is only a pending in-memory edit — EmployeePositionSync.cs calls this
    // BEFORE its own SaveChangesAsync, so a fresh Pos_PositionSlots query
    // here would still see the OLD occupant (or, for a brand-new slot, no
    // row at all) and silently resolve the wrong position. posExecTypeId
    // itself may legitimately be null (a slot with no position type set) —
    // that's a real value, not "unknown", which is why this is a separate
    // method rather than a nullable optional parameter on the method above.
    public static Task SyncAssignmentsForPositionAsync(HRMContext context, long hremployeeId, long? posExecTypeId, CancellationToken ct = default)
        => SyncCoreAsync(context, hremployeeId, resolvePositionFromDb: false, posExecTypeId, ct);

    private static async Task SyncCoreAsync(HRMContext context, long hremployeeId, bool resolvePositionFromDb, long? knownPosExecTypeId, CancellationToken ct)
    {
        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);
        if (emp is null) return;

        // Current position, if any — resolved the same way EmployeeDetail.razor
        // does ("ตำแหน่ง" comes from Pos_PositionSlot.HremployeeId, never a
        // direct field on Hremployee). No position yet is fine — requirements
        // scoped to "every new hire" (PosExecTypeId == null) still apply.
        var posExecTypeId = resolvePositionFromDb
            ? await context.Pos_PositionSlots
                .Where(s => s.HremployeeId == hremployeeId && s.IsActive)
                .Select(s => (long?)s.PosExecTypeId)
                .FirstOrDefaultAsync(ct)
            : knownPosExecTypeId;

        var requirements = await context.Lms_CourseRequirements
            .Where(r => r.IsActive && r.CompanyId == emp.companyid
                && (r.PosExecTypeId == null || r.PosExecTypeId == posExecTypeId))
            .ToListAsync(ct);
        if (requirements.Count == 0) return;

        var alreadyAssignedCourseIds = await context.Lms_MandatoryAssignments
            .Where(a => a.HremployeeId == hremployeeId && a.IsActive)
            .Select(a => a.CourseId)
            .ToListAsync(ct);

        foreach (var req in requirements)
        {
            if (alreadyAssignedCourseIds.Contains(req.CourseId)) continue;
            context.Lms_MandatoryAssignments.Add(new Lms_MandatoryAssignment
            {
                HremployeeId = hremployeeId,
                CourseId = req.CourseId,
                RequirementId = req.Id,
            });
        }
    }
}
