using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Asset;

// Company-asset custody for HR ("who holds which laptop / phone / card / uniform,
// has it come back"), modelled on the epms asset module the company shipped
// before (D:\Workspace\hro): asset_owner = the registry (one row per asset,
// with the current holder), asset_notice = the custody log (one row per
// hand-over / self-declaration / return, with dates). Both are the legacy
// tables already in the schema — reused rather than redesigned, per the house
// rule. Depreciation / book value stay with the accounting system; HR only
// tracks possession and return, and feeds offboarding + final-pay deductions.
public class AssetService(IDbContextFactory<HRMContext> dbFactory)
{
    public const string StatusAvailable = "ว่าง";
    public const string StatusHeld = "ครอบครอง";
    public const string StatusRepair = "ซ่อม";
    public const string StatusDisposed = "ตัดจำหน่าย";
    public static readonly string[] Statuses = { StatusAvailable, StatusHeld, StatusRepair, StatusDisposed };

    public const string NoticeHeld = "ครอบครอง";
    public const string NoticeReturned = "คืนแล้ว";
    public const string NoticeSelfDeclared = "แจ้งครอบครอง";

    public async Task<List<asset_owner>> ListAsync(string companyId, string? search, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        var q = ctx.asset_owners.Where(a => a.comCode == companyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(a => (a.assetNo != null && a.assetNo.Contains(s)) || (a.tag != null && a.tag.Contains(s)) || (a.serial != null && a.serial.Contains(s))
                          || (a.description != null && a.description.Contains(s)) || (a.brand != null && a.brand.Contains(s)) || (a.model != null && a.model.Contains(s))
                          || (a.empName != null && a.empName.Contains(s)) || (a.empid != null && a.empid.Contains(s)) || (a.category != null && a.category.Contains(s))
                          || (a.location != null && a.location.Contains(s)) || (a.costCenter != null && a.costCenter.Contains(s)));
        }
        return await q.OrderBy(a => a.assetNo).AsNoTracking().ToListAsync(ct);
    }

    public async Task<asset_owner?> GetAsync(long id, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        return await ctx.asset_owners.AsNoTracking().FirstOrDefaultAsync(a => a.id == id, ct);
    }

