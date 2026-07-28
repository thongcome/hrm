namespace HRM.Services.Pay.Exceptions;

using HRM.Models;

public class InvalidPayrollStatusTransitionException : Exception
{
    public PayrollRunStatus CurrentStatus { get; }
    public string AttemptedAction { get; }

    public InvalidPayrollStatusTransitionException(PayrollRunStatus currentStatus, string attemptedAction)
        : base($"Cannot {attemptedAction} a payroll run in status '{currentStatus}'.")
    {
        CurrentStatus = currentStatus;
        AttemptedAction = attemptedAction;
    }
}
