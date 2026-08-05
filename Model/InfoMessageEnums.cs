namespace HRM.Models;

public enum InfoMessagePriority
{
    Normal = 1,
    Important = 2,
    Urgent = 3,
}

public enum InfoMessageTargetType
{
    All = 1,
    Organization = 2,
    Employee = 3,
}

public enum InfoMessageReadAction
{
    Viewed = 1,
    Downloaded = 2,
}
