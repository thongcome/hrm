namespace HRM.Models;

public enum LifecycleTaskDirection { Onboarding = 1, Offboarding = 2 }

public enum LifecycleTaskStatus { Pending = 1, Completed = 2, Skipped = 3 }

public enum ExitReasonCode
{
    BetterOpportunity = 1,
    Compensation = 2,
    Relocation = 3,
    PersonalHealth = 4,
    FamilyReason = 5,
    Retirement = 6,
    Termination = 7,
    Other = 9
}
