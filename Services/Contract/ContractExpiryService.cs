namespace HRM.Services.Contract;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Turns the 4 expiry dates already sitting on CT_Contract (contract itself,
// warranty, bank guarantee, insurance policy) into something actionable —
// before this, they were fields nobody looked at until someone remembered to
// check manually. Same read-on-demand idiom as DocumentExpiryService (no
// scheduler in this codebase; classification is computed live, never stored,
// so it can never drift from the underlying dates).
public class ContractExpiryService(IDbContextFactory<HRMContext> dbFactory)
{
    public enum ExpiryStatus { Expired, ExpiringSoon, Normal }
    public enum ExpiryKind { Contract, Warranty, BankGuarantee, InsurancePolicy }

    public record ExpiryRow(
        long ContractId,
        string PoNo,
        string? ContractNo,
        string? VendorName,
        ExpiryKind Kind,
        DateOnly ExpiryDate,
        int DaysRemaining,
        ExpiryStatus Status);

    public async Task<List<ExpiryRow>> GetExpiringAsync(int daysAhead, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(daysAhead);

        var contracts = await context.CT_Contracts.Where(c => c.isActive).ToListAsync(ct);

        var rows = new List<ExpiryRow>();
        void AddIfRelevant(CT_Contract c, DateOnly? date, ExpiryKind kind)
        {
            if (date is null || date.Value > cutoff) return;
            var daysRemaining = date.Value.DayNumber - today.DayNumber;
            var status = daysRemaining < 0 ? ExpiryStatus.Expired
                : daysRemaining <= 30 ? ExpiryStatus.ExpiringSoon
                : ExpiryStatus.Normal;
            rows.Add(new ExpiryRow(c.id, c.po_no, c.constract_no, c.vendor_name, kind, date.Value, daysRemaining, status));
        }

        foreach (var c in contracts)
        {
            AddIfRelevant(c, c.expired_date, ExpiryKind.Contract);
            if (c.isWarrantyRequired == true) AddIfRelevant(c, c.WarrantyEnddate, ExpiryKind.Warranty);
            if (c.isBankGuarantee == true) AddIfRelevant(c, c.BankGuaranteeExpiryDate, ExpiryKind.BankGuarantee);
            if (c.isInsurancePolicyRequired == true) AddIfRelevant(c, c.InsurancePolicyExpiryDate, ExpiryKind.InsurancePolicy);
        }

        return rows.OrderBy(r => r.ExpiryDate).ToList();
    }

    // Static, pure classification helper — used by CTContractAdmin.razor's list
    // view to show a live status badge instead of the bare isActive boolean.
    public static (ExpiryStatus? Status, int? DaysRemaining) ClassifyContractExpiry(DateOnly? expiredDate)
    {
        if (expiredDate is null) return (null, null);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysRemaining = expiredDate.Value.DayNumber - today.DayNumber;
        var status = daysRemaining < 0 ? ExpiryStatus.Expired
            : daysRemaining <= 30 ? ExpiryStatus.ExpiringSoon
            : ExpiryStatus.Normal;
        return (status, daysRemaining);
    }

    public async Task<List<CT_ContractRenewal>> GetRenewalHistoryAsync(long contractId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.CT_ContractRenewals
            .Where(r => r.ContractId == contractId)
            .OrderByDescending(r => r.RenewedDate)
            .ToListAsync(ct);
    }

    public async Task RenewContractAsync(long contractId, DateOnly newExpiredDate, string? note, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var contract = await context.CT_Contracts.FirstOrDefaultAsync(c => c.id == contractId, ct)
            ?? throw new InvalidOperationException("ไม่พบสัญญานี้");

        context.CT_ContractRenewals.Add(new CT_ContractRenewal
        {
            ContractId = contractId,
            OldExpiredDate = contract.expired_date,
            NewExpiredDate = newExpiredDate,
            Note = note,
            RenewedByUserId = actorUserId,
        });

        contract.expired_date = newExpiredDate;
        contract.moddate = DateTime.Now;

        await context.SaveChangesAsync(ct);
    }
}
