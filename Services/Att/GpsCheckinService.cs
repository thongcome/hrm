namespace HRM.Services.Att;

using HRM.Models;
using HRM.Services.Shared;
using Microsoft.EntityFrameworkCore;

// GPS self check-in for employees physically on-site — strictly geofenced
// (per user decision: reject outside the configured radius, no soft
// warning). Separate concept from Att_PunchLog.Source=WfhSelfCheckin, which
// is for remote work and intentionally has no location requirement at all.
// HR can still record an exceptional punch manually via
// AttendanceReport.razor's existing WFH-entry panel if GPS genuinely fails
// for a legitimate reason — that path is the override, no new UI needed here.
public class GpsCheckinService(IDbContextFactory<HRMContext> dbFactory)
{
    public record CheckinResult(bool Success, string Message, AttPunchDirection? Direction);

    public async Task<CheckinResult> CheckinAsync(long hremployeeId, string companyId, decimal latitude, decimal longitude, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var settings = await context.Att_CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        if (settings is null || !settings.AllowGpsCheckin)
            return new(false, "บริษัทยังไม่เปิดใช้งานการเช็คอินด้วย GPS", null);

        var locations = await context.Att_GeofenceLocations
            .Where(l => l.CompanyId == companyId && l.IsActive)
            .ToListAsync(ct);
        if (locations.Count == 0)
            return new(false, "ยังไม่มีการตั้งค่าพื้นที่อนุญาตให้เช็คอิน กรุณาติดต่อ HR", null);

        Att_GeofenceLocation? nearest = null;
        var nearestDistance = double.MaxValue;
        foreach (var loc in locations)
        {
            var distance = GeoDistanceHelper.DistanceMeters((double)latitude, (double)longitude, (double)loc.Latitude, (double)loc.Longitude);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = loc;
            }
        }

        if (nearest is null || nearestDistance > nearest.RadiusMeters)
        {
            var nearestName = nearest?.Name ?? "-";
            return new(false,
                $"อยู่นอกพื้นที่ที่กำหนด (ใกล้ที่สุดคือ \"{nearestName}\" ห่างประมาณ {nearestDistance:N0} เมตร) — หากต้องการบันทึกเวลาด้วยเหตุผลพิเศษ กรุณาติดต่อ HR ให้บันทึกแทนที่หน้ารายงานเวลาเข้างาน",
                null);
        }

        var employee = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct)
            ?? throw new InvalidOperationException("ไม่พบข้อมูลพนักงาน");

        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1);
        var lastPunchToday = await context.Att_PunchLogs
            .Where(p => p.HremployeeId == hremployeeId && p.PunchTime >= todayStart && p.PunchTime < todayEnd && p.Source == AttPunchSource.GpsCheckin)
            .OrderByDescending(p => p.PunchTime)
            .FirstOrDefaultAsync(ct);

        // auto-detect direction: no GPS punch yet today, or last one was Out -> this is In;
        // last one was In -> this is Out (allows multiple in/out pairs per day, e.g. lunch break)
        var direction = lastPunchToday is null || lastPunchToday.Direction == AttPunchDirection.Out
            ? AttPunchDirection.In
            : AttPunchDirection.Out;

        context.Att_PunchLogs.Add(new Att_PunchLog
        {
            CompanyId = companyId,
            HremployeeId = hremployeeId,
            RawEmpCode = employee.EmpNo,
            PunchTime = DateTime.Now,
            Direction = direction,
            Source = AttPunchSource.GpsCheckin,
            Latitude = latitude,
            Longitude = longitude,
        });
        await context.SaveChangesAsync(ct);

        return new(true, direction == AttPunchDirection.In ? "เช็คอินสำเร็จ" : "เช็คเอาท์สำเร็จ", direction);
    }

    public async Task<List<Att_PunchLog>> GetTodaysPunchesAsync(long hremployeeId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1);
        return await context.Att_PunchLogs
            .Where(p => p.HremployeeId == hremployeeId && p.PunchTime >= todayStart && p.PunchTime < todayEnd && p.Source == AttPunchSource.GpsCheckin)
            .OrderBy(p => p.PunchTime)
            .ToListAsync(ct);
    }
}
