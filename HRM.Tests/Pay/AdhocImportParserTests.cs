using ClosedXML.Excel;
using HRM.Services.Pay.AdhocImport;
using Xunit;

namespace HRM.Tests.Pay;

// ไฟล์เดียวใช้ได้ทุกประเภทรายการ: คอลัมน์แรก EMP_NO คอลัมน์ถัดไปคือรหัสประเภท (หรือชื่อไทย)
// งวด/รอบไม่ได้อยู่ในไฟล์ (เลือกบนหน้าจอ) — ที่นี่เทสเฉพาะการอ่านไฟล์
public class AdhocImportParserTests
{
    private static readonly AdhocImportParser.Lookups Lookups = new(
        new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["COMMISSION"] = (10, "ค่าคอมมิชชั่น"),
            ["ALLOWANCE"] = (3, "เบี้ยเลี้ยง/เงินเพิ่มประจำ"),
            ["ABSENT"] = (11, "หักขาดงาน"),
        },
        new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            ["E001"] = 1, ["E002"] = 2, ["E003"] = 3,
        });

    private static MemoryStream Sheet(params object?[][] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("รายการ");
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
            {
                var value = rows[r][c];
                if (value is null) continue;
                if (value is decimal d) ws.Cell(r + 1, c + 1).Value = d;
                else if (value is int i) ws.Cell(r + 1, c + 1).Value = i;
                else ws.Cell(r + 1, c + 1).Value = value.ToString();
            }
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void One_file_carries_every_item_type_as_its_own_column()
    {
        using var file = Sheet(
            ["EMP_NO", "COMMISSION", "ALLOWANCE"],
            ["E001", 5000m, 1000m],
            ["E002", 2500m, null]);

        var result = AdhocImportParser.Parse(file, Lookups);

        Assert.False(result.HasErrors);
        Assert.Equal(3, result.Lines.Count);
        Assert.Equal(2, result.EmployeeCount);
        Assert.Equal(8500m, result.Total);
        Assert.Contains(result.Lines, l => l.EmpNo == "E001" && l.ItemCode == "COMMISSION" && l.Amount == 5000m);
        Assert.DoesNotContain(result.Lines, l => l.EmpNo == "E002" && l.ItemCode == "ALLOWANCE");   // ช่องว่าง = ไม่มีรายการ
    }

    [Fact]
    public void Thai_column_names_and_formatted_numbers_are_understood()
    {
        using var file = Sheet(
            ["รหัสพนักงาน", "ค่าคอมมิชชั่น"],
            ["E001", "12,500.50"]);

        var result = AdhocImportParser.Parse(file, Lookups);

        Assert.False(result.HasErrors);
        Assert.Equal(12500.50m, Assert.Single(result.Lines).Amount);
    }

    [Fact]
    public void Unknown_employee_bad_amount_negative_and_duplicate_rows_are_reported_with_row_and_column()
    {
        using var file = Sheet(
            ["EMP_NO", "COMMISSION"],
            ["E999", 100m],          // ไม่มีพนักงานคนนี้
            ["E001", "abc"],         // ไม่ใช่จำนวนเงิน
            ["E002", -50m],          // ติดลบ
            ["E003", 700m],
            ["E003", 800m]);         // ซ้ำ

        var result = AdhocImportParser.Parse(file, Lookups);

        Assert.Equal(4, result.Issues.Count);
        Assert.Contains(result.Issues, i => i.Row == 2 && i.Message.Contains("E999"));
        Assert.Contains(result.Issues, i => i.Row == 3 && i.Column == "COMMISSION" && i.Message.Contains("ไม่ใช่จำนวนเงิน"));
        Assert.Contains(result.Issues, i => i.Row == 4 && i.Message.Contains("ติดลบ"));
        Assert.Contains(result.Issues, i => i.Row == 6 && i.Message.Contains("ซ้ำ"));
        Assert.Equal(700m, Assert.Single(result.Lines).Amount);   // แถวที่ดีต้องยังใช้ได้
    }

    [Fact]
    public void Unknown_column_is_listed_for_the_user_to_map_then_parsed_after_mapping()
    {
        using var file = Sheet(
            ["EMP_NO", "ยอดคอมเดือนนี้"],
            ["E001", 3000m]);

        var unmapped = AdhocImportParser.Parse(file, Lookups);
        Assert.True(unmapped.HasErrors);
        Assert.Contains(unmapped.Columns, c => !c.Recognised && c.Header == "ยอดคอมเดือนนี้");

        file.Position = 0;
        var mapped = AdhocImportParser.Parse(file, Lookups, new Dictionary<int, string> { [2] = "COMMISSION" });
        Assert.False(mapped.HasErrors);
        Assert.Equal(3000m, Assert.Single(mapped.Lines).Amount);
    }

    [Fact]
    public void Row_level_date_detail_and_reference_columns_ride_along_with_every_item_in_that_row()
    {
        using var file = Sheet(
            ["EMP_NO", "ชื่อ-สกุล (ไม่ต้องกรอก)", "COMMISSION", "ALLOWANCE", "วันที่สรุป", "รายละเอียด", "เลขที่อ้างอิง"],
            ["E001", "สมชาย", 5000m, 300m, "31/08/2569", "ยอดขาย ส.ค. 1.2 ล้าน × 3%", "SR-2569-08-07"],
            ["E002", "สมหญิง", 2500m, null, "2026-08-31", null, null],
            ["E003", "สมศรี", 100m, null, "ไม่ใช่วันที่", null, null]);

        var result = AdhocImportParser.Parse(file, Lookups);

        // ชื่อ-สกุล และสามคอลัมน์ประกอบเป็นคอลัมน์ที่ระบบรู้จัก — ไม่ถูกขอให้จับคู่
        Assert.DoesNotContain(result.Columns, c => !c.Recognised);

        var e1 = result.Lines.Where(l => l.EmpNo == "E001").ToList();
        Assert.Equal(2, e1.Count);
        Assert.All(e1, l =>
        {
            Assert.Equal(new DateTime(2026, 8, 31), l.ItemDate);      // พ.ศ. 2569 → ค.ศ. 2026
            Assert.Equal("ยอดขาย ส.ค. 1.2 ล้าน × 3%", l.Remark);
            Assert.Equal("SR-2569-08-07", l.ReferenceNo);
        });
        var e2 = Assert.Single(result.Lines.Where(l => l.EmpNo == "E002"));
        Assert.Equal(new DateTime(2026, 8, 31), e2.ItemDate);
        Assert.Null(e2.Remark);

        // วันที่อ่านไม่ออก = ปัญหาที่แถวนั้น ไม่เดาให้ และแถวนั้นไม่ถูกบันทึก
        Assert.Contains(result.Issues, i => i.Row == 4 && i.Column == "วันที่สรุป");
        Assert.DoesNotContain(result.Lines, l => l.EmpNo == "E003");
    }

    [Fact]
    public void Period_stamp_from_the_template_is_read_back_and_a_customer_file_has_none()
    {
        // ไฟล์จากเทมเพลตของระบบ: ชีตข้อมูลอยู่หน้า ชีต "ข้อมูลไฟล์" ตรางวดไว้ท้ายสุด — parser ต้องไม่หลงอ่านชีตตรานั้นเป็นข้อมูล
        using var wb = new XLWorkbook();
        var data = wb.Worksheets.Add("รายการ งวด 202609");
        data.Cell(1, 1).Value = "EMP_NO"; data.Cell(1, 2).Value = "COMMISSION";
        data.Cell(2, 1).Value = "E001"; data.Cell(2, 2).Value = 900;
        wb.Worksheets.Add(AdhocImportParser.GuideSheetName).Cell(1, 1).Value = "วิธีใช้";
        var stamp = wb.Worksheets.Add(AdhocImportParser.StampSheetName);
        stamp.Cell(1, 2).Value = "202609"; stamp.Cell(2, 2).Value = "Bonus"; stamp.Cell(3, 2).Value = "2";
        stamp.Cell(4, 2).Value = "20/09/2569 10:00"; stamp.Cell(5, 2).Value = "advadmin";
        using var stamped = new MemoryStream(); wb.SaveAs(stamped); stamped.Position = 0;

        var result = AdhocImportParser.Parse(stamped, Lookups);
        Assert.False(result.HasErrors);
        Assert.Equal(900m, Assert.Single(result.Lines).Amount);
        Assert.NotNull(result.Stamp);
        Assert.Equal(("202609", "Bonus", 2, "advadmin"), (result.Stamp!.Period, result.Stamp.RunType, result.Stamp.TermNo, result.Stamp.GeneratedBy));

        // ชีตตราถูกลบ แต่ Subject ของไฟล์ยังอยู่ → ยังรู้งวด
        using var wb2 = new XLWorkbook();
        var d2 = wb2.Worksheets.Add("Sheet1");
        d2.Cell(1, 1).Value = "EMP_NO"; d2.Cell(1, 2).Value = "COMMISSION"; d2.Cell(2, 1).Value = "E001"; d2.Cell(2, 2).Value = 1;
        wb2.Properties.Subject = AdhocImportParser.WriteStampSubject("202610", "Regular", null);
        using var subjectOnly = new MemoryStream(); wb2.SaveAs(subjectOnly); subjectOnly.Position = 0;
        var s2 = AdhocImportParser.Parse(subjectOnly, Lookups).Stamp;
        Assert.Equal(("202610", "Regular", (int?)null), (s2!.Period, s2.RunType, s2.TermNo));

        // ไฟล์ของลูกค้าเอง: ไม่มีตรา = null (หน้าจอจะบังคับให้ยืนยันงวดเอง)
        using var plain = Sheet(["EMP_NO", "COMMISSION"], ["E001", 1m]);
        Assert.Null(AdhocImportParser.Parse(plain, Lookups).Stamp);
    }

    [Fact]
    public void Template_gl_account_row_under_the_header_is_skipped_not_read_as_an_employee()
    {
        using var file = Sheet(
            ["EMP_NO", "ชื่อ-สกุล (ไม่ต้องกรอก)", "COMMISSION", "ABSENT"],
            [AdhocImportParser.AccountRowLabel, "(แถวนี้ระบบไม่อ่าน)", "5035-COMMISSION", null],
            ["E001", "สมชาย", 4000m, 200m]);

        var result = AdhocImportParser.Parse(file, Lookups);

        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Lines.Count);
        Assert.All(result.Lines, l => Assert.Equal("E001", l.EmpNo));
        Assert.All(result.Lines, l => Assert.Equal(3, l.Row));
    }

    [Fact]
    public void A_file_without_the_employee_column_is_refused_rather_than_guessed()
    {
        using var file = Sheet(
            ["ชื่อ", "COMMISSION"],
            ["สมชาย", 100m]);

        var result = AdhocImportParser.Parse(file, Lookups);

        Assert.Contains(result.Issues, i => i.Message.Contains("EMP_NO"));
        Assert.Empty(result.Lines);
    }
}
