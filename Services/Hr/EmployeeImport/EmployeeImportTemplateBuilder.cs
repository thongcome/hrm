using ClosedXML.Excel;

namespace HRM.Services.Hr.EmployeeImport;

public sealed record ImportLookupItem(string Code, string Name)
{
    // What the customer picks in the dropdown; the importer takes the part before " - ".
    public string Display => string.IsNullOrWhiteSpace(Name) ? Code : $"{Code} - {Name}";
}

public sealed record ImportOrgRow(string Code, string Name, string? ParentCode, string? ApproverEmpNo);

public sealed record EmployeeImportLookups(
    string CompanyCode,
    string CompanyName,
    IReadOnlyList<ImportLookupItem> Banks,
    IReadOnlyList<ImportLookupItem> Positions,
    IReadOnlyList<ImportOrgRow> Orgs);

// Builds the .xlsx the customer fills in. Pure (no DB) so it is unit-testable; the lookups
// come from EmployeeImportTemplateService. Every column in EmployeeImportSchema gets Excel
// data validation (dropdown / date / number / length) so most mistakes are stopped while
// typing, before the file ever reaches the importer — which re-checks everything anyway.
public static class EmployeeImportTemplateBuilder
{
    public const string ListsSheetName = "_lists";
    public const string GuideSheetName = "คำอธิบาย";

    private static readonly XLColor HeaderRequired = XLColor.FromHtml("#F4B183");
    private static readonly XLColor HeaderOptional = XLColor.FromHtml("#DDEBF7");
    private static readonly XLColor Brand = XLColor.FromHtml("#1F2A44");

