namespace Advance.Payroll.Core;

// Ported verbatim from HRM Services/Pay/Calculators/NetPayGuardService.cs.
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
