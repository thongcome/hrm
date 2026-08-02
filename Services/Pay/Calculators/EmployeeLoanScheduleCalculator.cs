namespace HRM.Services.Pay.Calculators;

// Pure, no DB access. Splits a principal evenly (no interest) across N
// periods starting at startPeriod ("YYYYMM"), rounding each installment to
// 2 decimals and folding any rounding remainder into the LAST installment
// so the sum of all installments always equals the principal exactly.
public static class EmployeeLoanScheduleCalculator
{
    public record InstallmentLine(int InstallmentNo, string Period, decimal Amount, decimal BalanceAfter);

    public static List<InstallmentLine> Calculate(decimal principal, int totalInstallments, string startPeriod)
    {
        if (principal <= 0)
            throw new ArgumentException("principal must be positive");
        if (totalInstallments <= 0)
            throw new ArgumentException("totalInstallments must be positive");
        if (startPeriod.Length != 6 || !int.TryParse(startPeriod[..4], out var year) || !int.TryParse(startPeriod[4..], out var month) || month is < 1 or > 12)
            throw new ArgumentException("startPeriod must be in YYYYMM format");

        var baseInstallment = Math.Round(principal / totalInstallments, 2, MidpointRounding.AwayFromZero);
        var lines = new List<InstallmentLine>();
        var remaining = principal;
        var firstPeriod = new DateOnly(year, month, 1);

        for (var i = 1; i <= totalInstallments; i++)
        {
            var amount = i == totalInstallments ? remaining : baseInstallment;
            remaining -= amount;

            var period = firstPeriod.AddMonths(i - 1);
            lines.Add(new InstallmentLine(i, $"{period.Year:D4}{period.Month:D2}", amount, remaining));
        }

        return lines;
    }
}
