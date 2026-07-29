namespace HRM.Services.Pay;

using HRM.Models;
using System.Globalization;
using System.Text.RegularExpressions;

// Payslip PDF passwords are derived on demand from employee data plus a
// company-configurable template (Pay_PayslipSettings.PasswordTemplate) —
// never stored anywhere. Default is the employee's birthdate as DDMMYYYY,
// the convention most Thai payroll systems already use; HR can switch to a
// citizen-ID-based or composite format per company via the admin page.
public static class PayslipPasswordService
{
    public record PasswordResult(bool Success, string? Password, string? ErrorReason);

    public static readonly string[] SupportedTokens =
    {
        "{BirthDateDDMMYYYY}", "{BirthDateDDMMYY}", "{IdCardLast4}", "{EmpNo}",
    };

    private static readonly Regex TokenPattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

    public static PasswordResult Resolve(Hremployee employee, string template)
    {
        var missing = new List<string>();

        string ReplaceToken(Match m) => m.Groups[1].Value switch
        {
            "BirthDateDDMMYYYY" => FormatDate(employee.BirthDate, "ddMMyyyy", missing),
            "BirthDateDDMMYY" => FormatDate(employee.BirthDate, "ddMMyy", missing),
            "IdCardLast4" => LastDigits(employee.IdCard, 4, missing),
            "EmpNo" => employee.EmpNo,
            var unknown => throw new InvalidOperationException($"Unknown payslip password token: {{{unknown}}}"),
        };

        var password = TokenPattern.Replace(template, ReplaceToken);

        return missing.Count > 0
            ? new PasswordResult(false, null, $"พนักงาน {employee.EmpNo} ไม่มีข้อมูล {string.Join(", ", missing)} ในระบบ ไม่สามารถสร้างรหัสผ่านสลิปได้")
            : new PasswordResult(true, password, null);
    }

    private static string FormatDate(DateTime? value, string format, List<string> missing)
    {
        if (value is null)
        {
            missing.Add("วันเกิด");
            return "";
        }
        return value.Value.ToString(format, CultureInfo.InvariantCulture);
    }

    private static string LastDigits(string? value, int count, List<string> missing)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < count)
        {
            missing.Add("เลขบัตรประชาชน");
            return "";
        }
        return value[^count..];
    }
}
