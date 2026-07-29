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
    ManualEntry = 2
}

public enum AttWorkLocation
{
    Office = 0,
    Wfh = 1,
    Mixed = 2
}
