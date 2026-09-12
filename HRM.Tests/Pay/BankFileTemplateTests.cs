using HRM.Services.Pay;
using Xunit;

namespace HRM.Tests.Pay;

// แม่แบบไฟล์ธนาคารแบบ config: {ช่อง:รูปแบบ,ความกว้าง,ตัวเติม}
public class BankFileTemplateTests
{
    private static readonly Dictionary<string, object?> Line = new()
    {
        ["Seq"] = 7, ["EmpNo"] = "E001", ["Name"] = "สมชาย ตั้งใจ", ["BankCode"] = "004", ["BranchCode"] = "0001",
        ["NameCsv"] = "สมชาย ตั้งใจ", ["AccountNo"] = "1234567890", ["Amount"] = 27645.83m, ["AmountCents"] = BankFileTemplate.Cents(27645.83m),
        ["PayDate"] = new DateOnly(2025, 1, 28), ["PayDateBE"] = BankFileTemplate.BuddhistEra(new DateOnly(2025, 1, 28)),
    };

    [Fact]
    public void Generic_csv_line_matches_the_legacy_layout()
    {
        var s = BankFileTemplate.Render(BankFileTemplate.GenericCsvLine, Line);
        Assert.Equal("E001,\"สมชาย ตั้งใจ\",004,0001,1234567890,27645.83", s);
    }

    [Fact]
    public void Fixed_width_fields_pad_and_align()
    {
        Assert.Equal("        1234567890", BankFileTemplate.Render("{AccountNo:,18}", Line));
        Assert.Equal("1234567890        ", BankFileTemplate.Render("{AccountNo:,-18}", Line));
        Assert.Equal("0000002764583", BankFileTemplate.Render("{AmountCents:,13,0}", Line));
        Assert.Equal("0007", BankFileTemplate.Render("{Seq:,4,0}", Line));
    }

    [Fact]
    public void Dates_render_in_ce_and_be()
    {
        Assert.Equal("28012025", BankFileTemplate.Render("{PayDate:ddMMyyyy}", Line));
        Assert.Equal("28012568", BankFileTemplate.Render("{PayDateBE:ddMMyyyy}", Line));
    }

    [Fact]
    public void Unknown_field_is_an_error_not_silence()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BankFileTemplate.Render("{Nope}", Line));
        Assert.Contains("Nope", ex.Message);
    }

    [Fact]
    public void Build_joins_header_lines_and_trailer_with_the_chosen_line_ending()
    {
        var file = new Dictionary<string, object?> { ["RecordCount"] = 2, ["TotalAmount"] = 100m };
        var l1 = new Dictionary<string, object?> { ["EmpNo"] = "A", ["Amount"] = 40m };
        var l2 = new Dictionary<string, object?> { ["EmpNo"] = "B", ["Amount"] = 60m };
        var text = BankFileTemplate.Build("H{RecordCount}", "{EmpNo}:{Amount}", "T{TotalAmount}", "LF", file, new[] { l1, l2 });
        Assert.Equal("H2\nA:40.00\nB:60.00\nT100.00\n", text);
    }

    [Fact]
    public void Encodings_utf8bom_and_tis620()
    {
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, BankFileTemplate.Encode("", "UTF8BOM"));
        Assert.Equal(3, BankFileTemplate.Encode("นาย", "TIS620").Length);
        Assert.Equal(9, BankFileTemplate.Encode("นาย", "UTF8").Length);
    }
}
