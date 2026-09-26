using HRM.Services.Pay.Calculators;
using Xunit;

namespace HRM.Tests.Pay;

public class SsoCoverageTests
{
    private static readonly DateTime Born = new(1965, 3, 10);   // 60th birthday = 2025-03-10
    private static readonly DateOnly Period = new(2026, 1, 31);
    private static readonly List<SsoCoverage.Override> None = new();

    [Fact]
    public void Hired_before_60_is_insured_and_stays_insured_after_60()
    {
        var r = SsoCoverage.Resolve(Born, new DateTime(2010, 1, 1), 60, None, Period);
        Assert.True(r.IsInsured);
        Assert.False(r.FromOverride);
    }

    [Fact]
    public void Hired_on_the_60th_birthday_is_still_not_over_age()
    {
        Assert.True(SsoCoverage.Resolve(Born, new DateTime(2025, 3, 10), 60, None, Period).IsInsured);
    }

    [Fact]
    public void Hired_the_day_after_the_60th_birthday_is_not_insured()
    {
        var r = SsoCoverage.Resolve(Born, new DateTime(2025, 3, 11), 60, None, Period);
        Assert.False(r.IsInsured);
        Assert.Contains("ม.33", r.Reason);
    }

    [Fact]
    public void Unknown_birth_or_start_date_defaults_to_insured()
    {
        Assert.True(SsoCoverage.Resolve(null, new DateTime(2025, 6, 1), 60, None, Period).IsInsured);
        Assert.True(SsoCoverage.Resolve(Born, null, 60, None, Period).IsInsured);
    }

    [Fact]
    public void Configured_age_limit_is_used()
    {
        // hired at 61 — over a 60 limit, under a 65 limit
        var hired = new DateTime(2026, 4, 1);
        Assert.False(SsoCoverage.Resolve(Born, hired, 60, None, new DateOnly(2026, 4, 30)).IsInsured);
        Assert.True(SsoCoverage.Resolve(Born, hired, 65, None, new DateOnly(2026, 4, 30)).IsInsured);
    }

    [Fact]
    public void Hr_override_beats_the_age_rule_both_ways()
    {
        var over60 = new DateTime(2025, 6, 1);
        var previouslyInsured = new List<SsoCoverage.Override> { new(1, true, new DateOnly(2025, 6, 1), null, "เคยเป็นผู้ประกันตน ม.33") };
        var r = SsoCoverage.Resolve(Born, over60, 60, previouslyInsured, Period);
        Assert.True(r.IsInsured);
        Assert.True(r.FromOverride);
        Assert.Contains("เคยเป็นผู้ประกันตน", r.Reason);

        var exempt = new List<SsoCoverage.Override> { new(2, false, new DateOnly(2025, 1, 1), null, "ยกเว้นตาม ม.4") };
        Assert.False(SsoCoverage.Resolve(Born, new DateTime(2010, 1, 1), 60, exempt, Period).IsInsured);
    }

    [Fact]
    public void Override_applies_only_inside_its_dates_and_latest_wins()
    {
        var overrides = new List<SsoCoverage.Override>
        {
            new(1, false, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), null),
            new(2, true, new DateOnly(2026, 3, 1), null, null),
        };
        var hiredYoung = new DateTime(2010, 1, 1);
        Assert.False(SsoCoverage.Resolve(Born, hiredYoung, 60, overrides, new DateOnly(2025, 6, 30)).IsInsured);
        // gap between the two rows: back to the age rule (hired young = insured)
        var gap = SsoCoverage.Resolve(Born, hiredYoung, 60, overrides, new DateOnly(2026, 1, 31));
        Assert.True(gap.IsInsured);
        Assert.False(gap.FromOverride);
        Assert.True(SsoCoverage.Resolve(Born, hiredYoung, 60, overrides, new DateOnly(2026, 3, 31)).FromOverride);
    }
}
