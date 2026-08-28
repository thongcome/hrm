namespace HRM.Models;

public enum DisciplinaryActionType
{
    VerbalWarning = 1,
    WrittenWarning = 2,
    Suspension = 3,
    Termination = 4,
}

public enum GrievanceCategory
{
    Harassment = 1,
    Discrimination = 2,
    WorkingConditions = 3,
    Compensation = 4,
    Management = 5,
    Other = 9,
}

public enum GrievanceStatus
{
    Submitted = 1,
    UnderInvestigation = 2,
    Resolved = 3,
    Dismissed = 4,
}

public enum RewardType
{
    Commendation = 1,       // ยกย่อง/ชมเชย (ไม่มีเงิน)
    PerformanceAward = 2,   // รางวัลผลงานดีเด่น
    LengthOfServiceAward = 3, // รางวัลอายุงาน
    CashBonus = 4,           // เงินรางวัลพิเศษ
}

// พ.ร.บ.คุ้มครองแรงงาน มาตรา 118-119: severance is only a legal
// entitlement for employer-initiated termination — voluntary resignation
// carries no severance at all, and มาตรา 119 lists specific exceptions
// (fraud, serious misconduct, etc.) where even a termination doesn't
// require it. SeveranceService gates on this instead of just "ResignDate
// is set".
public enum SeparationType
{
    VoluntaryResignation = 1,  // ลาออกเอง — ไม่มีค่าชดเชย
    TerminationOrdinary = 2,   // เลิกจ้าง ไม่เข้าข่ายมาตรา 119 — มีค่าชดเชยตามอายุงาน
    TerminationSection119 = 3, // เลิกจ้างเข้าข่ายมาตรา 119 — ไม่มีค่าชดเชย
}

public enum SeparationRequestStatus
{
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
}
