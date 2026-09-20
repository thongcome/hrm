using System.Globalization;
using ClosedXML.Excel;

namespace HRM.Services.Pay.AdhocImport;

// นำเข้า "เงินได้/เงินหักรายครั้ง" จาก Excel — ไฟล์เดียวใช้ได้ทุกประเภทรายการ (CEO, 20 ก.ย. 2569)
//   คอลัมน์แรก = EMP_NO   คอลัมน์ถัดไป = รหัสประเภทรายการ (COMMISSION, ALLOWANCE, …) หรือชื่อไทยของประเภทนั้น
//   หนึ่งแถว = หนึ่งคน · ช่องว่าง/0 = ไม่สร้างรายการ · คอลัมน์ที่ระบบไม่รู้จักให้ผู้ใช้จับคู่เองบนหน้าจอ
// งวด/รอบที่จ่าย ไม่ได้อยู่ในไฟล์ — เลือกบนหน้าจอ เพื่อกันไฟล์เดือนเก่าถูกอัปโหลดเข้างวดใหม่โดยไม่รู้ตัว
//
// Parser นี้บริสุทธิ์ (ไม่แตะฐานข้อมูล) เหมือน EmployeeImportParser — รับรายชื่อพนักงานและประเภทรายการ
// ที่มีอยู่จริงเข้ามาเป็น lookup แล้วคืนรายการที่พร้อมบันทึก + ปัญหาทุกจุดพร้อมแถว/คอลัมน์
public static class AdhocImportParser
{
    public const string EmpNoHeader = "EMP_NO";

    // คอลัมน์ประกอบรายแถว (CEO, 20 ก.ย. 2569: "ต้องเพิ่ม field มากกว่านี้ เช่น วันที่สรุป รายละเอียด") — ใช้กับทุกรายการในแถวนั้น
    // หัวคอลัมน์รับได้ทั้งไทยและรหัสอังกฤษ ไม่บังคับต้องมี
    public const string ItemDateHeader = "วันที่สรุป";
    public const string RemarkHeader = "รายละเอียด";
    public const string ReferenceNoHeader = "เลขที่อ้างอิง";

    // แถว 2 ของเทมเพลต = เลขบัญชี GL ของแต่ละประเภท (ไว้ให้บัญชีดู) — ระบบไม่อ่าน ถ้าคอลัมน์ A แถว 2 ขึ้นต้นด้วยป้ายนี้ ข้อมูลเริ่มแถว 3
    public const string AccountRowLabel = "บัญชี GL";

    public sealed record Issue(int Row, string Column, string Message);

    public sealed record Line(int Row, string EmpNo, long HremployeeId, string ItemCode, int PayItemTypeId, decimal Amount,
        DateTime? ItemDate = null, string? Remark = null, string? ReferenceNo = null);

    public sealed record Column(int Index, string Header, int? PayItemTypeId, string? ItemCode, bool Recognised);

    // ตรางวดที่เทมเพลตของระบบฝังไว้ (ชีต "ข้อมูลไฟล์" + Subject ของไฟล์) — CEO 20 ก.ย. 2569: "lock หรือเช็คให้ถูกว่างวดไหน
    // เพราะอีกหน่อยเอาไฟล์นี้มาวนซ้ำ หรือทำผิดไฟล์" หน้าจอเทียบตรานี้กับงวดที่เลือก ไม่ตรง = ไม่นำเข้า · ไฟล์ของลูกค้าเองไม่มีตรา = null
    public const string StampSheetName = "ข้อมูลไฟล์";
    public const string GuideSheetName = "วิธีใช้";
    public sealed record FileStamp(string Period, string RunType, int? TermNo, string? GeneratedAt, string? GeneratedBy);

    public sealed record Result(IReadOnlyList<Line> Lines, IReadOnlyList<Column> Columns, IReadOnlyList<Issue> Issues, FileStamp? Stamp = null)
    {
        public bool HasErrors => Issues.Count > 0;
        public decimal Total => Lines.Sum(l => l.Amount);
        public int EmployeeCount => Lines.Select(l => l.EmpNo).Distinct().Count();
    }

    // ประเภทรายการที่ใช้ได้: รหัส -> (id, ชื่อไทย) · พนักงาน: EMP_NO -> id (ทั้งคู่ไม่สนตัวพิมพ์)
    public sealed record Lookups(
        IReadOnlyDictionary<string, (int Id, string NameTh)> ItemTypes,
        IReadOnlyDictionary<string, long> Employees);

