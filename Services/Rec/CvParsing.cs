using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace HRM.Services.Rec;

// ---------------------------------------------------------------------------
// CV / resume parsing — customer spec REQ-038 and the CEO's "อ่าน PDF/Word แล้ว
// เอาเข้า DB". Two layers, both replaceable:
//   1. CvTextExtractor  — file bytes -> plain text (PDF via PdfPig, DOCX via
//      OpenXml, images via the existing Tesseract OCR). No network.
//   2. ICvParser        — text -> structured fields. RegexCvParser is the
//      free, in-process default; an LLM-backed parser can be registered later
//      behind the same interface (config "CvParser:Provider") without touching
//      the pages. Whatever the parser returns is a SUGGESTION: every page shows
//      the fields for a person to check before anything is saved.
// ---------------------------------------------------------------------------

public record CvEducation(string? Level, string? Degree, string? Major, string? Institute, int? FinishedYear);
public record CvExperience(string? Position, string? Company, int? StartYear, int? EndYear, bool IsCurrent);

public record CvParseResult(
    string? FirstName, string? LastName, string? Email, string? Phone, string? NationalId, DateTime? BirthDate,
    List<CvEducation> Education, List<CvExperience> Experience, List<string> Skills, string RawText)
{
    public string? FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public interface ICvParser
{
    string Name { get; }
    Task<CvParseResult> ParseAsync(string text, CancellationToken ct = default);
}

public static class CvTextExtractor
{
    public static readonly string[] SupportedExtensions = { ".pdf", ".docx", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp" };

    public static async Task<string> ExtractTextAsync(byte[] bytes, string? fileName, string? contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (ext == ".pdf" || (contentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) ?? false))
            return await Task.Run(() => ExtractPdf(bytes), ct);
        if (ext == ".docx" || (contentType?.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase) ?? false))
            return await Task.Run(() => ExtractDocx(bytes), ct);
        if (ext == ".doc")
            throw new InvalidOperationException("ไฟล์ .doc (Word 97-2003) อ่านอัตโนมัติไม่ได้ กรุณาบันทึกเป็น .docx หรือ PDF");
        if (contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true || ext is ".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff" or ".bmp")
            return await CvReaderService.ReadTextAsync(bytes, ct);
        throw new InvalidOperationException("รองรับ PDF, DOCX หรือรูปภาพ (JPG/PNG) เท่านั้น");
    }

    private static string ExtractPdf(byte[] bytes)
    {
        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(bytes);
        foreach (var page in doc.GetPages())
        {
            // ContentOrderTextExtractor keeps reading order and line breaks, which
            // the line-oriented heuristics below depend on.
            sb.AppendLine(ContentOrderTextExtractor.GetText(page));
            sb.AppendLine();
        }
        var text = sb.ToString();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("PDF นี้ไม่มีข้อความ (น่าจะเป็นไฟล์สแกน) — กรุณาอัปโหลดเป็นรูปภาพเพื่อใช้ OCR แทน");
        return text;
    }

    private static string ExtractDocx(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return "";
        var sb = new StringBuilder();
        foreach (var p in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            sb.AppendLine(p.InnerText);
        return sb.ToString();
    }
}

// Heuristic parser for Thai/English CVs. Deliberately conservative: it would
// rather leave a field empty than fill it wrongly, because a person reviews the
// result. Name detection is the weakest part (that is why the earlier OCR
// path never attempted it); here it only fires on explicit "ชื่อ:" / "Name:"
// labels or a short header line with no digits.
public class RegexCvParser : ICvParser
{
    public string Name => "Regex (in-process)";

    private static readonly Regex NameLabel = new(@"^\s*(?:ชื่อ(?:\s*-\s*(?:นามสกุล|สกุล))?|Full\s*Name|Name)\s*[:：]\s*(?<n>.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex EduKeyword = new(@"ปริญญาเอก|ปริญญาโท|ปริญญาตรี|ปวส\.?|ปวช\.?|มัธยม|อนุปริญญา|Ph\.?D|Doctor|Master|M\.?Sc|M\.?Eng|MBA|M\.?A\b|Bachelor|B\.?Sc|B\.?Eng|B\.?A\b|B\.?B\.?A|Diploma|High\s*School", RegexOptions.IgnoreCase);
    // The trailing repeat group deliberately excludes a bare 4-digit year token
    // (a graduation year sitting right after the institute name on the same
    // line, e.g. Thai "...มหาวิทยาลัยเกษตรศาสตร์ 2558") so it isn't swallowed
    // into the institute name.
    private static readonly Regex Institute = new(@"(?<i>(?:มหาวิทยาลัย|วิทยาลัย|สถาบัน|โรงเรียน)[^\s,|/()]*(?:\s+(?!(?:19|20|25)\d{2}\b)[^\s,|/()]+){0,3}|[A-Z][A-Za-z&.\- ]{2,60}?(?:University|College|Institute|School|Academy)(?:\s+of\s+[A-Z][A-Za-z ]{2,40})?)");
    // Non-greedy, and stops before an institute keyword/comma/pipe/paren/year so
    // it doesn't run on into the institute name or graduation year that follows
    // on the same line with no separating comma (common in Thai CVs).
    private static readonly Regex Major = new(@"(?:สาขา(?:วิชา)?|Major(?:\s*in)?|in|วิชาเอก)\s*[:：]?\s*(?<m>[^\s,|/()][^,|/()\n]{1,60}?)(?=\s+(?:มหาวิทยาลัย|วิทยาลัย|สถาบัน|โรงเรียน|(?:19|20|25)\d{2}\b)|[,|/()\n]|$)", RegexOptions.IgnoreCase);
    private static readonly Regex YearRange = new(@"(?<s>(?:19|20|25)\d{2})\s*(?:[-–—]|to|ถึง)\s*(?<e>(?:19|20|25)\d{2}|ปัจจุบัน|present|now|current)", RegexOptions.IgnoreCase);
    private static readonly Regex Year = new(@"\b(?:19|20|25)\d{2}\b");
    private static readonly Regex SkillsHeading = new(@"^\s*(?:ทักษะ|ความสามารถ(?:พิเศษ)?|Skills?|Technical\s*Skills|Core\s*Competenc\w+)\s*[:：]?\s*(?<rest>.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex Heading = new(@"^\s*(?:ประวัติการศึกษา|การศึกษา|Education|ประสบการณ์(?:การทำงาน)?|Work\s*Experience|Experience|Employment|ทักษะ|Skills?|ข้อมูลส่วนตัว|Personal|Contact|References?|Certificat\w+|Languages?|ภาษา)\b", RegexOptions.IgnoreCase);

    public Task<CvParseResult> ParseAsync(string text, CancellationToken ct = default)
    {
        var basic = CvReaderService.ParseText(text);
        var lines = text.Replace("\r", "").Split('\n').Select(l => l.Trim()).ToList();

        // --- name ---
        string? first = null, last = null;
        var nm = NameLabel.Match(text);
        var nameText = nm.Success ? nm.Groups["n"].Value.Trim() : null;
        if (nameText is null)
        {
            // header line: first non-empty line with 2-4 tokens, no digits, no @, not a heading, not too long
            nameText = lines.Take(8).FirstOrDefault(l => l.Length is >= 4 and <= 60 && !l.Any(char.IsDigit) && !l.Contains('@')
                                                          && !Heading.IsMatch(l) && l.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length is >= 2 and <= 4);
        }
        if (nameText is not null)
        {
            nameText = Regex.Replace(nameText, @"^(?:นาย|นางสาว|นาง|น\.ส\.|Mr\.?|Mrs\.?|Ms\.?|Miss)\s*", "", RegexOptions.IgnoreCase).Trim();
            var parts = nameText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) { first = parts[0]; last = string.Join(" ", parts.Skip(1)); }
            else if (parts.Length == 1) first = parts[0];
        }

        // --- education: any line with a degree keyword; institute/major/year read
        // from that line plus a bounded lookahead. The lookahead stops at the
        // first blank line, section heading, or the START of another degree
        // entry — without that bound, a plain "take the next 2 lines" window
        // bleeds into the next degree (or the Experience section right below
        // it) and grabs ITS year/institute instead of the current one, which
        // silently produces a confidently wrong suggestion rather than just a
        // missing one.
        var education = new List<CvEducation>();
        for (var i = 0; i < lines.Count; i++)
        {
            var kw = EduKeyword.Match(lines[i]);
            if (!kw.Success) continue;
            var windowEnd = i + 1;
            while (windowEnd < lines.Count && windowEnd < i + 4 && lines[windowEnd].Length > 0
                   && !Heading.IsMatch(lines[windowEnd]) && !EduKeyword.IsMatch(lines[windowEnd]))
                windowEnd++;
            var window = string.Join(" | ", lines.Skip(i).Take(windowEnd - i));
            var inst = Institute.Match(window);
            var major = Major.Match(window);
            var year = Year.Matches(window).Select(m => int.Parse(m.Value)).Select(NormalizeYear).OrderByDescending(y => y).FirstOrDefault();
            var level = LevelFor(kw.Value);
            if (education.Any(e => e.Level == level && e.Institute == (inst.Success ? inst.Groups["i"].Value.Trim() : null))) continue;
            education.Add(new CvEducation(level, kw.Value, major.Success ? major.Groups["m"].Value.Trim() : null,
                inst.Success ? inst.Groups["i"].Value.Trim() : null, year == 0 ? null : year));
            if (education.Count >= 5) break;
        }

        // --- experience: year ranges; position/company from the range line and its neighbours ---
        var experience = new List<CvExperience>();
        for (var i = 0; i < lines.Count; i++)
        {
            var yr = YearRange.Match(lines[i]);
            if (!yr.Success || EduKeyword.IsMatch(lines[i])) continue;
            var start = NormalizeYear(int.Parse(yr.Groups["s"].Value));
            var endRaw = yr.Groups["e"].Value;
            var isCurrent = !int.TryParse(endRaw, out var endParsed);
            int? end = isCurrent ? null : NormalizeYear(endParsed);
            var rest = YearRange.Replace(lines[i], "").Trim(' ', '-', '–', '|', ',', ':', '(', ')');
            var nextLine = i + 1 < lines.Count ? lines[i + 1] : "";
            string? position = null, company = null;
            var pieces = rest.Split(new[] { " - ", " – ", " | ", ", ", " at ", " @ ", " ที่ " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (pieces.Length >= 2) { position = pieces[0]; company = pieces[1]; }
            else if (pieces.Length == 1) { position = pieces[0]; company = !Heading.IsMatch(nextLine) && !YearRange.IsMatch(nextLine) && nextLine.Length is > 2 and < 80 ? nextLine : null; }
            else { position = !Heading.IsMatch(nextLine) && nextLine.Length is > 2 and < 80 ? nextLine : null; }
            experience.Add(new CvExperience(Truncate(position, 150), Truncate(company, 150), start, end, isCurrent));
            if (experience.Count >= 8) break;
        }

        // --- skills: everything after a skills heading until a blank line or the next heading ---
        var skills = new List<string>();
        var sh = SkillsHeading.Match(text);
        if (sh.Success)
        {
            var collected = new StringBuilder(sh.Groups["rest"].Value);
            var idx = lines.FindIndex(l => SkillsHeading.IsMatch(l));
            for (var i = idx + 1; i < lines.Count && i < idx + 12; i++)
            {
                if (lines[i].Length == 0 || Heading.IsMatch(lines[i])) break;
                collected.Append(',').Append(lines[i]);
            }
            skills = collected.ToString().Split(new[] { ',', '•', '·', '|', ';', '/', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.Trim('-', ' ')).Where(s => s.Length is >= 2 and <= 40).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
        }

        return Task.FromResult(new CvParseResult(first, last, basic.Email, basic.Phone, basic.NationalId, basic.BirthDate, education, experience, skills, text));
    }

    // Thai CVs often give Buddhist years (2562); normalise to Gregorian for the DB.
    private static int NormalizeYear(int y) => y >= 2400 ? y - 543 : y;

    private static string LevelFor(string keyword)
    {
        var k = keyword.ToLowerInvariant();
        if (k.Contains("เอก") || k.Contains("ph") || k.Contains("doctor")) return "ปริญญาเอก";
        if (k.Contains("โท") || k.StartsWith("m") ) return "ปริญญาโท";
        if (k.Contains("ตรี") || k.StartsWith("b")) return "ปริญญาตรี";
        if (k.Contains("ปวส") || k.Contains("อนุปริญญา") || k.Contains("diploma")) return "ปวส./อนุปริญญา";
        if (k.Contains("ปวช")) return "ปวช.";
        return "มัธยม";
    }

    private static string? Truncate(string? s, int max) => s is null ? null : s.Length <= max ? s : s[..max];
}

// Facade the pages use: file -> text -> fields, with the provider chosen in DI.
public class CvParserService(ICvParser parser)
{
    public string ProviderName => parser.Name;

    public async Task<CvParseResult> ParseFileAsync(byte[] bytes, string? fileName, string? contentType, CancellationToken ct = default)
    {
        var text = await CvTextExtractor.ExtractTextAsync(bytes, fileName, contentType, ct);
        return await parser.ParseAsync(text, ct);
    }
}
