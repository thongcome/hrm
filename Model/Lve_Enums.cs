namespace HRM.Models;

public enum HalfDayPeriod
{
    Morning = 1,
    Afternoon = 2,
}

// Per (company, leave type) policy — see Lve_LeavePolicy.CarryOverMode.
public enum LeaveCarryOverMode
{
    None = 0,
    Capped = 1,
    Unlimited = 2,
}