    public static byte[] Build(EmployeeImportLookups lookups)
    {
        using var wb = new XLWorkbook();
        var guide = wb.AddWorksheet(GuideSheetName);
        var sheets = EmployeeImportSchema.DataSheets.ToDictionary(s => s.Name, s => wb.AddWorksheet(s.Name));
        var lists = wb.AddWorksheet(ListsSheetName);

        var listRanges = WriteLists(lists, lookups);
        lists.Visibility = XLWorksheetVisibility.Hidden;

        // Ranges for dropdowns that point at the customer's own rows on another data sheet.
        listRanges[EmployeeImportSchema.ListOrgSheet] =
            FirstColumnRange(sheets[EmployeeImportSchema.Org.Name], EmployeeImportSchema.Org.MaxRows);
        listRanges[EmployeeImportSchema.ListEmployeeSheet] =
            FirstColumnRange(sheets[EmployeeImportSchema.Employee.Name], EmployeeImportSchema.Employee.MaxRows);

        foreach (var sheet in EmployeeImportSchema.DataSheets)
            WriteDataSheet(sheets[sheet.Name], sheet, listRanges);

        WriteOrgRows(sheets[EmployeeImportSchema.Org.Name], lookups.Orgs);
        WriteGuide(guide, lookups);
        guide.SetTabActive();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static IXLRange FirstColumnRange(IXLWorksheet ws, int maxRows) => ws.Range(2, 1, maxRows + 1, 1);

    private static Dictionary<string, IXLRange> WriteLists(IXLWorksheet ws, EmployeeImportLookups lookups)
    {
        var columns = new List<(string Name, IEnumerable<string> Values)>
        {
            (EmployeeImportSchema.ListPrename, EmployeeImportSchema.Prenames.Select(p => p.Text)),
            (EmployeeImportSchema.ListSex, EmployeeImportSchema.Sexes.Select(s => s.Text)),
            (EmployeeImportSchema.ListEmpType, EmployeeImportSchema.EmpTypes.Select(t => t.Text)),
            (EmployeeImportSchema.ListPayType, [EmployeeImportSchema.PayMonthly, EmployeeImportSchema.PayDaily]),
            (EmployeeImportSchema.ListYesNo, [EmployeeImportSchema.Yes, EmployeeImportSchema.No]),
            (EmployeeImportSchema.ListBank, lookups.Banks.Select(b => b.Display)),
            (EmployeeImportSchema.ListPosition, lookups.Positions.Select(p => p.Display)),
        };

        var ranges = new Dictionary<string, IXLRange>();
        for (var c = 0; c < columns.Count; c++)
        {
            var (name, values) = columns[c];
            var col = c + 1;
            ws.Cell(1, col).Value = name;
            var row = 2;
            foreach (var v in values) ws.Cell(row++, col).Value = v;
            // An empty list still needs a valid range, or Excel rejects the validation.
            ranges[name] = ws.Range(2, col, Math.Max(2, row - 1), col);
        }
        return ranges;
    }

    private static void WriteDataSheet(IXLWorksheet ws, ImportSheet sheet, Dictionary<string, IXLRange> listRanges)
    {
        var lastRow = sheet.MaxRows + 1;
        for (var i = 0; i < sheet.Columns.Count; i++)
        {
            var col = i + 1;
            var def = sheet.Columns[i];
            var header = ws.Cell(1, col);
            header.Value = def.Header;
            header.Style.Font.SetBold();
            header.Style.Fill.BackgroundColor = def.Required ? HeaderRequired : HeaderOptional;
            header.Style.Alignment.WrapText = true;
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            if (!string.IsNullOrWhiteSpace(def.Note))
                header.CreateComment().AddText(def.Note);
            ws.Column(col).Width = def.Width;

            var body = ws.Range(2, col, lastRow, col);
            switch (def.Kind)
            {
                // Text format keeps leading zeros (bank codes, account and ID-card numbers).
                case ImportColumnKind.Code:
                case ImportColumnKind.Text:
                case ImportColumnKind.List:
                    body.Style.NumberFormat.Format = "@";
                    break;
                case ImportColumnKind.Date:
                    body.Style.NumberFormat.Format = "dd/mm/yyyy";
                    break;
                case ImportColumnKind.Money:
                    body.Style.NumberFormat.Format = "#,##0.00";
                    break;
                case ImportColumnKind.Percent:
                    body.Style.NumberFormat.Format = "0.00";
                    break;
            }
            AddValidation(body, def, listRanges);
        }
        ws.Row(1).Height = 32;
        ws.SheetView.FreezeRows(1);
        ws.Range(1, 1, 1, sheet.Columns.Count).SetAutoFilter();
    }

    private static void AddValidation(IXLRange body, ImportColumn def, Dictionary<string, IXLRange> listRanges)
    {
        var needed = def.Kind switch
        {
            ImportColumnKind.List => def.ListName is not null && listRanges.ContainsKey(def.ListName),
            ImportColumnKind.Code or ImportColumnKind.Text => def.ExactLength is not null || def.MaxLength is not null,
            _ => true,
        };
        if (!needed) return;

        var dv = body.CreateDataValidation();
        dv.IgnoreBlanks = true;
        dv.ShowErrorMessage = true;
        dv.ErrorStyle = XLErrorStyle.Stop;
        dv.ErrorTitle = def.Header.TrimEnd('*');

        switch (def.Kind)
        {
            case ImportColumnKind.List when def.ListName is not null && listRanges.TryGetValue(def.ListName, out var range):
                dv.List(range, true);
                dv.ErrorMessage = "กรุณาเลือกจากรายการ";
                break;
            case ImportColumnKind.Date:
                dv.Date.Between(new DateTime(1900, 1, 1), new DateTime(2700, 12, 31));
                dv.ErrorMessage = "กรุณากรอกเป็นวันที่ เช่น 01/06/2024";
                break;
            case ImportColumnKind.Money:
                dv.Decimal.EqualOrGreaterThan(0);
                dv.ErrorMessage = "กรุณากรอกตัวเลขตั้งแต่ 0 ขึ้นไป";
                break;
            case ImportColumnKind.Percent:
                dv.Decimal.Between(0, 100);
                dv.ErrorMessage = "กรุณากรอกเปอร์เซ็นต์ 0–100";
                break;
            case ImportColumnKind.WholeNumber when def.Key == "Month":
                dv.WholeNumber.Between(1, 12);
                dv.ErrorMessage = "เดือน 1–12";
                break;
            case ImportColumnKind.WholeNumber when def.Key == "TaxYear":
                dv.WholeNumber.Between(2500, 2700);
                dv.ErrorMessage = "ปี พ.ศ. เช่น 2569";
                break;
            case ImportColumnKind.Code or ImportColumnKind.Text when def.ExactLength is int exact:
                dv.TextLength.EqualTo(exact);
                dv.ErrorMessage = $"ต้องมี {exact} ตัวอักษร";
                break;
            case ImportColumnKind.Code or ImportColumnKind.Text when def.MaxLength is int max:
                dv.TextLength.EqualOrLessThan(max);
                dv.ErrorMessage = $"ยาวได้ไม่เกิน {max} ตัวอักษร";
                break;
        }
    }

    private static void WriteOrgRows(IXLWorksheet ws, IReadOnlyList<ImportOrgRow> orgs)
    {
        var row = 2;
        foreach (var o in orgs)
        {
            ws.Cell(row, 1).Value = o.Code;
            ws.Cell(row, 2).Value = o.Name;
            ws.Cell(row, 3).Value = o.ParentCode ?? "";
            ws.Cell(row, 4).Value = o.ApproverEmpNo ?? "";
            row++;
        }
    }

    private static void WriteGuide(IXLWorksheet ws, EmployeeImportLookups lookups)
    {
        ws.Column(1).Width = 24;
        ws.Column(2).Width = 10;
        ws.Column(3).Width = 22;
        ws.Column(4).Width = 60;

        var r = 1;
        ws.Cell(r, 1).Value = "Advance.Payroll — แบบฟอร์มนำเข้าข้อมูลพนักงาน";
        ws.Range(r, 1, r, 4).Merge().Style.Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(Brand);
        r++;
        ws.Cell(r, 1).Value = $"บริษัท: {lookups.CompanyName} ({lookups.CompanyCode})";
        ws.Cell(r, 4).Value = EmployeeImportSchema.VersionCellNote;
        ws.Cell(r, 4).Style.Font.SetFontColor(XLColor.Gray);
        r += 2;

        string[] steps =
        [
            "1. กรอกชีต \"หน่วยงาน\" ก่อน — หน่วยงานที่มีอยู่ในระบบแล้วเติมมาให้ แก้หรือเพิ่มต่อท้ายได้",
            "2. กรอกชีต \"พนักงาน\" หนึ่งแถวต่อหนึ่งคน — หัวคอลัมน์สีส้มต้องกรอก สีฟ้าไม่บังคับ",
            "3. ห้ามเปลี่ยนชื่อชีตหรือหัวคอลัมน์ ห้ามแทรกคอลัมน์ — ลบแถวได้ เพิ่มแถวได้",
            "4. อัปโหลดที่เมนู นำเข้าพนักงาน — ระบบจะตรวจทุกแถวและแสดงจุดที่ผิดก่อน ยังไม่บันทึกอะไรจนกว่าจะกดยืนยัน",
            "5. ถ้ามีแถวผิดแม้แต่แถวเดียว ระบบจะไม่นำเข้าเลย แก้ไฟล์แล้วอัปโหลดใหม่ได้ไม่จำกัดครั้ง",
            "6. นำเข้าซ้ำได้ — รหัสพนักงานที่มีอยู่แล้วจะถูกอัปเดต ไม่สร้างคนซ้ำ",
        ];
        foreach (var s in steps)
        {
            ws.Cell(r, 1).Value = s;
            ws.Range(r, 1, r, 4).Merge();
            r++;
        }
        r++;

        foreach (var sheet in EmployeeImportSchema.DataSheets)
        {
            ws.Cell(r, 1).Value = $"ชีต \"{sheet.Name}\"";
            ws.Range(r, 1, r, 4).Merge().Style.Font.SetBold().Font.SetFontSize(12);
            r++;
            ws.Cell(r, 1).Value = sheet.Purpose;
            ws.Range(r, 1, r, 4).Merge().Style.Font.SetItalic();
            r++;
            ws.Cell(r, 1).Value = "คอลัมน์";
            ws.Cell(r, 2).Value = "บังคับ";
            ws.Cell(r, 3).Value = "ตัวอย่าง";
            ws.Cell(r, 4).Value = "คำอธิบาย";
            ws.Range(r, 1, r, 4).Style.Font.SetBold().Fill.SetBackgroundColor(HeaderOptional);
            r++;
            foreach (var c in sheet.Columns)
            {
                ws.Cell(r, 1).Value = c.Header.TrimEnd('*');
                ws.Cell(r, 2).Value = c.Required ? "✓" : "";
                ws.Cell(r, 3).SetValue(c.Example);
                ws.Cell(r, 4).Value = c.Note;
                if (c.Required) ws.Cell(r, 1).Style.Fill.BackgroundColor = HeaderRequired;
                r++;
            }
            r++;
        }
        ws.Column(4).Style.Alignment.WrapText = true;
    }
}
