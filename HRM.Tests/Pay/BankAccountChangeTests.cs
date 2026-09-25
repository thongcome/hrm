using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

public class BankAccountChangeTests
{
    [Fact]
    public void Same_account_and_bank_is_not_flagged_even_with_different_formatting()
    {
        Assert.Null(PayrollAnomalyDetectionService.DescribeBankAccountChange("004", "123-4-56789-0", "004", "1234567890"));
    }

    [Fact]
    public void Changed_account_is_flagged_showing_only_last_four_digits()
    {
        var text = PayrollAnomalyDetectionService.DescribeBankAccountChange("004", "1234567890", "004", "9998887776");
        Assert.NotNull(text);
        Assert.Contains("xxx7890", text);
        Assert.Contains("xxx7776", text);
        Assert.DoesNotContain("1234567890", text);
        Assert.DoesNotContain("9998887776", text);
    }

    [Fact]
    public void Same_number_at_a_different_bank_is_flagged()
    {
        Assert.NotNull(PayrollAnomalyDetectionService.DescribeBankAccountChange("004", "1234567890", "014", "1234567890"));
    }

    [Fact]
    public void Account_removed_is_flagged()
    {
        var text = PayrollAnomalyDetectionService.DescribeBankAccountChange("004", "1234567890", "004", null);
        Assert.NotNull(text);
        Assert.Contains("ไม่มีเลขบัญชี", text);
    }

    [Fact]
    public void First_account_ever_is_not_flagged()
    {
        Assert.Null(PayrollAnomalyDetectionService.DescribeBankAccountChange(null, null, "004", "1234567890"));
    }
}
