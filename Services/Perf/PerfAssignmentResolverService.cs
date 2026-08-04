using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Perf;

// Resolves a Perf_EvaluationAssignment "rule" into real Perf_EvaluationInstance +
// Perf_RaterAssignment rows. Walks the org chart directly via
// Hremployee.OrganizationId / com_organization.parent_code / approver_empid —
// per the plan's explicit instruction, NOT via wf_employee (the legacy demo
// table WorkflowEngineService's Vertical resolution still uses, a known,
// separate gap outside this module's scope).
public class PerfAssignmentResolverService(IDbContextFactory<HRMContext> dbFactory)
{
    public record RaterResolution(PerfRaterDirection Direction, long? RaterHremployeeId, PerfRaterStatus Status);

    public async Task<List<Hremployee>> ResolveTargetEmployeesAsync(Perf_EvaluationAssignment assignment, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var period = await context.Perf_EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == assignment.EvaluationPeriodId, ct)
            ?? throw new InvalidOperationException("ไม่พบรอบการประเมินนี้แล้ว");

        return await ResolveTargetEmployeesAsync(context, assignment, period.CompanyId, ct);
    }

    private static async Task<List<Hremployee>> ResolveTargetEmployeesAsync(
        HRMContext context, Perf_EvaluationAssignment assignment, string companyId, CancellationToken ct)
    {
        switch (assignment.TargetScope)
        {
            case PerfTargetScope.Organization:
            {
                if (assignment.TargetOrganizationId is not long orgId)
                    return new();
                var org = await context.com_organizations.FirstOrDefaultAsync(o => o.id == orgId, ct);
                if (org is null || string.IsNullOrWhiteSpace(org.orgcodefull))
                    return new();
                return await context.Hremployee
                    .Where(e => e.companyid == companyId && e.ResignDate == null
                        && e.orgcodefull != null && e.orgcodefull.StartsWith(org.orgcodefull))
                    .ToListAsync(ct);
            }
            case PerfTargetScope.Position:
            {
                if (assignment.TargetPosExecTypeId is not long posExecTypeId)
                    return new();
                var employeeIds = await context.Pos_PositionSlots
                    .Where(s => s.CompanyId == companyId && s.IsActive && s.PosExecTypeId == posExecTypeId && s.HremployeeId != null)
                    .Select(s => s.HremployeeId!.Value)
                    .ToListAsync(ct);
                return await context.Hremployee
                    .Where(e => e.companyid == companyId && e.ResignDate == null && employeeIds.Contains(e.id))
                    .ToListAsync(ct);
            }
            case PerfTargetScope.Employee:
            {
                if (assignment.TargetHremployeeId is not long empId)
                    return new();
                var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == empId && e.companyid == companyId && e.ResignDate == null, ct);
                return emp is null ? new() : new() { emp };
            }
            default:
                return new();
        }
    }

    public async Task<List<RaterResolution>> ResolveRatersAsync(long hremployeeId, Perf_RaterDirectionConfig config, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบพนักงานคนนี้แล้ว");
        return await ResolveRatersAsync(context, employee, config, ct);
    }

    private static async Task<List<RaterResolution>> ResolveRatersAsync(
        HRMContext context, Hremployee employee, Perf_RaterDirectionConfig config, CancellationToken ct)
    {
        var results = new List<RaterResolution>();

        if (config.AllowSelf)
            results.Add(new(PerfRaterDirection.Self, employee.id, PerfRaterStatus.Pending));

        if (config.SuperiorLevels > 0)
            results.AddRange(await ResolveSuperiorChainAsync(context, employee, config.SuperiorLevels, ct));

        if (config.SubordinateLevels > 0)
            results.AddRange(await ResolveSubordinateChainAsync(context, employee, config.SubordinateLevels, ct));

        if (config.IncludePeers && employee.OrganizationId is not null)
        {
            var peerIds = await context.Hremployee
                .Where(e => e.OrganizationId == employee.OrganizationId && e.ResignDate == null && e.id != employee.id)
                .Select(e => e.id)
                .ToListAsync(ct);
            results.AddRange(peerIds.Select(id => new RaterResolution(PerfRaterDirection.Peer, id, PerfRaterStatus.Pending)));
        }

        return results;
    }

    private static readonly PerfRaterDirection[] SuperiorDirections =
        { PerfRaterDirection.Superior1, PerfRaterDirection.Superior2, PerfRaterDirection.Superior3 };
    private static readonly PerfRaterDirection[] SubordinateDirections =
        { PerfRaterDirection.Subordinate1, PerfRaterDirection.Subordinate2, PerfRaterDirection.Subordinate3 };

    // Walks up org.parent_code one hop per level (level 1 = employee's own
    // org's approver, level 2 = that org's parent's approver, ...). Once a
    // hop can't be resolved (org missing, or no approver_empid set on it),
    // every remaining level is marked Skipped rather than attempted, since
    // the chain itself is broken from that point up — this is the graceful
    // degradation the plan calls for given incomplete org approver_empid data.
    private static async Task<List<RaterResolution>> ResolveSuperiorChainAsync(
        HRMContext context, Hremployee employee, int levels, CancellationToken ct)
    {
        var results = new List<RaterResolution>();
        com_organization? currentOrg = employee.OrganizationId is long orgId
            ? await context.com_organizations.FirstOrDefaultAsync(o => o.id == orgId, ct)
            : null;
        var chainBroken = currentOrg is null;

        for (var level = 1; level <= levels; level++)
        {
            var direction = SuperiorDirections[level - 1];

            if (chainBroken)
            {
                results.Add(new(direction, null, PerfRaterStatus.Skipped));
                continue;
            }

            var approverId = await ResolveOrgApproverEmployeeIdAsync(context, currentOrg!, ct);
            if (approverId is long resolvedId && resolvedId != employee.id)
            {
                results.Add(new(direction, resolvedId, PerfRaterStatus.Pending));
            }
            else
            {
                results.Add(new(direction, null, PerfRaterStatus.Skipped));
            }

            // Advance to the next org up regardless of whether this level's
            // approver resolved, so a single org missing approver_empid
            // doesn't also block levels further up that might still resolve
            // — only a missing parent org genuinely breaks the chain.
            if (string.IsNullOrWhiteSpace(currentOrg!.parent_code))
            {
                chainBroken = true;
            }
            else
            {
                currentOrg = await context.com_organizations.FirstOrDefaultAsync(o => o.code == currentOrg.parent_code, ct);
                chainBroken = currentOrg is null;
            }
        }

        return results;
    }

    // Fans out downward: level 1 = employees whose org's approver is this
    // employee, level 2 = employees whose org's approver is one of level 1's
    // people, etc. Unlike the Superior chain this is 0..N people per level,
    // not exactly one — an empty level is a normal outcome (e.g. an
    // individual contributor genuinely has no subordinates), not a data gap,
    // so no Skipped placeholder is added when a level comes up empty.
    private static async Task<List<RaterResolution>> ResolveSubordinateChainAsync(
        HRMContext context, Hremployee employee, int levels, CancellationToken ct)
    {
        var results = new List<RaterResolution>();
        var currentLevelEmpNos = new List<string> { employee.EmpNo };

        for (var level = 1; level <= levels && currentLevelEmpNos.Count > 0; level++)
        {
            var direction = SubordinateDirections[level - 1];

            var reportOrgIds = await context.com_organizations
                .Where(o => o.approver_empid != null && currentLevelEmpNos.Contains(o.approver_empid))
                .Select(o => o.id)
                .ToListAsync(ct);

            if (reportOrgIds.Count == 0)
                break;

            var reports = await context.Hremployee
                .Where(e => e.ResignDate == null && e.OrganizationId != null && reportOrgIds.Contains(e.OrganizationId!.Value) && e.id != employee.id)
                .ToListAsync(ct);

            results.AddRange(reports.Select(r => new RaterResolution(direction, r.id, PerfRaterStatus.Pending)));
            currentLevelEmpNos = reports.Select(r => r.EmpNo).ToList();
        }

        return results;
    }

    // approver_empid stores sc_user.empid, which is always the employee's
    // EmpNo (see ScUserClaimsPrincipalFactory) — resolved directly against
    // Hremployee.EmpNo here rather than round-tripping through sc_user, so a
    // configured approver who has no login account yet still resolves.
    private static async Task<long?> ResolveOrgApproverEmployeeIdAsync(HRMContext context, com_organization org, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(org.approver_empid))
            return null;
        var approver = await context.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == org.approver_empid && e.ResignDate == null, ct);
        return approver?.id;
    }

    // Creates Perf_EvaluationInstance + Perf_RaterAssignment rows for every
    // target of the assignment. Idempotent at the assignment level
    // (IsResolved guard) and skips any target employee who already has an
    // instance for this period (guards against overlapping assignments —
    // e.g. an Organization-scope rule and an Employee-scope rule both
    // covering the same person in the same period).
    public async Task<int> CreateInstancesAsync(long assignmentId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var assignment = await context.Perf_EvaluationAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new InvalidOperationException("ไม่พบกติกาการมอบหมายนี้แล้ว");
        if (assignment.IsResolved)
            throw new InvalidOperationException("กติกานี้ถูกสร้างรายการประเมินไปแล้ว");

        var period = await context.Perf_EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == assignment.EvaluationPeriodId, ct)
            ?? throw new InvalidOperationException("ไม่พบรอบการประเมินนี้แล้ว");
        var config = await context.Perf_RaterDirectionConfigs.FirstOrDefaultAsync(c => c.Id == assignment.RaterDirectionConfigId, ct)
            ?? throw new InvalidOperationException("ไม่พบทิศทางการประเมินนี้แล้ว");

        var targets = await ResolveTargetEmployeesAsync(context, assignment, period.CompanyId, ct);
        if (targets.Count == 0)
        {
            assignment.IsResolved = true;
            assignment.ResolvedDate = DateTime.Now;
            await context.SaveChangesAsync(ct);
            return 0;
        }

        var targetIds = targets.Select(t => t.id).ToList();
        var existingInstanceEmpIds = await context.Perf_EvaluationInstances
            .Where(i => i.EvaluationPeriodId == assignment.EvaluationPeriodId && targetIds.Contains(i.HremployeeId))
            .Select(i => i.HremployeeId)
            .ToListAsync(ct);
        var existingSet = existingInstanceEmpIds.ToHashSet();

        var orgIds = targets.Where(t => t.OrganizationId is not null).Select(t => t.OrganizationId!.Value).Distinct().ToList();
        var orgsById = await context.com_organizations.Where(o => orgIds.Contains(o.id)).ToDictionaryAsync(o => o.id, ct);
        var slotIds = targetIds;
        var slotsByEmpId = await context.Pos_PositionSlots
            .Where(s => s.IsActive && s.HremployeeId != null && slotIds.Contains(s.HremployeeId!.Value))
            .ToListAsync(ct);
        var slotsByEmpIdMap = slotsByEmpId.GroupBy(s => s.HremployeeId!.Value).ToDictionary(g => g.Key, g => g.First());

        var createdCount = 0;
        foreach (var target in targets)
        {
            if (existingSet.Contains(target.id))
                continue;

            com_organization? org = target.OrganizationId is long oid && orgsById.TryGetValue(oid, out var o) ? o : null;
            slotsByEmpIdMap.TryGetValue(target.id, out var slot);

            var instance = new Perf_EvaluationInstance
            {
                EvaluationPeriodId = assignment.EvaluationPeriodId,
                EvaluationTypeId = assignment.EvaluationTypeId,
                HremployeeId = target.id,
                SnapshotEmpNo = target.EmpNo,
                SnapshotEmpName = $"{target.EmpName} {target.EmpSurname}".Trim(),
                SnapshotPositionName = slot?.Name,
                SnapshotOrganizationCode = target.orgcode ?? org?.code,
                SnapshotOrganizationName = org?.name,
                Status = PerfInstanceStatus.Draft,
            };
            context.Perf_EvaluationInstances.Add(instance);

            var raters = await ResolveRatersAsync(context, target, config, ct);
            var directionWeight = new Func<PerfRaterDirection, decimal>(d => d switch
            {
                PerfRaterDirection.Self => config.WeightSelf,
                PerfRaterDirection.Superior1 or PerfRaterDirection.Superior2 or PerfRaterDirection.Superior3 => config.WeightSuperior,
                PerfRaterDirection.Subordinate1 or PerfRaterDirection.Subordinate2 or PerfRaterDirection.Subordinate3 => config.WeightSubordinate,
                PerfRaterDirection.Peer => config.WeightPeer,
                _ => 0m,
            });

            foreach (var rater in raters)
            {
                context.Perf_RaterAssignments.Add(new Perf_RaterAssignment
                {
                    EvaluationInstance = instance,
                    RaterHremployeeId = rater.RaterHremployeeId,
                    Direction = rater.Direction,
                    WeightInFinalScore = directionWeight(rater.Direction),
                    Status = rater.Status,
                });
            }

            createdCount++;
        }

        assignment.IsResolved = true;
        assignment.ResolvedDate = DateTime.Now;

        await context.SaveChangesAsync(ct);
        return createdCount;
    }
}
