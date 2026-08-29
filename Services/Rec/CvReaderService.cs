namespace HRM.Services.Rec;

using System.Globalization;
using System.Text.RegularExpressions;
using Tesseract;

// Fields lifted from a candidate's CV/resume image by OCR (all nullable — the
// candidate/HR user verifies before saving). FullName is intentionally not
// extracted — see ParseText.
public record CvExtract(string? FullName, string? Email, string? Phone, string? NationalId, DateTime? BirthDate);

// Free, in-process CV reader using Tesseract OCR (bundled Thai+English
// traineddata). Mirrors AccountingFirm's TesseractReceiptReader, scoped down
// for HRM: no DI/interface abstraction (no paid alternative engine exists
// here), and it extracts CV-relevant fields (name/email/phone/national
// id/birth date) instead of receipt/tax fields. Static class — call directly,
// no registration needed.
public static class CvReaderService
{
    private static readonly string _tessdata = Path.Combine(AppContext.BaseDirectory, "tessdata");

    // Enabled when the bundled Thai traineddata is present in the app's tessdata folder.
    public static bool Enabled => File.Exists(Path.Combine(_tessdata, "tha.traineddata"));

    public static Task<CvExtract> ReadAsync(byte[] content, string? contentType, CancellationToken ct = default)
    {
        if (contentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true)
            throw new InvalidOperationException("OCR อ่าน CV รองรับเฉพาะไฟล์รูปภาพ (PDF กรุณาแปลงเป็นรูปภาพก่อน)");
        if (!Enabled)
            throw new InvalidOperationException("ยังไม่ได้ติดตั้งไฟล์ tha.traineddata สำหรับ OCR");

        // Run the (native, blocking) OCR off the request thread.
        return Task.Run(() =>
        {
            using var engine = new TesseractEngine(_tessdata, "tha+eng", EngineMode.Default);
            using var img = Pix.LoadFromMemory(content);
            using var page = engine.Process(img);
            return ParseText(page.GetText());
        }, ct);
    }

    // Heuristically pulls national id, email, phone, and birth date out of raw OCR text
    // (public for tests). FullName is deliberately left null: Thai/English name extraction
    // from raw OCR text (with no reliable label like "ชื่อ-นามสกุล" guaranteed to be present,
    // and no guaranteed line order) is unreliable with simple regex — a fragile heuristic here
    // would confidently overwrite a candidate's real name with garbage more often than it would
    // help, so the user fills FullName in manually instead.
    public static CvExtract ParseText(string text)
    {
        var t = text ?? "";

        // 13-digit Thai national id, tolerating spaces/dashes between digits.
        string? nationalId = null;
        var idm = Regex.Match(t, @"(?<!\d)(\d[ \-]?){13}(?!\d)");
        if (idm.Success)
        {
            var digits = new string(idm.Value.Where(char.IsDigit).ToArray());
            if (digits.Length == 13) nationalId = digits;
        }

        // Standard email address.
        string? email = null;
        var em = Regex.Match(t, @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}");
        if (em.Success) email = em.Value;

        // Thai mobile number: 10 digits starting with 0, tolerating dashes/spaces
        // (e.g. "08-1234-5678", "081 234 5678", "0812345678").
        string? phone = null;
        var pm = Regex.Match(t, @"(?<!\d)0[ \-]?\d[ \-]?\d[ \-]?\d[ \-]?\d[ \-]?\d[ \-]?\d[ \-]?\d[ \-]?\d(?!\d)");
        if (pm.Success)
        {
            var digits = new string(pm.Value.Where(char.IsDigit).ToArray());
            if (digits.Length == 10) phone = digits;
        }

        // Date dd/mm/yyyy; convert a Buddhist year to CE.
        DateTime? birthDate = null;
        var dm = Regex.Match(t, @"\b(\d{1,2})[/.\-](\d{1,2})[/.\-](\d{4})\b");
        if (dm.Success && int.TryParse(dm.Groups[1].Value, out var d) && int.TryParse(dm.Groups[2].Value, out var mo) && int.TryParse(dm.Groups[3].Value, out var y))
        {
            if (y > 2300) y -= 543; // Buddhist -> Gregorian
            try { birthDate = new DateTime(y, Math.Clamp(mo, 1, 12), Math.Clamp(d, 1, 28)); } catch { }
        }
        else
        {
            var dm2 = Regex.Match(t, @"\b(\d{4})-(\d{1,2})-(\d{1,2})\b");
            if (dm2.Success) { try { birthDate = DateTime.Parse(dm2.Value, CultureInfo.InvariantCulture); } catch { } }
        }

        return new CvExtract(null, email, phone, nationalId, birthDate);
    }
}
