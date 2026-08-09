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
