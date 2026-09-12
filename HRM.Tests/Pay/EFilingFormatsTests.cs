using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

// ไฟล์ยื่นแบบอิเล็กทรอนิกส์: ภ.ง.ด.1 (RD Prep, คั่น |) และ สปส.1-10 (135 ตัวอักษรคงที่)
public class EFilingFormatsTests
{
    [Fact]
    public void Pnd1_line_has_twelve_pipe_fields_in_rd_prep_slot_order()
    {
        var line = EFilingFormats.Pnd1Line(3, "1234567890123", "นาย", "สมชาย", "ตั้งใจ", new DateOnly(2025, 1, 28), 30000m, 95.83m);
        var f = line.Split('|');
        Assert.Equal(EFilingFormats.Pnd1ColumnCount, f.Length);
        Assert.Equal("3", f[0]);                 // ลำดับ
        Assert.Equal("1234567890123", f[1]);     // เลขประจำตัวผู้เสียภาษี
        Assert.Equal("", f[2]);                  // เลขประจำตัวที่ 2 (ว่าง)
        Assert.Equal("นาย", f[3]);
        Assert.Equal("28012568", f[6]);          // ddMMyyyy พ.ศ.
        Assert.Equal("1", f[7]);                 // ประเภทเงินได้ 40(1)
        Assert.Equal("", f[8]);                  // อัตราภาษี (ว่าง)
        Assert.Equal("30000.00", f[9]);
        Assert.Equal("95.83", f[10]);
        Assert.Equal("1", f[11]);                // หัก ณ ที่จ่าย
    }

    [Fact]
    public void Pnd1_line_never_contains_a_stray_pipe_from_names()
    {
        var line = EFilingFormats.Pnd1Line(1, "1234567890123", "นาง", "สม|หญิง", "ขยัน\n", new DateOnly(2025, 6, 30), 1m, 0m);
        Assert.Equal(EFilingFormats.Pnd1ColumnCount, line.Split('|').Length);
    }

    [Fact]
    public void Pnd1kor_line_has_fifteen_fields_with_year_end_date_and_address()
    {
        var f = EFilingFormats.Pnd1KorLine(1, "1234567890123", "นาย", "ก", "ข", 2025, 360000m, 1140m, "99/1", "บางนา", "บางนา").Split('|');
        Assert.Equal(EFilingFormats.Pnd1KorColumnCount, f.Length);
        Assert.Equal("31122568", f[6]);          // วันที่จ่าย = สิ้นปีภาษี (พ.ศ.)
        Assert.Equal("360000.00", f[9]);
        Assert.Equal("1140.00", f[10]);
        Assert.Equal("99/1", f[12]);
        Assert.Equal("บางนา", f[14]);
    }

    [Fact]
    public void Sso110_header_and_detail_are_exactly_135_characters()
    {
        var h = EFilingFormats.Sso110Header("1234567890", "000000", new DateOnly(2025, 2, 14), new DateOnly(2025, 1, 1),
            "บริษัท ทดสอบ จำกัด", 5m, 9, 123456.78m, 12345.68m, 6172.84m, 6172.84m);
        var d = EFilingFormats.Sso110Detail("1234567890123", EFilingFormats.PrefixBySex("M").SsoCode, "สมชาย", "ตั้งใจ", 15000m, 750m);
        Assert.Equal(135, h.Length);
        Assert.Equal(135, d.Length);
        Assert.StartsWith("1" + "1234567890" + "000000" + "140268" + "0168", h);   // วันที่ชำระ 14/02/2568, งวด 01/2568
        Assert.Contains("0500", h);                                                  // อัตรา 5.00%
        Assert.Equal("000006172.84", h[^12..]);                                      // ส่วนนายจ้าง 12 ตัวท้าย
        Assert.Equal("2" + "1234567890123" + "003", d[..17]);
        Assert.Equal("00000015000.00", d.Substring(82, 14));                         // ค่าจ้าง 14 ตัว เริ่มหลังชื่อ-สกุล
    }

    [Fact]
    public void Fixed_pads_numbers_left_with_zero_and_text_right_with_space_and_truncates()
    {
        Assert.Equal("0000123", EFilingFormats.Fixed("123", 7, numeric: true));
        Assert.Equal("abc  ", EFilingFormats.Fixed("abc", 5));
        Assert.Equal("abcde", EFilingFormats.Fixed("abcdefgh", 5));
        Assert.Equal("67890", EFilingFormats.Fixed("1234567890", 5, numeric: true));
    }

    [Fact]
    public void Thai_dates_use_buddhist_year()
    {
        var d = new DateOnly(2025, 12, 5);
        Assert.Equal("05122568", EFilingFormats.ThaiDate(d));
        Assert.Equal("051268", EFilingFormats.ThaiDate(d, "ddMMyy"));
        Assert.Equal("1268", EFilingFormats.ThaiDate(d, "MMyy"));
    }

    [Fact]
    public void Tis620_encodes_thai_as_single_bytes()
    {
        var bytes = EFilingFormats.Tis620("นาย");
        Assert.Equal(3, bytes.Length);
    }
}
