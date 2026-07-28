namespace HRM.Services.Pay.Calculators;

// Pure, no DB access. Rates come from Pay_ProvidentFundElection if an active
// row exists for the employee, else fall back to Hremployee.ProvfEmprate/ProvfCorprate.
public static class ProvidentFundCalculator
{
    public record ProvidentFundResult(decimal EmployeeAmount, decimal CompanyAmount);

    public static ProvidentFundResult Calculate(decimal grossWage, decimal employeeRatePercent, decimal companyRatePercent)
    {
        if (grossWage < 0) grossWage = 0;
        var employeeAmount = Math.Round(grossWage * employeeRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        var companyAmount = Math.Round(grossWage * companyRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        return new ProvidentFundResult(employeeAmount, companyAmount);
    }
}
