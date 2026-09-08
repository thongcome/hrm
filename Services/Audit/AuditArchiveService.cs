namespace HRM.Services.Audit;

using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// Moves old AuditLog rows out of the database into monthly files on a file
// server, then deletes them from the table.
//
// Why files at all: the table was 445 MB of a 912 MB database (measured
// 8 ก.ย. 2569) and grows with every payroll run. Audit rows are write-once,
// read-rarely, and compress roughly 10-20x because they repeat heavily.
//
// Why this stays legally sound: พ.ร.บ. คอมพิวเตอร์ มาตรา 26 requires the data
// be RETAINED, not that it live in any particular table. What it does require
// is that the retained copy be intact and producible — which is why every
// archive file gets a SHA-256 recorded in a sidecar manifest, and why rows
// are deleted only after the file has been written, closed, re-read and
// verified line-for-line. A failure anywhere in that sequence leaves the
// database untouched.
//
// There is no background scheduler anywhere in this app (deliberate), so this
// runs from the admin page's button, or from a Windows scheduled task calling
// the same service.
public sealed class AuditArchiveService(
    IDbContextFactory<HRMContext> dbFactory,
    IOptionsMonitor<AuditArchiveOptions> options,
    ILogger<AuditArchiveService> logger)
{
    public AuditArchiveOptions Options => options.CurrentValue;

    public sealed record MonthCandidate(int Year, int Month, int Rows);
    public sealed record ArchiveResult(string FileName, int Rows, long Bytes, string Sha256);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Options.Path);

    // Nothing on or after this date is ever archived. The 90-day floor is the
    // statutory minimum and is enforced here, not merely defaulted, so a
    // mis-set config cannot delete data the law still wants online.
    public int EffectiveKeepDays => Math.Max(90, Options.KeepDaysInDatabase);
    public DateTime CutoffDate() => DateTime.Today.AddDays(-EffectiveKeepDays);

    // Whole months old enough to archive, newest first. Partial months are
    // excluded — archiving half of a month and leaving the rest would produce
    // a second file later claiming the same period.
    public async Task<List<MonthCandidate>> GetCandidatesAsync(CancellationToken ct = default)
    {
        var cutoff = CutoffDate();
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var grouped = await context.AuditLogs
            .Where(a => a.EventDate < cutoff)
            .GroupBy(a => new { a.EventDate.Year, a.EventDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Rows = g.Count() })
            .ToListAsync(ct);

        return grouped
            .Where(g => new DateTime(g.Year, g.Month, DateTime.DaysInMonth(g.Year, g.Month)) < cutoff)
            .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
            .Select(g => new MonthCandidate(g.Year, g.Month, g.Rows))
            .ToList();
    }

    public async Task<ArchiveResult> ArchiveMonthAsync(int year, int month, string actor, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("ยังไม่ได้ตั้งค่าที่เก็บไฟล์ (AuditArchive:Path) ใน appsettings");

        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);
        if (to > CutoffDate())
            throw new InvalidOperationException($"เดือน {month}/{year} ยังใหม่เกินไป ต้องเก็บไว้ในฐานข้อมูลอย่างน้อย {EffectiveKeepDays} วัน");

        var dir = Path.Combine(Options.Path, year.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(dir);

        var stamp = $"{year:D4}-{month:D2}";
        var file = Path.Combine(dir, $"audit-{stamp}.jsonl.gz");
        if (File.Exists(file))
            throw new InvalidOperationException($"มีไฟล์ {Path.GetFileName(file)} อยู่แล้ว — ระบบจะไม่เขียนทับ กรุณาตรวจสอบก่อน");

        // ---- 1) write to a temporary name, so a crash never leaves a
        //         half-written file looking like a complete archive ----
        var rows = 0;
        var tmp = file + ".writing";
        await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
        await using (var gz = new GZipStream(fs, CompressionLevel.SmallestSize))
        await using (var sw = new StreamWriter(gz, new UTF8Encoding(false)))
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var lastId = 0L;
            while (true)
            {
                var batch = await context.AuditLogs.AsNoTracking()
                    .Where(a => a.EventDate >= from && a.EventDate < to && a.Id > lastId)
                    .OrderBy(a => a.Id)
                    .Take(Options.BatchSize)
                    .ToListAsync(ct);
                if (batch.Count == 0) break;

                foreach (var a in batch)
                {
                    await sw.WriteLineAsync(JsonSerializer.Serialize(a).AsMemory(), ct);
                    rows++;
                }
                lastId = batch[^1].Id;
            }
        }

        if (rows == 0)
        {
            File.Delete(tmp);
            throw new InvalidOperationException($"ไม่พบข้อมูล audit ของเดือน {month}/{year}");
        }

        File.Move(tmp, file);

        // ---- 2) verify what actually landed on disk, before deleting ----
        var sha = await Sha256OfFileAsync(file, ct);
        var lineCount = await CountGzipLinesAsync(file, ct);
        if (lineCount != rows)
            throw new InvalidOperationException($"ไฟล์ที่เขียนมี {lineCount} บรรทัด แต่ควรมี {rows} — ยกเลิก ไม่ลบข้อมูลออกจากฐานข้อมูล");

        var bytes = new FileInfo(file).Length;
        await File.WriteAllTextAsync(file + ".manifest.json", JsonSerializer.Serialize(new
        {
            file = Path.GetFileName(file),
            period = stamp,
            rows,
            bytes,
            sha256 = sha,
            archivedAtUtc = DateTime.UtcNow,
            archivedBy = actor,
            note = "AuditLog rows exported from HRM. Retained per Thai Computer Crime Act section 26. Verify SHA-256 before use as evidence.",
        }, new JsonSerializerOptions { WriteIndented = true }), ct);

        // ---- 3) only now remove them from the database ----
        var deleted = 0;
        await using (var context = await dbFactory.CreateDbContextAsync(ct))
        {
            while (true)
            {
                var n = await context.AuditLogs
                    .Where(a => a.EventDate >= from && a.EventDate < to)
                    .Take(Options.BatchSize)
                    .ExecuteDeleteAsync(ct);
                if (n == 0) break;
                deleted += n;
            }
        }

        logger.LogInformation(
            "Audit archive {Period}: wrote {Rows} rows to {File} ({Bytes} bytes, sha {Sha}), removed {Deleted} rows from AuditLog.",
            stamp, rows, file, bytes, sha[..12], deleted);

        return new ArchiveResult(Path.GetFileName(file), rows, bytes, sha);
    }

    // Reads an archived month back so the admin screen can search it without
    // importing anything into the database.
    public async Task<List<AuditLog>> ReadArchiveAsync(int year, int month, int max = 2000, CancellationToken ct = default)
    {
        var list = new List<AuditLog>();
        if (!IsConfigured) return list;

        var file = Path.Combine(Options.Path, year.ToString(CultureInfo.InvariantCulture), $"audit-{year:D4}-{month:D2}.jsonl.gz");
        if (!File.Exists(file)) return list;

        await using var fs = File.OpenRead(file);
        await using var gz = new GZipStream(fs, CompressionMode.Decompress);
        using var sr = new StreamReader(gz, Encoding.UTF8);
        while (list.Count < max && await sr.ReadLineAsync(ct) is string line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var row = JsonSerializer.Deserialize<AuditLog>(line);
            if (row is not null) list.Add(row);
        }
        return list;
    }

    public List<(int Year, int Month, long Bytes)> ListArchivedFiles()
    {
        var result = new List<(int, int, long)>();
        if (!IsConfigured || !Directory.Exists(Options.Path)) return result;

        foreach (var f in Directory.EnumerateFiles(Options.Path, "audit-*.jsonl.gz", SearchOption.AllDirectories))
        {
            // strip .gz then .jsonl -> "audit-2026-09"
            var name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(f));
            var parts = name.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[1], out var y) && int.TryParse(parts[2], out var m))
                result.Add((y, m, new FileInfo(f).Length));
        }
        return result.OrderByDescending(x => x.Item1).ThenByDescending(x => x.Item2).ToList();
    }

    private static async Task<string> Sha256OfFileAsync(string path, CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);
        using var sha = SHA256.Create();
        return Convert.ToHexString(await sha.ComputeHashAsync(fs, ct));
    }

    private static async Task<int> CountGzipLinesAsync(string path, CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);
        await using var gz = new GZipStream(fs, CompressionMode.Decompress);
        using var sr = new StreamReader(gz, Encoding.UTF8);
        var n = 0;
        while (await sr.ReadLineAsync(ct) is not null) n++;
        return n;
    }
}
