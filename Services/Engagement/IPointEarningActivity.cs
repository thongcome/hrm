using HRM.Models;

namespace HRM.Services.Engagement;

// A pluggable point-earning activity. Any module that wants to join the points
// program implements this with its own stable Code, registers it in DI as
// IPointEarningActivity, and it becomes selectable on the points-rules setup
// page — no change to the engagement module itself. This is the extensibility
// the owner asked for: "each activity has its own code; at setup you pick the
// code to enrol it in the points program."
//
// DetectAsync returns the qualifying events it finds for a company; the caller
// (EngPointsService) skips any already in Eng_PointsLedger (dedup on
// RefTable+RefId) and credits the rest at the rule's configured points.
public interface IPointEarningActivity
{
    string Code { get; }        // stable id, e.g. "LMS_COMPLETION"
    string Name { get; }        // display name, e.g. "จบหลักสูตรอบรม"
    string HowEarned { get; }   // one-line description of the trigger

    Task<IReadOnlyList<PointEarnEvent>> DetectAsync(HRMContext context, string companyId, CancellationToken ct = default);
}

// One qualifying earn event. RefTable+RefId must be stable + unique per event
// so re-running the sync never double-awards (e.g. "Lms_Enrollment"/id).
public record PointEarnEvent(long HremployeeId, string RefTable, string RefId, string Note);

// Collects every registered IPointEarningActivity so the service and setup page
// can look one up by code and enumerate what's available to enrol.
public class PointActivityRegistry(IEnumerable<IPointEarningActivity> activities)
{
    private readonly Dictionary<string, IPointEarningActivity> _byCode =
        activities.ToDictionary(a => a.Code, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<IPointEarningActivity> All => _byCode.Values.OrderBy(a => a.Name).ToList();
    public IPointEarningActivity? Find(string code) => _byCode.TryGetValue(code, out var a) ? a : null;
}
