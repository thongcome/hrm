namespace HRM.Services.Engagement;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Peer-to-peer kudos feed — unlike survey responses, recognition is
// intentionally attributed (see Eng_Recognition's comment), so this service
// deals in real employee identities throughout.
public class RecognitionService(IDbContextFactory<HRMContext> dbFactory)
{
    // Points a kudos grants the receiver by default (spendable in the redeem
    // catalog). Kept as a flat, code-level default so recognition stays simple;
    // the form may pass a different value within reason.
    public const int DefaultKudosPoints = 10;

    public async Task GiveAsync(string companyId, long fromHremployeeId, long toHremployeeId, string message, string? coreValueTag, int? points = null, CancellationToken ct = default)
    {
        if (fromHremployeeId == toHremployeeId)
            throw new InvalidOperationException("ไม่สามารถให้ kudos ตัวเองได้");
        var pts = points ?? DefaultKudosPoints;
        if (pts < 0) throw new InvalidOperationException("คะแนนต้องไม่ติดลบ");

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        context.Eng_Recognitions.Add(new Eng_Recognition
        {
            CompanyId = companyId,
            FromHremployeeId = fromHremployeeId,
            ToHremployeeId = toHremployeeId,
            Message = message,
            CoreValueTag = coreValueTag,
            Points = pts,
            CreatedDate = DateTime.Now,
            IsActive = true,
        });
        await context.SaveChangesAsync(ct);
    }

    public record RecognitionRow(long Id, string FromName, string ToName, string Message, string? CoreValueTag, int Points, DateTime CreatedDate);

    // Company-wide feed, newest first — the shared ESS/admin recognition view.
    public async Task<List<RecognitionRow>> GetFeedAsync(string companyId, int take = 50, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var items = await context.Eng_Recognitions
            .Where(r => r.CompanyId == companyId && r.IsActive)
            .OrderByDescending(r => r.CreatedDate)
            .Take(take)
            .ToListAsync(ct);
        if (items.Count == 0) return new();

        var empIds = items.Select(r => r.FromHremployeeId).Concat(items.Select(r => r.ToHremployeeId)).Distinct().ToList();
        var names = await context.Hremployee.Where(e => empIds.Contains(e.id))
            .ToDictionaryAsync(e => e.id, e => $"{e.EmpName} {e.EmpSurname}".Trim(), ct);

        return items.Select(r => new RecognitionRow(
            r.Id,
            names.GetValueOrDefault(r.FromHremployeeId, $"#{r.FromHremployeeId}"),
            names.GetValueOrDefault(r.ToHremployeeId, $"#{r.ToHremployeeId}"),
            r.Message, r.CoreValueTag, r.Points, r.CreatedDate)).ToList();
    }

    public async Task HideAsync(long recognitionId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var item = await context.Eng_Recognitions.FirstOrDefaultAsync(r => r.Id == recognitionId, ct);
        if (item is null) return;
        item.IsActive = false;
        await context.SaveChangesAsync(ct);
    }
}
