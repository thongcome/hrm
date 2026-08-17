namespace HRM.Models;

// Master switch per company — attendance tracking is opt-in, not mandatory.
// Some companies presenting this system won't have shifts, or won't care
// about work hours at all, so every other Att_* page must behave inertly
// when TrackingMode is None rather than requiring data to be entered.
public enum AttTrackingMode
{
    None = 0,
    SimpleInOut = 1,
    ShiftBased = 2
}

public enum AttDeviceType
{
    Fingerprint = 0,
    FaceRecognition = 1,
    CardReader = 2,
    Mobile = 3,
    Manual = 9
}

// Many fingerprint/face-recognition devices don't reliably report direction
// in their exported logs — Unknown is a first-class value, not an error case.
public enum AttPunchDirection
{
    In = 0,
    Out = 1,
    Unknown = 9
}

public enum AttPunchSource
{
    Device = 0,
    WfhSelfCheckin = 1,
    ManualEntry = 2,
    GpsCheckin = 3
}

public enum AttWorkLocation
{
    Office = 0,
    Wfh = 1,
    Mixed = 2
}

// Weekly project-timesheet submission status. No scheduler exists in this
// codebase — Status only ever transitions on explicit user action (Draft ->
// PendingApproval) or via lazy apply-on-read from the Workflow Engine job
// (PendingApproval -> Approved/Rejected), same pattern as Idp_Plan/
// Perf_EvaluationInstance.
public enum Att_TimesheetStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4
}

// Classifies the calendar date an OT request falls on, for looking up the
// legally-mandated premium multiplier (พ.ร.บ.คุ้มครองแรงงาน มาตรา 61-63) in
// Att_OtRule. Holiday takes priority over RestDay if a company's weekly rest
// day happens to fall on a configured holiday. RestDay uses the same
// Saturday/Sunday convention as LeaveDayCalculator — this codebase has no
// per-company configurable weekly rest day yet.
public enum OtDayType
{
    Workday = 1,
    Holiday = 2,
    RestDay = 3
}