    /// <param name="columnOverrides">คอลัมน์ที่ผู้ใช้จับคู่เองบนหน้าจอ: ดัชนีคอลัมน์ -> รหัสประเภทรายการ</param>
    public static Result Parse(Stream excel, Lookups lookups, IReadOnlyDictionary<int, string>? columnOverrides = null)
    {
        var issues = new List<Issue>();
        var lines = new List<Line>();
        var columns = new List<Column>();

        using var wb = new XLWorkbook(excel);
        var stamp = ReadStamp(wb);
        var ws = wb.Worksheets.FirstOrDefault(w => w.Name != StampSheetName && w.Name != GuideSheetName);
        if (ws is null)
        {
            issues.Add(new Issue(0, "", "ไฟล์นี้ไม่มีชีตข้อมูล"));
            return new Result(lines, columns, issues, stamp);
        }

        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
        if (lastCol < 2 || lastRow < 2)
        {
            issues.Add(new Issue(0, "", "ไฟล์นี้ยังไม่มีข้อมูล — ต้องมีคอลัมน์ EMP_NO และอย่างน้อยหนึ่งคอลัมน์ประเภทรายการ พร้อมข้อมูลอย่างน้อยหนึ่งแถว"));
            return new Result(lines, columns, issues, stamp);
        }

        // ── หัวตาราง ─────────────────────────────────────────────────────────
        var empNoColumn = 0;
        int itemDateColumn = 0, remarkColumn = 0, referenceColumn = 0;
        var byName = lookups.ItemTypes.ToDictionary(kv => Normalise(kv.Value.NameTh), kv => kv.Key, StringComparer.OrdinalIgnoreCase);
        for (var c = 1; c <= lastCol; c++)
        {
            var header = ws.Cell(1, c).GetString().Trim();
            if (header.Length == 0) continue;

            if (empNoColumn == 0 && Normalise(header) is "empno" or "รหัสพนักงาน" or "empid")
            {
                empNoColumn = c;
                columns.Add(new Column(c, header, null, null, true));
                continue;
            }
            // คอลัมน์ประกอบรายแถว — รู้จักแต่ไม่ใช่ประเภทรายการ
            var norm = Normalise(header);
            if (itemDateColumn == 0 && norm is "วันที่สรุป" or "วันที่" or "itemdate" or "date")
            { itemDateColumn = c; columns.Add(new Column(c, header, null, null, true)); continue; }
            if (remarkColumn == 0 && norm is "รายละเอียด" or "หมายเหตุ" or "remark" or "note" or "detail")
            { remarkColumn = c; columns.Add(new Column(c, header, null, null, true)); continue; }
            if (referenceColumn == 0 && norm is "เลขที่อ้างอิง" or "เลขที่เอกสาร" or "อ้างอิง" or "refno" or "reference" or "ref")
            { referenceColumn = c; columns.Add(new Column(c, header, null, null, true)); continue; }
            if (norm.StartsWith("ชื่อ")) { columns.Add(new Column(c, header, null, null, true)); continue; }   // ชื่อ-สกุล มีไว้ให้ดู

            var code = columnOverrides is not null && columnOverrides.TryGetValue(c, out var picked) ? picked
                : lookups.ItemTypes.ContainsKey(header) ? header
                : byName.TryGetValue(Normalise(header), out var byThai) ? byThai
                : null;

            columns.Add(code is not null && lookups.ItemTypes.TryGetValue(code, out var t)
                ? new Column(c, header, t.Id, code, true)
                : new Column(c, header, null, null, false));
        }

        if (empNoColumn == 0)
        {
            issues.Add(new Issue(1, EmpNoHeader, $"ไม่พบคอลัมน์ {EmpNoHeader} (รหัสพนักงาน) — ต้องมีหนึ่งคอลัมน์ชื่อนี้"));
            return new Result(lines, columns, issues, stamp);
        }
        if (!columns.Any(c => c.PayItemTypeId is not null))
        {
            issues.Add(new Issue(1, "", "ไม่พบคอลัมน์ประเภทรายการที่ระบบรู้จัก — จับคู่คอลัมน์บนหน้าจอ หรือใช้รหัสประเภทรายการเป็นหัวคอลัมน์"));
            return new Result(lines, columns, issues, stamp);
        }

        // ── ข้อมูล ───────────────────────────────────────────────────────────
        var firstDataRow = ws.Cell(2, empNoColumn).GetString().Trim().StartsWith("บัญชี", StringComparison.Ordinal) ? 3 : 2;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var r = firstDataRow; r <= lastRow; r++)
        {
            var empNo = ws.Cell(r, empNoColumn).GetString().Trim();
            if (empNo.Length == 0 && columns.Where(c => c.PayItemTypeId is not null)
                    .All(c => ws.Cell(r, c.Index).GetString().Trim().Length == 0))
                continue;   // แถวว่างทั้งแถว — ข้ามเงียบ ๆ

            if (empNo.Length == 0)
            {
                issues.Add(new Issue(r, EmpNoHeader, "แถวนี้มีจำนวนเงินแต่ไม่มีรหัสพนักงาน"));
                continue;
            }
            if (!lookups.Employees.TryGetValue(empNo, out var empId))
            {
                issues.Add(new Issue(r, EmpNoHeader, $"ไม่พบพนักงานรหัส {empNo} ในบริษัทนี้ (หรือพ้นสภาพแล้ว)"));
                continue;
            }
            if (!seen.Add(empNo))
            {
                issues.Add(new Issue(r, EmpNoHeader, $"รหัส {empNo} ซ้ำในไฟล์ — รวมให้เหลือแถวเดียวต่อคน"));
                continue;
            }

            DateTime? itemDate = null;
            if (itemDateColumn > 0)
            {
                var dateCell = ws.Cell(r, itemDateColumn);
                var rawDate = dateCell.GetString().Trim();
                if (rawDate.Length > 0)
                {
                    if (TryParseDate(dateCell, rawDate, out var d)) itemDate = d;
                    else { issues.Add(new Issue(r, ItemDateHeader, $"\"{rawDate}\" ไม่ใช่วันที่ (ใช้ วว/ดด/ปปปป — ปี พ.ศ. หรือ ค.ศ. ก็ได้)")); continue; }
                }
            }
            var remark = remarkColumn > 0 ? Truncate(ws.Cell(r, remarkColumn).GetString().Trim(), 500) : null;
            var reference = referenceColumn > 0 ? Truncate(ws.Cell(r, referenceColumn).GetString().Trim(), 100) : null;

            foreach (var col in columns.Where(c => c.PayItemTypeId is not null))
            {
                var cell = ws.Cell(r, col.Index);
                var raw = cell.GetString().Trim();
                if (raw.Length == 0) continue;

                if (!TryParseAmount(cell, raw, out var amount))
                {
                    issues.Add(new Issue(r, col.Header, $"\"{raw}\" ไม่ใช่จำนวนเงิน"));
                    continue;
                }
                if (amount == 0m) continue;          // 0 = ไม่มีรายการ ไม่ใช่ข้อผิดพลาด
                if (amount < 0m)
                {
                    issues.Add(new Issue(r, col.Header, "จำนวนเงินติดลบไม่ได้ — เงินหักให้ใช้คอลัมน์ของประเภทเงินหัก และใส่เป็นจำนวนบวก"));
                    continue;
                }

                lines.Add(new Line(r, empNo, empId, col.ItemCode!, col.PayItemTypeId!.Value, decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
                    itemDate, remark, reference));
            }
        }

