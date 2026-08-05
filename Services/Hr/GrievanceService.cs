using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Hr;

// Grievance/complaint pipeline: employee (optionally anonymous) submits a complaint,
// HR assigns an investigator, then records a resolution. No approval workflow here —
// unlike disciplinary cases, a grievance isn't a decision that needs sign-off, it's a
// case HR works and closes; Status is tracked directly on the row instead of derived
// from job_masters.
public class GrievanceService
{
    private readonly IDbContextFactory<HRMContext> _dbFactory;

    public GrievanceService(IDbContextFactory<HRMContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<long> SubmitAsync(long reporterHremployeeId, string companyId, bool isAnonymous, GrievanceCategory category,
        string subject, string description, DateOnly? incidentDate, string? involvedPersons, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(subject)) throw new InvalidOperationException("กรุณาระบุหัวข้อ");
        if (string.IsNullOrWhiteSpace(description)) throw new InvalidOperationException("กรุณาระบุรายละเอียด");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var grievance = new Hr_Grievance
        {
            ReporterHremployeeId = isAnonymous ? null : reporterHremployeeId,
            IsAnonymous = isAnonymous,
            CompanyId = companyId,
            Category = category,
            Subject = subject,
            Description = description,
            IncidentDate = incidentDate,
            InvolvedPersons = involvedPersons,
            Status = GrievanceStatus.Submitted,
        };
        context.Hr_Grievances.Add(grievance);
        await context.SaveChangesAsync(ct);
        return grievance.Id;
    }

    public async Task AssignAsync(long grievanceId, long investigatorUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var g = await context.Hr_Grievances.FirstOrDefaultAsync(x => x.Id == grievanceId, ct)
            ?? throw new InvalidOperationException("ไม่พบเรื่องร้องเรียนนี้");
        g.AssignedToUserId = investigatorUserId;
        if (g.Status == GrievanceStatus.Submitted)
            g.Status = GrievanceStatus.UnderInvestigation;
        await context.SaveChangesAsync(ct);
    }

    public async Task ResolveAsync(long grievanceId, string resolutionNotes, bool dismissed, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotes))
            throw new InvalidOperationException("กรุณาระบุผลการดำเนินการ");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var g = await context.Hr_Grievances.FirstOrDefaultAsync(x => x.Id == grievanceId, ct)
            ?? throw new InvalidOperationException("ไม่พบเรื่องร้องเรียนนี้");
        g.ResolutionNotes = resolutionNotes;
        g.ResolvedDate = DateTime.Now;
        g.Status = dismissed ? GrievanceStatus.Dismissed : GrievanceStatus.Resolved;
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Hr_Grievance>> GetAllAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Hr_Grievances
            .Where(g => g.CompanyId == companyId)
            .OrderByDescending(g => g.Id)
            .ToListAsync(ct);
    }

    // Anonymous submissions never show up here — there is no reporter link to
    // filter by, by design (see IsAnonymous handling in SubmitAsync).
    public async Task<List<Hr_Grievance>> GetMineAsync(long reporterHremployeeId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Hr_Grievances
            .Where(g => g.ReporterHremployeeId == reporterHremployeeId)
            .OrderByDescending(g => g.Id)
            .ToListAsync(ct);
    }

    public static string StatusLabel(GrievanceStatus status) => status switch
    {
        GrievanceStatus.Submitted => "รับเรื่องแล้ว",
        GrievanceStatus.UnderInvestigation => "อยู่ระหว่างตรวจสอบ",
        GrievanceStatus.Resolved => "ดำเนินการแล้วเสร็จ",
        GrievanceStatus.Dismissed => "ยุติเรื่อง (ไม่เข้าเงื่อนไข)",
        _ => status.ToString(),
    };

    public static string CategoryLabel(GrievanceCategory category) => category switch
    {
        GrievanceCategory.Harassment => "การล่วงละเมิด",
        GrievanceCategory.Discrimination => "การเลือกปฏิบัติ",
        GrievanceCategory.WorkingConditions => "สภาพการทำงาน",
        GrievanceCategory.Compensation => "ค่าตอบแทน",
        GrievanceCategory.Management => "การบริหารจัดการ",
        GrievanceCategory.Other => "อื่นๆ",
        _ => category.ToString(),
    };
}