    public async Task<long> SaveAsync(asset_owner input, string companyId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.assetNo)) throw new InvalidOperationException("ต้องระบุเลขทรัพย์สิน");
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        var dup = await ctx.asset_owners.AnyAsync(a => a.comCode == companyId && a.assetNo == input.assetNo.Trim() && a.id != input.id, ct);
        if (dup) throw new InvalidOperationException($"เลขทรัพย์สิน {input.assetNo} มีอยู่แล้ว");

        asset_owner row;
        if (input.id > 0)
        {
            row = await ctx.asset_owners.FirstOrDefaultAsync(a => a.id == input.id, ct) ?? throw new InvalidOperationException("ไม่พบทรัพย์สิน");
        }
        else
        {
            row = new asset_owner { comCode = companyId, status = StatusAvailable };
            ctx.asset_owners.Add(row);
        }
        row.assetNo = input.assetNo.Trim(); row.tag = input.tag?.Trim(); row.serial = input.serial?.Trim();
        row.category = input.category?.Trim(); row.description = input.description?.Trim();
        row.brand = input.brand?.Trim(); row.model = input.model?.Trim();
        row.location = input.location?.Trim(); row.place = input.place?.Trim(); row.costCenter = input.costCenter?.Trim();
        row.inServiceDate = input.inServiceDate?.Trim(); row.qty = string.IsNullOrWhiteSpace(input.qty) ? "1" : input.qty.Trim();
        row.currentCost = input.currentCost?.Trim();
        if (input.id > 0 && !string.IsNullOrWhiteSpace(input.status) && row.status != StatusHeld) row.status = input.status;
        await ctx.SaveChangesAsync(ct);
        return row.id;
    }

    // HR hands an asset to an employee (epms "แจ้งทรัพย์สิน (ตัวแทน)").
    public async Task AssignAsync(long assetId, long hremployeeId, string actorLogin, long actorUserId, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        var asset = await ctx.asset_owners.FirstOrDefaultAsync(a => a.id == assetId, ct) ?? throw new InvalidOperationException("ไม่พบทรัพย์สิน");
        if (asset.status == StatusDisposed) throw new InvalidOperationException("ทรัพย์สินนี้ตัดจำหน่ายแล้ว");
        var emp = await ctx.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct) ?? throw new InvalidOperationException("ไม่พบพนักงาน");
        if (!string.IsNullOrWhiteSpace(asset.empid) && asset.empid == emp.EmpNo) throw new InvalidOperationException($"{emp.EmpNo} ถือทรัพย์สินนี้อยู่แล้ว");
        if (!string.IsNullOrWhiteSpace(asset.empid))
            await CloseOpenNoticeAsync(ctx, asset, actorLogin, actorUserId, NoticeReturned, ct);
        await OpenNoticeAsync(ctx, asset, emp, actorLogin, actorUserId, NoticeHeld, ct);
        await ctx.SaveChangesAsync(ct);
    }

    // Employee declares "I hold asset X" from ESS (epms "รายการทรัพย์สินส่วนตัว → แจ้งครอบครอง").
    public async Task SelfDeclareAsync(string assetNo, Hremployee emp, string actorLogin, long actorUserId, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        var asset = await ctx.asset_owners.FirstOrDefaultAsync(a => a.comCode == emp.companyid && a.assetNo == assetNo.Trim(), ct)
            ?? throw new InvalidOperationException($"ไม่พบเลขทรัพย์สิน {assetNo} ในทะเบียนของบริษัท");
        if (asset.empid == emp.EmpNo) throw new InvalidOperationException($"ท่านแจ้งรายการนี้ไว้แล้ว {assetNo}");
        if (asset.status == StatusDisposed) throw new InvalidOperationException("ทรัพย์สินนี้ตัดจำหน่ายแล้ว");
        if (!string.IsNullOrWhiteSpace(asset.empid))
            await CloseOpenNoticeAsync(ctx, asset, actorLogin, actorUserId, NoticeReturned, ct);
        await OpenNoticeAsync(ctx, asset, emp, actorLogin, actorUserId, NoticeSelfDeclared, ct);
        await ctx.SaveChangesAsync(ct);
    }

    // Asset comes back (offboarding, replacement, repair). Optional new status
    // for the asset afterwards (default: available).
    public async Task ReturnAsync(long assetId, string actorLogin, long actorUserId, string? newStatus = null, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        var asset = await ctx.asset_owners.FirstOrDefaultAsync(a => a.id == assetId, ct) ?? throw new InvalidOperationException("ไม่พบทรัพย์สิน");
        await CloseOpenNoticeAsync(ctx, asset, actorLogin, actorUserId, NoticeReturned, ct);
        asset.status = string.IsNullOrWhiteSpace(newStatus) ? StatusAvailable : newStatus;
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<List<asset_notice>> HistoryAsync(long assetId, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        return await ctx.asset_notices.Where(n => n.assetid == assetId).OrderByDescending(n => n.createdate).AsNoTracking().ToListAsync(ct);
    }

    public async Task<List<asset_owner>> HeldByAsync(string companyId, string empNo, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        return await ctx.asset_owners.Where(a => a.comCode == companyId && a.empid == empNo).OrderBy(a => a.assetNo).AsNoTracking().ToListAsync(ct);
    }

    public async Task<List<asset_notice>> MyNoticesAsync(string companyId, string empNo, CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        return await ctx.asset_notices.Where(n => n.comCode == companyId && n.empid == empNo).OrderByDescending(n => n.createdate).Take(50).AsNoTracking().ToListAsync(ct);
    }

    private static async Task OpenNoticeAsync(HRMContext ctx, asset_owner asset, Hremployee emp, string actorLogin, long actorUserId, string noticeStatus, CancellationToken ct)
    {
        var now = DateTime.Now;
        var name = $"{emp.EmpName} {emp.EmpSurname}".Trim();
        ctx.asset_notices.Add(new asset_notice
        {
            assetid = asset.id, assetNo = asset.assetNo, serial = asset.serial, tag = asset.tag, qty = asset.qty, comCode = asset.comCode,
            empid = emp.EmpNo, empName = name, orgcode = emp.orgcodefull, status = noticeStatus, getdate = now,
            createdate = now, createby = actorLogin, moddate = now, modby = actorLogin, moduserid = actorUserId, isOwnbyOrg = false,
        });
        asset.empid = emp.EmpNo; asset.empName = name; asset.status = StatusHeld;
        await Task.CompletedTask;
    }

    private static async Task CloseOpenNoticeAsync(HRMContext ctx, asset_owner asset, string actorLogin, long actorUserId, string closeStatus, CancellationToken ct)
    {
        var open = await ctx.asset_notices.Where(n => n.assetid == asset.id && n.outdate == null).ToListAsync(ct);
        var now = DateTime.Now;
        foreach (var n in open) { n.outdate = now; n.status = closeStatus; n.moddate = now; n.modby = actorLogin; n.moduserid = actorUserId; }
        asset.empid = null; asset.empName = null;
    }
}
