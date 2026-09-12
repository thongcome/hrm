namespace Advance.Payroll.Core;

// Ported verbatim from HRM Services/Pay/Calculators/WelfareFundCalculator.cs.
public static class WelfareFundCalculator
{
    public record Result(decimal EmployeeAmount, decimal CompanyAmount);

    public static Result Calculate(decimal grossWage, decimal employeeRatePercent, decimal companyRatePercent, decimal? wageCap)
    {
        if (grossWage < 0) grossWage = 0;
        var cappedWage = wageCap is decimal cap && cap > 0 ? Math.Min(grossWage, cap) : grossWage;

        var employeeAmount = Math.Round(cappedWage * employeeRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        var companyAmount = Math.Round(cappedWage * companyRatePercent / 100m, 2, MidpointRounding.AwayFromZero);

        return new Result(employeeAmount, companyAmount);
    }
}
