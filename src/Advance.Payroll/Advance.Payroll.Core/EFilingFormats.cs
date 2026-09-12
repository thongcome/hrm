namespace Advance.Payroll.Core;

using System.Globalization;
using System.Text;

// Split out of HRM Services/Pay/EFilingExportService.cs (that file's static `EFilingFormats`
// class was already a separate, DB-free formatting class nested in the same source file — this
// is a mechanical extraction, not a rewrite). The DB-touching half (EFilingExportService's
// BuildPnd1Async/BuildPnd1KorAsync/BuildSso110Async) stays in Advance.Payroll.Engine, which
// calls into this class for every line/field format.
public static class EFilingFormats
{
    static EFilingFormats() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static string SsoPrefixCodeMale { get; set; } = "003";
    public static string SsoPrefixCodeFemale { get; set; } = "004";

    public static (string Text, string SsoCode) PrefixBySex(string? sex)
        => string.Equals(sex, "F", StringComparison.OrdinalIgnoreCase) ? ("นางสาว", SsoPrefixCodeFemale) : ("นาย", SsoPrefixCodeMale);

    public static string Digits(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

    public const int Pnd1ColumnCount = 12;
    public static string Pnd1Line(int seq, string taxId, string prefix, string firstName, string lastName, DateOnly payDate, decimal income, decimal tax)
        => string.Join("|", seq, taxId, "", Clean(prefix), Clean(firstName), Clean(lastName), ThaiDate(payDate), "1", "", Money(income), Money(tax), "1");

    public const int Pnd1KorColumnCount = 15;
    public static string Pnd1KorLine(int seq, string taxId, string prefix, string firstName, string lastName, int taxYear, decimal income, decimal tax,
        string? addressNo = null, string? subDistrict = null, string? district = null)
        => string.Join("|", seq, taxId, "", Clean(prefix), Clean(firstName), Clean(lastName), ThaiDate(new DateOnly(taxYear, 12, 31)), "1", "", Money(income), Money(tax), "1",
            Clean(addressNo ?? ""), Clean(subDistrict ?? ""), Clean(district ?? ""));

    public static string Sso110Header(string? employerAccountNo, string? branchSeq, DateOnly payDate, DateOnly periodStart, string companyName,
        decimal ratePercent, int insuredCount, decimal totalWages, decimal totalContribution, decimal employeePart, decimal employerPart)
    {
        var s = "1"
            + Fixed(Digits(employerAccountNo), 10, numeric: true)
            + Fixed(Digits(string.IsNullOrWhiteSpace(branchSeq) ? "0" : branchSeq), 6, numeric: true)
            + ThaiDate(payDate, "ddMMyy")
            + ThaiDate(periodStart, "MMyy")
            + Fixed(companyName, 45)
            + Fixed(((int)Math.Round(ratePercent * 100m)).ToString(CultureInfo.InvariantCulture), 4, numeric: true)
            + Fixed(insuredCount.ToString(CultureInfo.InvariantCulture), 6, numeric: true)
            + Fixed(Money(totalWages), 15, numeric: true)
            + Fixed(Money(totalContribution), 14, numeric: true)
            + Fixed(Money(employeePart), 12, numeric: true)
            + Fixed(Money(employerPart), 12, numeric: true);
        return s;
    }

    public static string Sso110Detail(string idCard, string prefixCode, string firstName, string lastName, decimal wage, decimal employeeContribution)
        => "2" + Fixed(idCard, 13, numeric: true) + Fixed(prefixCode, 3, numeric: true) + Fixed(firstName, 30) + Fixed(lastName, 35)
           + Fixed(Money(wage), 14, numeric: true) + Fixed(Money(employeeContribution), 12, numeric: true) + new string(' ', 27);

    public static string Money(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);

    public static string ThaiDate(DateOnly d, string format = "ddMMyyyy")
    {
        var be = d.Year + 543;
        return format switch
        {
            "ddMMyy" => $"{d.Day:00}{d.Month:00}{be % 100:00}",
            "MMyy" => $"{d.Month:00}{be % 100:00}",
            _ => $"{d.Day:00}{d.Month:00}{be:0000}",
        };
    }

    public static string Fixed(string? value, int width, bool numeric = false)
    {
        var v = (value ?? "").Trim();
        if (v.Length > width) v = numeric ? v[^width..] : v[..width];
        return numeric ? v.PadLeft(width, '0') : v.PadRight(width, ' ');
    }

    private static string Clean(string s) => (s ?? "").Replace("|", " ").Replace("\r", " ").Replace("\n", " ").Trim();

    public static byte[] Utf8(string text) => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
    public static byte[] Tis620(string text) => Encoding.GetEncoding(874).GetBytes(text);
}
