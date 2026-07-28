namespace HRM.Services.Pay.Calculators;

// Never lets a negative net pay silently ship in a bank file: clips to zero
// and reports the shortfall so the caller can set
// Pay_PayrollEmployee.IsNegativeNetPayFlag, which PayrollWorkflowService.ApproveAsync
// checks and blocks on. The legacy engine had no such guard at all.
public static class NetPayGuardService
{
    public record NetPayGuardResult(decimal AdjustedNetPay, bool WasNegative, decimal ShortfallAmount);

    public static NetPayGuardResult Ensure(decimal calculatedNetPay)
    {
        if (calculatedNetPay < 0)
            return new NetPayGuardResult(0m, true, -calculatedNetPay);

        return new NetPayGuardResult(calculatedNetPay, false, 0m);
    }
}
