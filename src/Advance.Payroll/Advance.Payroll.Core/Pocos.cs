namespace Advance.Payroll.Core;

// Trimmed, EF-free stand-ins for the Advance.Payroll.Domain entities these calculators read.
// Only the properties actually referenced by a calculator in this project are copied — see
// EXTRACTION-PLAN.md "Core POCO shapes" for the source table + full-column reference.
// Advance.Payroll.Engine is responsible for mapping the real EF entity -> this shape at the
// call site (a couple of lines per call; no implicit operator is provided on purpose so this
// project's dependency graph stays honest — zero reference to Domain or EF Core).

// รอบจ่ายใช้กับพนักงานกลุ่มไหน (Pay_PaySchedule.AppliesTo) — mirrors Model/Pay_Enums.cs's PayScheduleGroup 1:1.
public enum PayScheduleGroup
{
    MonthlySalaried = 0,
    DailyWage = 1,
    All = 2,
}

// From Model/Pay_TaxBracket.cs — only the columns TaxBracketCalculator reads.
public sealed class TaxBracket
{
    public int Step { get; set; }
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal RatePercent { get; set; }
    public bool IsActive { get; set; } = true;
}

// From Model/Pay_PaySchedule.cs — only the columns PayScheduleResolver reads.
public sealed class PaySchedule
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public int PeriodsPerMonth { get; set; } = 1;
    public int SecondTermStartDay { get; set; } = 16;
    public PayScheduleGroup AppliesTo { get; set; } = PayScheduleGroup.All;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

// From Model/Pay_EmployeePayScheduleOverride.cs — only the columns PayScheduleResolver reads.
public sealed class EmployeePayScheduleOverride
{
    public long HremployeeId { get; set; }
    public long PayScheduleId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

// From Model/Pay_ProvidentFundVestingTier.cs — only the columns ProvidentFundVestingCalculator reads.
public sealed class ProvidentFundVestingTier
{
    public int MinYearsOfService { get; set; }
    public int? MaxYearsOfService { get; set; }
    public decimal VestingPercent { get; set; }
}
