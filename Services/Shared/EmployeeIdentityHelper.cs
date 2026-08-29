namespace HRM.Services.Shared;

using System.Text.RegularExpressions;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

public enum IdCardMatchResult { NoMatch, MatchesActiveEmployee, MatchesDepartedEmployee }
public record IdCardCheckResult(IdCardMatchResult Result, Hremployee? Matched);

// Single source of truth for "is this ID card already on file" (checked
// before every new-employee creation, whether manual via
// PayrollEmployeeAdmin.razor or automatic via RecOfferService — both used to
// have zero identity checking, meaning the same real person could end up
// with two Hremployee rows) and for EmpNo generation (previously duplicated
// near-verbatim in both of those call sites — this is now the one
// implementation both call).
public static class EmployeeIdentityHelper
{
    public static async Task<IdCardCheckResult> CheckAsync(HRMContext context, string idCard, CancellationToken ct = default)
    {
        var matches = await context.Hremployee.Where(e => e.IdCard == idCard).ToListAsync(ct);

        var active = matches.FirstOrDefault(EmployeeStatusHelper.CanTransact);
        if (active is not null)
            return new IdCardCheckResult(IdCardMatchResult.MatchesActiveEmployee, active);

        var departed = matches.FirstOrDefault(EmployeeStatusHelper.HasDeparted);
        return departed is not null
            ? new IdCardCheckResult(IdCardMatchResult.MatchesDepartedEmployee, departed)
            : new IdCardCheckResult(IdCardMatchResult.NoMatch, null);
    }

    // Read-only preview of the next EmpNo — no side effects, safe to call
    // just to show a suggested value (e.g. PayrollEmployeeAdmin.razor's
    // OpenCreate, before the admin has decided whether to keep it or type
    // their own). Does NOT advance the stateful counter — the caller decides
    // when/whether to actually reserve the number (see GenerateNextEmpNoAsync
    // below, or PayrollEmployeeAdmin.razor's own increment-on-save-if-not-
    // unlocked logic).
    public static async Task<string> PeekNextEmpNoAsync(HRMContext context, string? companyId, CancellationToken ct = default)
    {
        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        var prefix = settings?.EmpCodePrefix ?? string.Empty;
        var digits = settings is null || settings.EmpCodeDigits < 1 ? 3 : settings.EmpCodeDigits;

        var next = settings?.EmpCodeNextNumber is int statefulNext
            ? statefulNext
            : await ScanMaxExistingNumberAsync(context, prefix, ct) + 1;

        return prefix + next.ToString(new string('0', digits));
    }

    // Atomic generate-and-reserve — same formula PayrollEmployeeAdmin.razor
    // and RecOfferService.cs each implemented separately before this.
    // Increments the stateful counter (if configured) as a side effect, so
    // only call this at the point a new employee is actually about to be
    // persisted (e.g. RecOfferService.ConfirmHireAsync) — never just to show
    // a preview (use PeekNextEmpNoAsync for that). Caller must have already
    // opened `context` for the transaction that will actually persist the
    // new employee, since this may call SaveChangesAsync itself for the
    // counter increment.
    public static async Task<string> GenerateNextEmpNoAsync(HRMContext context, string? companyId, CancellationToken ct = default)
    {
        var settings = await context.Pay_PayslipSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        var prefix = settings?.EmpCodePrefix ?? string.Empty;
        var digits = settings is null || settings.EmpCodeDigits < 1 ? 3 : settings.EmpCodeDigits;

        int next;
        if (settings?.EmpCodeNextNumber is int statefulNext)
        {
            next = statefulNext;
        }
        else
        {
            next = await ScanMaxExistingNumberAsync(context, prefix, ct) + 1;
        }

        var candidate = prefix + next.ToString(new string('0', digits));
        if (await context.Hremployee.AnyAsync(e => e.EmpNo == candidate, ct))
        {
            // Extremely unlikely race with the stateful counter path — fall
            // back to scanning for real rather than crashing the caller.
            next = await ScanMaxExistingNumberAsync(context, prefix, ct) + 1;
            candidate = prefix + next.ToString(new string('0', digits));
        }

        if (settings?.EmpCodeNextNumber is not null)
        {
            settings.EmpCodeNextNumber++;
            await context.SaveChangesAsync(ct);
        }

        return candidate;
    }

    private static async Task<int> ScanMaxExistingNumberAsync(HRMContext context, string prefix, CancellationToken ct)
    {
        var existingNumbers = await context.Hremployee.Where(e => e.EmpNo.StartsWith(prefix)).Select(e => e.EmpNo).ToListAsync(ct);
        var pattern = $"^{Regex.Escape(prefix)}(\\d+)$";
        return existingNumbers
            .Select(no => Regex.Match(no, pattern))
            .Where(m => m.Success)
            .Select(m => int.Parse(m.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();
    }
}
