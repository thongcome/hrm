using HRM.Services.Pay;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HRM.Tests.Pay;

public class PayrollSeparationOfDutiesTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))).Build();

    [Fact]
    public void Approval_stays_separated_but_steps_after_approval_do_not_need_a_third_person_by_default()
    {
        Assert.True(PayrollSeparationOfDuties.IsRequired(null));
        Assert.False(PayrollSeparationOfDuties.IsRequiredAfterApproval(null));
        Assert.True(PayrollSeparationOfDuties.IsRequiredAfterApproval(Config(("Payroll:SeparateFinalizer", "true"))));
        // the dev/demo switch that turns separation off also turns the finalizer rule off
        Assert.False(PayrollSeparationOfDuties.IsRequiredAfterApproval(Config(
            ("Payroll:SeparateFinalizer", "true"), ("Payroll:RequireSeparateApprover", "false"))));
    }
}
