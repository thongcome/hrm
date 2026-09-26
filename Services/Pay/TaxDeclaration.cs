using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Pay;

// One save rule for a personal-income-tax deduction the employee declares (แบบ ล.ย.01), used by
// both the HR page (/pay/admin/employee-tax-deductions) and the employee's own page
// (/ess/tax-declaration). The engine (TaxDeductionRules) still applies the % and group caps at
// calculation time; this only refuses what is plainly invalid when the row is entered.
public static class TaxDeclaration
{
    public static string? Validate(Pay_TaxDeductionType type, decimal amount, int? personCount, decimal alreadyAmount, int alreadyPersons)
    {
        switch (type.CalcMethod)
        {
            case TaxDeductionCalcMethod.PerPerson:
                if (personCount is not int n || n <= 0) return "กรุณาระบุจำนวนคน";
                if (type.MaxPersons is int max && alreadyPersons + n > max)
                    return alreadyPersons > 0
                        ? $"ประเภทนี้แจ้งไว้แล้ว {alreadyPersons} คน รวมแล้วเกิน {max} คน"
                        : $"ประเภทนี้นับได้ไม่เกิน {max} คน";
                return null;
            default:
                if (amount <= 0m) return "กรุณาระบุจำนวนเงินที่จ่ายจริง";
                if (type.MaxAmountPerYear > 0m && alreadyAmount + amount > type.MaxAmountPerYear)
                    return alreadyAmount > 0
                        ? $"ประเภทนี้แจ้งไว้แล้ว {alreadyAmount:N2} บาท รวมกับรายการใหม่จะเกินเพดาน ({type.MaxAmountPerYear:N2} บาท/ปี)"
                        : $"จำนวนเกินเพดานของประเภทนี้ ({type.MaxAmountPerYear:N2} บาท/ปี)";
                return null;
        }
    }

    /// <summary>Validates against what the employee already declared for this type and adds the row (caller saves).</summary>
    public static async Task<string?> AddAsync(HRMContext context, long hremployeeId, Pay_TaxDeductionType type, decimal amount, int? personCount,
        bool applyMonthly, string? note, long actorUserId, bool declaredByEmployee, CancellationToken ct = default)
    {
        var existing = await context.Pay_EmployeeTaxDeductionElections
            .Where(e => e.HremployeeId == hremployeeId && e.DeductionTypeId == type.Id && e.IsActive)
            .Select(e => new { e.AnnualAmount, e.PersonCount })
            .ToListAsync(ct);
        if (Validate(type, amount, personCount, existing.Sum(e => e.AnnualAmount), existing.Sum(e => e.PersonCount ?? 0)) is string problem)
            return problem;

        var perPerson = type.CalcMethod == TaxDeductionCalcMethod.PerPerson;
        context.Pay_EmployeeTaxDeductionElections.Add(new Pay_EmployeeTaxDeductionElection
        {
            HremployeeId = hremployeeId,
            DeductionTypeId = type.Id,
            // per-person: the amount is informational — the engine recomputes it from the year's rate
            AnnualAmount = perPerson ? (personCount ?? 0) * (type.AmountPerPerson ?? 0m) : amount,
            PersonCount = perPerson ? personCount : null,
            ApplyMonthly = applyMonthly,
            Note = note,
            ElectedByUserId = actorUserId,
            DeclaredByEmployee = declaredByEmployee,
        });
        return null;
    }
}
