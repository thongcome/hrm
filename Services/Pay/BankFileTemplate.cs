namespace HRM.Services.Pay;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

// ตัวเรนเดอร์แม่แบบไฟล์ธนาคาร — สูตรล้วน ไม่แตะฐานข้อมูล (ทดสอบใน HRM.Tests/Pay/BankFileTemplateTests.cs)
//
// ช่องในแม่แบบ: {ชื่อช่อง} หรือ {ชื่อช่อง:รูปแบบ} หรือ {ชื่อช่อง:รูปแบบ,ความกว้าง} หรือ {ชื่อช่อง:รูปแบบ,ความกว้าง,ตัวเติม}
//   รูปแบบ = format ของ .NET (ตัวเลข "0.00" "N2" / วันที่ "ddMMyyyy") · ความกว้าง บวก = ชิดขวา, ลบ = ชิดซ้าย · ตัวเติม ค่าเริ่มต้นช่องว่าง
//   ตัวอย่าง {AccountNo:,-20} = เลขบัญชีชิดซ้ายกว้าง 20 · {AmountCents:,13,0} = สตางค์ชิดขวาเติม 0 กว้าง 13 · {PayDateBE:ddMMyyyy}
// ช่องรายบรรทัด: Seq EmpNo Name FirstName LastName BankCode BranchCode AccountNo Amount AmountCents IdCard Email
// ช่องหัว/ท้าย: CompanyName CompanyAccountNo CompanyBankCode CompanyBranchCode PayDate PayDateBE Period RecordCount TotalAmount TotalAmountCents BatchNo Today TodayBE
public static class BankFileTemplate
{
    public const string GenericCsvHeader = "EmpNo,Name,BankCode,BankBranchCode,BankAccountNo,Amount";
    public const string GenericCsvLine = "{EmpNo},\"{NameCsv}\",{BankCode},{BranchCode},{AccountNo},{Amount:0.00}";

    private static readonly Regex Token = new(@"\{(?<name>[A-Za-z][A-Za-z0-9]*)(?::(?<fmt>[^,}]*))?(?:,(?<width>-?\d+))?(?:,(?<pad>.))?\}", RegexOptions.Compiled);

    public static string Render(string? template, IReadOnlyDictionary<string, object?> values)
    {
        if (string.IsNullOrEmpty(template)) return "";
        return Token.Replace(template, m =>
        {
            var name = m.Groups["name"].Value;
            // {Blank:,N} หรือ {Blank:,N,ตัวเติม} — ช่องว่าง/ช่องเติมความยาวคงที่ ไม่ต้องมีในค่าที่ส่งมา
            // ธนาคารหลายแห่ง (เช่น K-Cash Connect Plus) มีช่องว่างคั่นระหว่างข้อมูลยาวหลายสิบตัวอักษรในหนึ่งบรรทัด
            if (string.Equals(name, "Blank", StringComparison.Ordinal) || string.Equals(name, "Filler", StringComparison.Ordinal))
            {
                var w = m.Groups["width"].Success ? Math.Abs(int.Parse(m.Groups["width"].Value, CultureInfo.InvariantCulture)) : 0;
                var padChar = m.Groups["pad"].Success ? m.Groups["pad"].Value[0] : ' ';
                return new string(padChar, w);
            }
            if (!values.TryGetValue(name, out var v))
                throw new InvalidOperationException($"แม่แบบไฟล์ธนาคารอ้างช่อง {{{name}}} ที่ไม่มี — ช่องที่ใช้ได้: {string.Join(", ", values.Keys)}");
            var fmt = m.Groups["fmt"].Success ? m.Groups["fmt"].Value : null;
            var text = Format(v, fmt);
            if (m.Groups["width"].Success)
            {
                var width = int.Parse(m.Groups["width"].Value, CultureInfo.InvariantCulture);
                var pad = m.Groups["pad"].Success ? m.Groups["pad"].Value[0] : ' ';
                var w = Math.Abs(width);
                if (text.Length > w) text = width > 0 ? text[^w..] : text[..w];   // ยาวเกิน: ตัวเลขชิดขวาเก็บท้าย, ข้อความชิดซ้ายเก็บหน้า
                text = width > 0 ? text.PadLeft(w, pad) : text.PadRight(w, pad);
            }
            return text;
        });
    }

    private static string Format(object? v, string? fmt) => v switch
    {
        null => "",
        decimal d => string.IsNullOrEmpty(fmt) ? d.ToString("0.00", CultureInfo.InvariantCulture) : d.ToString(fmt, CultureInfo.InvariantCulture),
        long l => string.IsNullOrEmpty(fmt) ? l.ToString(CultureInfo.InvariantCulture) : l.ToString(fmt, CultureInfo.InvariantCulture),
        int i => string.IsNullOrEmpty(fmt) ? i.ToString(CultureInfo.InvariantCulture) : i.ToString(fmt, CultureInfo.InvariantCulture),
        DateOnly dt => dt.ToString(string.IsNullOrEmpty(fmt) ? "yyyyMMdd" : fmt, CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString(string.IsNullOrEmpty(fmt) ? "yyyyMMdd" : fmt, CultureInfo.InvariantCulture),
        _ => v.ToString() ?? "",
    };

    public static long Cents(decimal amount) => (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

    public static DateOnly BuddhistEra(DateOnly d) => new(Math.Min(9999, d.Year + 543), d.Month, d.Day);

    public static string NewLine(string? lineEnding) => lineEnding?.ToUpperInvariant() switch { "LF" => "\n", "NONE" => "", _ => "\r\n" };

    public static byte[] Encode(string text, string? encoding)
    {
        switch (encoding?.ToUpperInvariant())
        {
            case "TIS620":
            case "TIS-620":
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(874).GetBytes(text);
            case "UTF8":
            case "UTF-8":
                return new UTF8Encoding(false).GetBytes(text);
            default:
                var utf8 = new UTF8Encoding(true);
                return utf8.GetPreamble().Concat(utf8.GetBytes(text)).ToArray();
        }
    }

    // ประกอบทั้งไฟล์: หัว (ถ้ามี) + บรรทัดรายคน + ท้าย (ถ้ามี)
    public static string Build(string? headerTemplate, string lineTemplate, string? trailerTemplate, string? lineEnding,
        IReadOnlyDictionary<string, object?> fileValues, IEnumerable<IReadOnlyDictionary<string, object?>> lineValues)
    {
        var nl = NewLine(lineEnding);
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(headerTemplate)) sb.Append(Render(headerTemplate, fileValues)).Append(nl);
        foreach (var lv in lineValues)
        {
            var merged = new Dictionary<string, object?>(fileValues);
            foreach (var kv in lv) merged[kv.Key] = kv.Value;
            sb.Append(Render(lineTemplate, merged)).Append(nl);
        }
        if (!string.IsNullOrEmpty(trailerTemplate)) sb.Append(Render(trailerTemplate, fileValues)).Append(nl);
        return sb.ToString();
    }
}
