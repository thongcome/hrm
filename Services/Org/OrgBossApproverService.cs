using HRM.Models;
using HRM.Services.Pay;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Org;

// Two direct (non-workflow) HR actions on com_organization, per explicit
// user decision (2026-08-28) — distinct from Org_OrganizationChangeRequest's
// multi-level-approval-gated permanent changes:
//   1) Directly setting/overriding boss_name/boss_emp_id — normally this is
//      derived automatically from whichever Pos_PositionSlot with IsBoss=true
//      is occupied (see Services/Shared/EmployeePositionSync.cs), but HR can
//      also override it directly here (e.g. for orgs without a proper boss
//      position slot set up yet). Every change is captured automatically by
//      HRMContext.Audit.cs — no extra logging needed here.
//   2) Temporary approver delegation, backed by `toa` ("Table of
//      Authority") — a dormant legacy JSP-era table repurposed for this
//      (see Model/toa.cs's doc comment) rather than a new table, per CRUD
//      skill rule 5. HR hands off workflow-approval authority to someone
//      other than the boss for a date range, with full history kept (rows
//      are never deleted, only isactive flipped off).
public class OrgBossApproverService(IDbContextFactory<HRMContext> dbFactory, PrivateFileStorage fileStorage)
{
    public async Task SetBossDirectAsync(long organizationId, string? empId, string name, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.id == organizationId, ct)
            ?? throw new InvalidOperationException("ไม่พบหน่วยงานนี้");

        org.boss_emp_id = empId;
        org.boss_name = name;

        // Approver defaults to boss unless a delegation is currently active.
        var hasActiveDelegation = await context.toas
            .AnyAsync(d => d.OrganizationId == organizationId && d.isactive, ct);
        if (!hasActiveDelegation)
        {
            org.approver_empid = empId;
            org.approver_name = name;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<long> CreateDelegationAsync(long organizationId, string? delegateEmpId, string delegateName,
        DateOnly startDate, DateOnly? endDate, string? reason, string? attachmentFileName, byte[]? attachmentBytes,
        CancellationToken ct = default)
    {
        if (endDate is DateOnly end && end < startDate)
            throw new InvalidOperationException("วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มมอบหมาย");

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.id == organizationId, ct)
            ?? throw new InvalidOperationException("ไม่พบหน่วยงานนี้");

        var existingActive = await context.toas
            .AnyAsync(d => d.OrganizationId == organizationId && d.isactive, ct);
        if (existingActive)
            throw new InvalidOperationException("มีการมอบหมายผู้อนุมัติแทนที่ยังไม่สิ้นสุดอยู่แล้ว — สิ้นสุดรายการเดิมก่อนมอบหมายใหม่");

        string? storagePath = null;
        if (attachmentBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(attachmentFileName))
        {
            var storedFileName = $"{organizationId}_{DateTime.Now:yyyyMMddHHmmss}_{attachmentFileName}";
            (storagePath, _) = await fileStorage.SaveAsync("org-approver-delegations", storedFileName, attachmentBytes, ct);
        }

        var delegation = new toa
        {
            OrganizationId = organizationId,
            DelegateEmpId = delegateEmpId,
            DelegateName = delegateName,
            OriginalApproverEmpId = org.approver_empid,
            StartDate = startDate,
            EndDate = endDate,
            remark = reason,
            AttachmentFileName = storagePath is null ? null : attachmentFileName,
            AttachmentStoragePath = storagePath,
            isactive = true,
        };
        context.toas.Add(delegation);

        // Apply immediately if already effective; otherwise SyncEffectiveApproverAsync
        // picks it up lazily once StartDate arrives (no scheduler in this app).
        if (startDate <= DateOnly.FromDateTime(DateTime.Today))
        {
            org.approver_empid = delegateEmpId;
            org.approver_name = delegateName;
        }

        await context.SaveChangesAsync(ct);
        return delegation.toaid;
    }

    public async Task EndDelegationAsync(long delegationId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var delegation = await context.toas.FirstOrDefaultAsync(d => d.toaid == delegationId, ct)
            ?? throw new InvalidOperationException("ไม่พบรายการมอบหมายนี้");
        if (!delegation.isactive) return;

        if (delegation.OrganizationId is long orgId)
        {
            var org = await context.com_organizations.FirstOrDefaultAsync(o => o.id == orgId, ct);
            if (org is not null)
            {
                org.approver_empid = delegation.OriginalApproverEmpId;
                org.approver_name = delegation.OriginalApproverEmpId is null ? org.boss_name
                    : (await context.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == delegation.OriginalApproverEmpId, ct)) is { } origEmp
                        ? $"{origEmp.EmpName} {origEmp.EmpSurname}".Trim()
                        : org.boss_name;
            }
        }
        delegation.isactive = false;

        await context.SaveChangesAsync(ct);
    }

    // Lazy apply-on-read (see OrgChangeRequestService.ApplyDueChangesAsync for
    // the established pattern this mirrors) — call from every page that
    // displays an org's approver, before reading approver_name/approver_empid.
    // Reverts a delegation whose EndDate has passed, and activates one whose
    // StartDate has just arrived (covers a future-dated delegation nobody's
    // visited a relevant page for since its start date).
    public async Task SyncEffectiveApproverAsync(long organizationId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var active = await context.toas
            .Where(d => d.OrganizationId == organizationId && d.isactive)
            .ToListAsync(ct);
        if (active.Count == 0) return;

        var org = await context.com_organizations.FirstOrDefaultAsync(o => o.id == organizationId, ct);
        if (org is null) return;

        var changed = false;
        foreach (var d in active)
        {
            if (d.EndDate is DateOnly end && end < today)
            {
                org.approver_empid = d.OriginalApproverEmpId;
                if (d.OriginalApproverEmpId is not null)
                {
                    var origEmp = await context.Hremployee.FirstOrDefaultAsync(e => e.EmpNo == d.OriginalApproverEmpId, ct);
                    org.approver_name = origEmp is not null ? $"{origEmp.EmpName} {origEmp.EmpSurname}".Trim() : org.boss_name;
                }
                else
                {
                    org.approver_name = org.boss_name;
                }
                d.isactive = false;
                changed = true;
            }
            else if (d.StartDate is DateOnly start && start <= today && org.approver_empid != d.DelegateEmpId)
            {
                org.approver_empid = d.DelegateEmpId;
                org.approver_name = d.DelegateName;
                changed = true;
            }
        }

        if (changed) await context.SaveChangesAsync(ct);
    }

    public async Task<toa?> GetActiveDelegationAsync(long organizationId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        return await context.toas
            .Where(d => d.OrganizationId == organizationId && d.isactive)
            .FirstOrDefaultAsync(ct);
    }

    // Ended-reason is inferred (no dedicated column on this legacy table):
    // if EndDate had already passed by the time it was deactivated, it
    // lapsed naturally; otherwise HR ended it manually. Exact
    // deactivation timestamp/actor is in AuditLog like every other change
    // in this app, not duplicated onto this row.
    public async Task<List<(toa Delegation, string StatusLabel)>> GetHistoryAsync(long organizationId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var rows = await context.toas
            .Where(d => d.OrganizationId == organizationId)
            .OrderByDescending(d => d.toaid)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        return rows.Select(d =>
        {
            var status = d.isactive ? "กำลังใช้งาน"
                : (d.EndDate is DateOnly end && end < today) ? "หมดอายุแล้ว"
                : "ยกเลิกโดย HR";
            return (d, status);
        }).ToList();
    }
}
