namespace HRM.Services.Pay;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Reads Pay_EmployeeDocument.ExpiryDate (added alongside the WorkPermit/Visa/
// DrivingLicense/ProfessionalLicense document types) and classifies each row
// as Expired / ExpiringSoon / Normal relative to today. No scheduler in this
// codebase — this is read-on-demand for the HR dashboard, not a background
// alert job (confirmed with user: dashboard/list only, no proactive email).
public class DocumentExpiryService(IDbContextFactory<HRMContext> dbFactory)
{
    public enum ExpiryStatus { Expired, ExpiringSoon, Normal }

    public record ExpiryRow(
        long DocumentId,
        long HremployeeId,
        string EmpNo,
        string EmpName,
        Pay_EmployeeDocumentType DocumentType,
        DateOnly ExpiryDate,
        int DaysRemaining,
        ExpiryStatus Status);

    public async Task<List<ExpiryRow>> GetExpiringDocumentsAsync(string companyId, int daysAhead, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(daysAhead);

        var docs = await context.Pay_EmployeeDocuments
            .Include(d => d.Hremployee)
            .Where(d => d.ExpiryDate != null
                && d.Hremployee.companyid == companyId
                // Always include already-expired docs regardless of the
                // day-ahead filter — those are the most urgent and
                // shouldn't disappear just because the filter window is small.
                && d.ExpiryDate <= cutoff)
            .OrderBy(d => d.ExpiryDate)
            .ToListAsync(ct);

        return docs.Select(d =>
        {
            var expiry = d.ExpiryDate!.Value;
            var daysRemaining = expiry.DayNumber - today.DayNumber;
            var status = daysRemaining < 0 ? ExpiryStatus.Expired
                : daysRemaining <= 30 ? ExpiryStatus.ExpiringSoon
                : ExpiryStatus.Normal;
            return new ExpiryRow(
                d.Id,
                d.HremployeeId,
                d.Hremployee.EmpNo,
                $"{d.Hremployee.EmpName} {d.Hremployee.EmpSurname}".Trim(),
                d.DocumentType,
                expiry,
                daysRemaining,
                status);
        }).ToList();
    }
}
