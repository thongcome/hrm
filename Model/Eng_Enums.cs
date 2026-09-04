namespace HRM.Models;

public enum Eng_QuestionType { Rating = 1, Text = 2, YesNo = 3, MultipleChoice = 4 }

// Values are explicit because existing DB rows store the raw ints — never renumber.
// Culture = culture-assessment campaigns live here so there is exactly one survey
// engine in the app (the OrgDev CultureAssessmentAdmin page launches/reads these
// instead of keeping its own manual-entry engine).
public enum Eng_CampaignType { Survey = 1, Pulse = 2, ENPS = 3, Culture = 4 }

public enum Eng_CampaignStatus { Draft = 1, Open = 2, Closed = 3 }

// Mirrors Info_MessageTarget's targeting shape but kept as its own enum/table
// per this codebase's convention of not sharing target tables across modules.
public enum Eng_TargetType { All = 1, Organization = 2, Employee = 3 }

public enum Eng_ActionPlanStatus { Planned = 1, InProgress = 2, Completed = 3, Cancelled = 4 }

public enum Eng_MilestoneStatus { Pending = 1, Completed = 2, Cancelled = 3 }

// Activity-based points earning is now pluggable by string Code (see
// IPointEarningActivity + Eng_PointsRule.ActivityCode), so the old fixed
// EngPointsSource enum was removed.

// Recognition/kudos side of the Engagement module: points earned from peer
// kudos are redeemed for rewards through the shared workflow engine.
public enum EngRedeemStatus
{
    Draft = 1,          // สร้างคำขอ ยังไม่ส่งอนุมัติ
    PendingApproval = 2, // ส่งเข้า workflow รออนุมัติ
    Approved = 3,        // อนุมัติแล้ว หักแต้มแล้ว
    Rejected = 4,        // ไม่อนุมัติ คืนแต้ม
    Cancelled = 5,       // ผู้ขอยกเลิกเอง
    Fulfilled = 6,       // มอบของรางวัลแล้ว
}
