namespace HRM.Services.Pay.Calculators;

// Pure, no DB access. Rate/cap come from ISocialSecurityRateProvider.
public static class SocialSecurityCalculator
{
    public static decimal Calculate(decimal grossWage, decimal ratePercent, decimal wageCap)
    {
        if (grossWage < 0) grossWage = 0;
        var cappedWage = wageCap > 0 ? Math.Min(grossWage, wageCap) : grossWage;
        return Math.Round(cappedWage * ratePercent / 100m, 2, MidpointRounding.AwayFromZero);
    }
}
