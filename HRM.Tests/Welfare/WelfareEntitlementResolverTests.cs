using HRM.Models;
using HRM.Services.Welfare;
using Xunit;

namespace HRM.Tests.Welfare;

// The welfare entitlement layer lets the same benefit differ by person
// (company default → position → individual). WelfareEntitlementResolver.Pick is
// the pure most-specific-wins resolution the whole feature leans on — get it
// wrong and someone is granted the wrong amount, or an individual's special
// case is silently ignored. These pin every layer and the per-field inheritance.
public class WelfareEntitlementResolverTests
{
    private static Wel_BenefitType Benefit(WelfareEntitlementMode mode = WelfareEntitlementMode.AnnualAmount,
        decimal? annual = 3000, decimal? perEvent = null, int? maxClaims = null)
        => new() { Id = 1, EntitlementMode = mode, AnnualLimitAmount = annual, PerEventLimitAmount = perEvent, MaxClaimsPerYear = maxClaims };

    private static Wel_Entitlement Rule(WelfareEntitlementScope scope, decimal? amount = null, int? maxClaims = null,
        long? pos = null, long? emp = null, string? note = null)
        => new() { Scope = scope, OverrideAmount = amount, OverrideMaxClaimsPerYear = maxClaims, PosExecTypeId = pos, HremployeeId = emp, Note = note, IsActive = true };

    private const long Emp = 500;
    private const long Pos = 4;

    [Fact]
    public void No_rules_falls_back_to_benefit_default()
    {
        var eff = WelfareEntitlementResolver.Pick(Benefit(annual: 3000), new List<Wel_Entitlement>(), Pos, Emp);
        Assert.Equal(3000, eff.Amount);
        Assert.Equal(WelfareEntitlementScope.All, eff.SourceScope);
    }

    [Fact]
    public void Position_rule_overrides_the_company_default()
    {
        // ค่ารถ: default 3,000, but managers (this position) get 8,000.
        var rules = new List<Wel_Entitlement> { Rule(WelfareEntitlementScope.Position, amount: 8000, pos: Pos) };
        var eff = WelfareEntitlementResolver.Pick(Benefit(annual: 3000), rules, Pos, Emp);
        Assert.Equal(8000, eff.Amount);
        Assert.Equal(WelfareEntitlementScope.Position, eff.SourceScope);
    }

    [Fact]
    public void Position_rule_does_not_apply_to_a_different_position()
    {
        var rules = new List<Wel_Entitlement> { Rule(WelfareEntitlementScope.Position, amount: 8000, pos: 99) };
        var eff = WelfareEntitlementResolver.Pick(Benefit(annual: 3000), rules, Pos, Emp);
        Assert.Equal(3000, eff.Amount); // fell through to default
        Assert.Equal(WelfareEntitlementScope.All, eff.SourceScope);
    }

    [Fact]
    public void Employee_rule_beats_position_and_default()
    {
        // Individual special case wins over everything.
        var rules = new List<Wel_Entitlement>
        {
            Rule(WelfareEntitlementScope.All, amount: 3000),
            Rule(WelfareEntitlementScope.Position, amount: 8000, pos: Pos),
            Rule(WelfareEntitlementScope.Employee, amount: 15000, emp: Emp, note: "รถประจำตำแหน่งพิเศษ"),
        };
        var eff = WelfareEntitlementResolver.Pick(Benefit(annual: 3000), rules, Pos, Emp);
        Assert.Equal(15000, eff.Amount);
        Assert.Equal(WelfareEntitlementScope.Employee, eff.SourceScope);
        Assert.Equal("รถประจำตำแหน่งพิเศษ", eff.SourceNote);
    }

    [Fact]
    public void Employee_rule_for_someone_else_is_ignored()
    {
        var rules = new List<Wel_Entitlement>
        {
            Rule(WelfareEntitlementScope.Position, amount: 8000, pos: Pos),
            Rule(WelfareEntitlementScope.Employee, amount: 15000, emp: 999),
        };
        var eff = WelfareEntitlementResolver.Pick(Benefit(annual: 3000), rules, Pos, Emp);
        Assert.Equal(8000, eff.Amount); // this employee gets the position amount, not the other person's special
        Assert.Equal(WelfareEntitlementScope.Position, eff.SourceScope);
    }

    [Fact]
    public void Null_override_field_inherits_from_less_specific_level()
    {
        // Position rule overrides only the claim-count cap, leaving the amount to
        // inherit the company All rule's amount.
        var rules = new List<Wel_Entitlement>
        {
            Rule(WelfareEntitlementScope.All, amount: 5000, maxClaims: 4),
            Rule(WelfareEntitlementScope.Position, amount: null, maxClaims: 2, pos: Pos),
        };
        var eff = WelfareEntitlementResolver.Pick(Benefit(annual: 3000, maxClaims: 10), rules, Pos, Emp);
        Assert.Equal(5000, eff.Amount);       // inherited from All (Position amount was null)
        Assert.Equal(2, eff.MaxClaimsPerYear); // overridden by Position
    }

    [Fact]
    public void Employee_with_no_position_still_resolves_default_and_own_rule()
    {
        // posExecTypeId null (no active position slot) — Position rules can't
        // match, but an Employee rule and the default still apply.
        var rules = new List<Wel_Entitlement>
        {
            Rule(WelfareEntitlementScope.Position, amount: 8000, pos: Pos),
            Rule(WelfareEntitlementScope.Employee, amount: 12000, emp: Emp),
        };
        var eff = WelfareEntitlementResolver.Pick(Benefit(annual: 3000), rules, posExecTypeId: null, Emp);
        Assert.Equal(12000, eff.Amount);
        Assert.Equal(WelfareEntitlementScope.Employee, eff.SourceScope);
    }

    [Fact]
    public void PerEvent_mode_uses_the_per_event_default_as_baseline()
    {
        var eff = WelfareEntitlementResolver.Pick(
            Benefit(mode: WelfareEntitlementMode.PerEventAmount, annual: null, perEvent: 2000, maxClaims: 1),
            new List<Wel_Entitlement>(), Pos, Emp);
        Assert.Equal(2000, eff.Amount);
        Assert.Equal(1, eff.MaxClaimsPerYear);
    }
}
