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

// How often the entitlement for a leave type renews — see
// Lve_LeaveType.EntitlementFrequency. PerYear is the classic annual
// entitlement (sick/personal/vacation). PerEvent means the right attaches
// to an occurrence (maternity, paternity, bereavement) and is NOT capped by
// any yearly balance. OncePerEmployment can be exercised a single time for
// the whole employment (ordination, Hajj, marriage) — LeaveRequestService
// rejects a second non-rejected/non-cancelled request of such a type.
public enum LeaveEntitlementFrequency
{
    PerYear = 0,
    PerEvent = 1,
    OncePerEmployment = 2,
}

// How a request's TotalDays is counted — see Lve_LeaveType.DayCountMethod.
// WorkingDays runs the existing LeaveDayCalculator (company work-week mask +
// holiday calendar); CalendarDays is a plain inclusive calendar-day count,
// the Thai-law convention for maternity/ordination/military style leaves.
public enum LeaveDayCountMethod
{
    WorkingDays = 0,
    CalendarDays = 1,
}
