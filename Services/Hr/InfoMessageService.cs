namespace HRM.Services.Hr;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Core visibility + read-tracking logic for the info_message announcement
// module. GetVisibleAnnouncementsAsync is the single source of truth for
// "what can this employee see" — reused by both InfoMessageList.razor (the
// public page) and the EssHome.razor pinned-items widget, so the two
// surfaces can never drift apart on filtering rules.
public class InfoMessageService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<List<info_message>> GetVisibleAnnouncementsAsync(Hremployee employee, bool pinnedOnly = false, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await GetVisibleAnnouncementsAsync(context, employee, pinnedOnly, ct);
    }

    public async Task<List<info_message>> GetVisibleAnnouncementsAsync(HRMContext context, Hremployee employee, bool pinnedOnly, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var candidates = await context.info_messages
            .Where(m => m.CompanyId == employee.companyid
                && m.isactive == true
                && (m.startdate == null || m.startdate <= today)
                && (m.enddate == null || m.enddate >= today))
            .ToListAsync(ct);

        if (pinnedOnly)
            candidates = candidates.Where(m => m.IsPinned).ToList();

        if (candidates.Count == 0) return new();

        var ids = candidates.Select(m => m.Id).ToList();
        var targets = await context.Info_MessageTargets.Where(t => ids.Contains(t.InfoMessageId)).ToListAsync(ct);
        var targetsByMessage = targets.GroupBy(t => t.InfoMessageId).ToDictionary(g => g.Key, g => g.ToList());

        var orgIds = targets.Where(t => t.TargetOrganizationId != null).Select(t => t.TargetOrganizationId!.Value).Distinct().ToList();
        var orgById = orgIds.Count == 0
            ? new Dictionary<long, com_organization>()
            : (await context.com_organizations.Where(o => orgIds.Contains(o.id)).ToListAsync(ct)).ToDictionary(o => o.id);

        bool IsVisible(long messageId)
        {
            // No target rows at all -> visible to everyone (deliberate
            // fallback, not an error state — confirmed with the user).
            if (!targetsByMessage.TryGetValue(messageId, out var msgTargets) || msgTargets.Count == 0)
                return true;

            foreach (var t in msgTargets)
            {
                if (t.TargetType == InfoMessageTargetType.All) return true;
                if (t.TargetType == InfoMessageTargetType.Employee && t.TargetHremployeeId == employee.id) return true;
                if (t.TargetType == InfoMessageTargetType.Organization && t.TargetOrganizationId is long orgId
                    && orgById.TryGetValue(orgId, out var org) && !string.IsNullOrWhiteSpace(org.orgcodefull)
                    && !string.IsNullOrWhiteSpace(employee.orgcodefull) && employee.orgcodefull.StartsWith(org.orgcodefull))
                    return true;
            }
            return false;
        }

        return candidates.Where(m => IsVisible(m.Id))
            .OrderByDescending(m => m.IsPinned)
            .ThenByDescending(m => m.PriorityLevel)
            .ThenByDescending(m => m.startdate)
            .ToList();
    }

    public async Task MarkViewedAsync(long infoMessageId, long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        context.Info_MessageReadLogs.Add(new Info_MessageReadLog
        {
            InfoMessageId = infoMessageId,
            HremployeeId = hremployeeId,
            Action = InfoMessageReadAction.Viewed,
            EventDate = DateTime.Now,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task MarkDownloadedAsync(long infoMessageId, long hremployeeId, long docCenterId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        context.Info_MessageReadLogs.Add(new Info_MessageReadLog
        {
            InfoMessageId = infoMessageId,
            HremployeeId = hremployeeId,
            Action = InfoMessageReadAction.Downloaded,
            DocCenterId = docCenterId,
            EventDate = DateTime.Now,
        });
        await context.SaveChangesAsync(ct);
    }

    public record ReadStats(int TargetEmployeeCount, int ViewedCount, int DownloadedCount, List<Hremployee> NotYetViewed);

    public async Task<ReadStats> GetReadStatsAsync(long infoMessageId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var message = await context.info_messages.FirstOrDefaultAsync(m => m.Id == infoMessageId, ct)
            ?? throw new InvalidOperationException("ไม่พบประกาศนี้แล้ว");

        var targetEmployees = await ResolveTargetEmployeesAsync(context, infoMessageId, message.CompanyId, ct);

        var logs = await context.Info_MessageReadLogs.Where(l => l.InfoMessageId == infoMessageId).ToListAsync(ct);
        var viewedEmpIds = logs.Where(l => l.Action == InfoMessageReadAction.Viewed).Select(l => l.HremployeeId).Distinct().ToHashSet();
        var downloadedCount = logs.Count(l => l.Action == InfoMessageReadAction.Downloaded);

        return new ReadStats(
            targetEmployees.Count,
            viewedEmpIds.Count,
            downloadedCount,
            targetEmployees.Where(e => !viewedEmpIds.Contains(e.id)).ToList());
    }

    // Resolves an announcement's Info_MessageTarget rows into the actual list
    // of employees they cover — org targets expand to their full subtree.
    // Mirrors PerfAssignmentResolverService.ResolveTargetEmployeesAsync's shape.
    public async Task<List<Hremployee>> ResolveTargetEmployeesAsync(long infoMessageId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var message = await context.info_messages.FirstOrDefaultAsync(m => m.Id == infoMessageId, ct)
            ?? throw new InvalidOperationException("ไม่พบประกาศนี้แล้ว");
        return await ResolveTargetEmployeesAsync(context, infoMessageId, message.CompanyId, ct);
    }

    private static async Task<List<Hremployee>> ResolveTargetEmployeesAsync(HRMContext context, long infoMessageId, string companyId, CancellationToken ct)
    {
        var targets = await context.Info_MessageTargets.Where(t => t.InfoMessageId == infoMessageId).ToListAsync(ct);

        if (targets.Count == 0 || targets.Any(t => t.TargetType == InfoMessageTargetType.All))
            return await context.Hremployee.Where(e => e.companyid == companyId && e.ResignDate == null).ToListAsync(ct);

        var result = new Dictionary<long, Hremployee>();

        var directEmpIds = targets.Where(t => t.TargetType == InfoMessageTargetType.Employee && t.TargetHremployeeId != null)
            .Select(t => t.TargetHremployeeId!.Value).Distinct().ToList();
        if (directEmpIds.Count > 0)
        {
            var direct = await context.Hremployee
                .Where(e => directEmpIds.Contains(e.id) && e.companyid == companyId && e.ResignDate == null)
                .ToListAsync(ct);
            foreach (var e in direct) result[e.id] = e;
        }

        var orgIds = targets.Where(t => t.TargetType == InfoMessageTargetType.Organization && t.TargetOrganizationId != null)
            .Select(t => t.TargetOrganizationId!.Value).Distinct().ToList();
        if (orgIds.Count > 0)
        {
            var orgs = await context.com_organizations.Where(o => orgIds.Contains(o.id)).ToListAsync(ct);
            foreach (var org in orgs)
            {
                if (string.IsNullOrWhiteSpace(org.orgcodefull)) continue;
                var inOrg = await context.Hremployee
                    .Where(e => e.companyid == companyId && e.ResignDate == null
                        && e.orgcodefull != null && e.orgcodefull.StartsWith(org.orgcodefull))
                    .ToListAsync(ct);
                foreach (var e in inOrg) result[e.id] = e;
            }
        }

        return result.Values.ToList();
    }

    public async Task<List<doc_center>> GetAttachmentsAsync(long infoMessageId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.doc_centers
            .Where(d => d.refid == infoMessageId && d.doctypecode == "HR_ANNOUNCEMENT" && d.isActive != false)
            .ToListAsync(ct);
    }

    public async Task AddAttachmentAsync(long infoMessageId, string fileName, string relativePath, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        context.doc_centers.Add(new doc_center
        {
            refid = infoMessageId,
            doctypecode = "HR_ANNOUNCEMENT",
            files = fileName,
            path = relativePath,
            isActive = true,
            moddate = DateTime.Now,
        });
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveAttachmentAsync(long docCenterId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var doc = await context.doc_centers.FirstOrDefaultAsync(d => d.id == docCenterId && d.doctypecode == "HR_ANNOUNCEMENT", ct);
        if (doc is null) return;
        doc.isActive = false;
        await context.SaveChangesAsync(ct);
    }

    // Upsert one info_message row + wholesale-replace its targets — simplest
    // correct approach for a small admin form (not a hot path).
    public async Task<info_message> SaveAsync(info_message entity, List<Info_MessageTarget> targets, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        if (entity.Id == 0)
        {
            entity.CreatedByUserId = actorUserId;
            entity.CreatedDate = DateTime.Now;
            context.info_messages.Add(entity);
        }
        else
        {
            var existing = await context.info_messages.FirstOrDefaultAsync(m => m.Id == entity.Id, ct)
                ?? throw new InvalidOperationException("ไม่พบประกาศนี้แล้ว");
            existing.subject = entity.subject;
            existing.message = entity.message;
            existing.startdate = entity.startdate;
            existing.enddate = entity.enddate;
            existing.isactive = entity.isactive;
            existing.PriorityLevel = entity.PriorityLevel;
            existing.IsPinned = entity.IsPinned;
            existing.CompanyId = entity.CompanyId;
            existing.ModifiedDate = DateTime.Now;
            existing.ModifiedByUserId = actorUserId;
            entity = existing;
        }
        await context.SaveChangesAsync(ct);

        var oldTargets = await context.Info_MessageTargets.Where(t => t.InfoMessageId == entity.Id).ToListAsync(ct);
        context.Info_MessageTargets.RemoveRange(oldTargets);
        foreach (var t in targets)
        {
            t.Id = 0;
            t.InfoMessageId = entity.Id;
            context.Info_MessageTargets.Add(t);
        }
        await context.SaveChangesAsync(ct);

        return entity;
    }

    public async Task<List<info_message>> GetAllForAdminAsync(string companyId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.info_messages
            .Where(m => m.CompanyId == companyId)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync(ct);
    }
}
