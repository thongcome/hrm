using HRM.Models;
using HRM.Services.Shared;
using Xunit;

namespace HRM.Tests.Shared;

// Comp_ProficiencyLevel is effective-dated — revising a BARS anchor's wording appends a new row
// rather than editing the old one, so an evaluation scored last year still shows the wording the
// rater actually saw. These tests are about the "which row is current" resolution only.
public class ProficiencyLevelResolverTests
{
    private static Comp_ProficiencyLevel Row(long compId, int level, string desc, int y, int m, int d, bool active = true, (int y, int m, int d)? to = null) =>
        new()
        {
            CompetencyId = compId, Level = level, Description = desc,
            EffectiveFrom = new DateOnly(y, m, d), IsActive = active,
            EffectiveTo = to is { } t ? new DateOnly(t.y, t.m, t.d) : null,
        };

    [Fact]
    public void No_rows_means_no_current_description()
    {
        Assert.Null(ProficiencyLevelResolver.Current(Array.Empty<Comp_ProficiencyLevel>(), 1, 3, new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Latest_version_not_after_the_date_wins()
    {
        var rows = new[]
        {
            Row(1, 3, "เก่า", 2020, 1, 1),
            Row(1, 3, "ใหม่กว่า", 2026, 6, 1),
            Row(1, 3, "อนาคต", 2027, 1, 1),
        };
        Assert.Equal("เก่า", ProficiencyLevelResolver.Current(rows, 1, 3, new DateOnly(2025, 1, 1))!.Description);
        Assert.Equal("ใหม่กว่า", ProficiencyLevelResolver.Current(rows, 1, 3, new DateOnly(2026, 6, 1))!.Description);
        Assert.Equal("ใหม่กว่า", ProficiencyLevelResolver.Current(rows, 1, 3, new DateOnly(2026, 12, 31))!.Description);
        Assert.Equal("อนาคต", ProficiencyLevelResolver.Current(rows, 1, 3, new DateOnly(2027, 6, 1))!.Description);
    }

    [Fact]
    public void Inactive_and_expired_rows_are_ignored()
    {
        var rows = new[]
        {
            Row(1, 3, "ปิดใช้งาน", 2026, 1, 1, active: false),
            Row(1, 3, "หมดอายุ", 2020, 1, 1, to: (2025, 12, 31)),
            Row(1, 3, "ใช้อยู่", 2020, 1, 1),
        };
        Assert.Equal("ใช้อยู่", ProficiencyLevelResolver.Current(rows, 1, 3, new DateOnly(2026, 6, 1))!.Description);
    }

    [Fact]
    public void Different_competencies_and_levels_do_not_bleed_into_each_other()
    {
        var rows = new[]
        {
            Row(1, 3, "สมรรถนะ1 ระดับ3", 2020, 1, 1),
            Row(1, 4, "สมรรถนะ1 ระดับ4", 2020, 1, 1),
            Row(2, 3, "สมรรถนะ2 ระดับ3", 2020, 1, 1),
        };
        Assert.Equal("สมรรถนะ1 ระดับ3", ProficiencyLevelResolver.Current(rows, 1, 3, new DateOnly(2026, 1, 1))!.Description);
        Assert.Equal("สมรรถนะ1 ระดับ4", ProficiencyLevelResolver.Current(rows, 1, 4, new DateOnly(2026, 1, 1))!.Description);
        Assert.Equal("สมรรถนะ2 ระดับ3", ProficiencyLevelResolver.Current(rows, 2, 3, new DateOnly(2026, 1, 1))!.Description);
    }

    [Fact]
    public void CurrentOnly_returns_exactly_one_row_per_competency_and_level()
    {
        var rows = new[]
        {
            Row(1, 3, "เก่า", 2020, 1, 1),
            Row(1, 3, "ใหม่", 2026, 1, 1),
            Row(1, 4, "อีกระดับ", 2020, 1, 1),
        };
        var current = ProficiencyLevelResolver.CurrentOnly(rows, new DateOnly(2026, 6, 1));
        Assert.Equal(2, current.Count);
        Assert.Equal("ใหม่", current.Single(l => l.Level == 3).Description);
        Assert.Equal("อีกระดับ", current.Single(l => l.Level == 4).Description);
    }
}