        if (lines.Count == 0 && issues.Count == 0)
            issues.Add(new Issue(0, "", "ไม่มีจำนวนเงินให้บันทึกสักรายการ"));

        return new Result(lines, columns, issues, stamp);
    }

    // ชีต "ข้อมูลไฟล์" (B1 งวด, B2 รอบ, B3 งวดที่, B4 สร้างเมื่อ, B5 สร้างโดย) — ถ้าคนลบชีตทิ้ง ยังมี Subject ของไฟล์เป็นสำรอง
    private static FileStamp? ReadStamp(XLWorkbook wb)
    {
        var ws = wb.Worksheets.FirstOrDefault(w => w.Name == StampSheetName);
        if (ws is not null)
        {
            var period = ws.Cell(1, 2).GetString().Trim();
            if (period.Length == 6 && int.TryParse(period, out _))
            {
                var term = ws.Cell(3, 2).GetString().Trim();
                return new FileStamp(period, ws.Cell(2, 2).GetString().Trim(), int.TryParse(term, out var t) ? t : null,
                    ws.Cell(4, 2).GetString().Trim(), ws.Cell(5, 2).GetString().Trim());
            }
        }
        var subject = wb.Properties.Subject ?? "";
        if (!subject.StartsWith("ADP-ADHOC;", StringComparison.Ordinal)) return null;
        var kv = subject.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2)).Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);
        if (!kv.TryGetValue("PERIOD", out var sp) || sp.Length != 6 || !int.TryParse(sp, out _)) return null;
        return new FileStamp(sp, kv.GetValueOrDefault("RUN") ?? "", int.TryParse(kv.GetValueOrDefault("TERM"), out var st) ? st : null, null, null);
    }

    public static string WriteStampSubject(string period, string runType, int? termNo) => $"ADP-ADHOC;PERIOD={period};RUN={runType};TERM={termNo}";

    private static bool TryParseAmount(IXLCell cell, string raw, out decimal amount)
    {
        if (cell.DataType == XLDataType.Number) { amount = (decimal)cell.GetDouble(); return true; }
        var cleaned = raw.Replace(",", "").Replace("บาท", "").Trim();
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out amount)
               || decimal.TryParse(cleaned, NumberStyles.Any, new CultureInfo("th-TH"), out amount);
    }

    // Excel เก็บวันที่เป็นตัวเลขถ้าช่องเป็น date จริง; ถ้าพิมพ์เป็นข้อความ รับ วว/ดด/ปปปป ทั้ง พ.ศ. (2569) และ ค.ศ. (2026)
    private static bool TryParseDate(IXLCell cell, string raw, out DateTime date)
    {
        if (cell.DataType == XLDataType.DateTime) { date = cell.GetDateTime().Date; return true; }
        var formats = new[] { "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "yyyy-MM-dd", "d/M/yy" };
        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            if (date.Year > 2400) date = date.AddYears(-543);   // พ.ศ. → ค.ศ.
            date = date.Date;
            return true;
        }
        return false;
    }

    private static string? Truncate(string s, int max) => s.Length == 0 ? null : s.Length <= max ? s : s[..max];

    private static string Normalise(string s) => s.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
}
