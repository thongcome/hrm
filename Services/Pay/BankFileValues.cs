namespace HRM.Services.Pay;

using HRM.Models;

// ค่าที่แม่แบบไฟล์ธนาคาร (BankFileTemplate) มองเห็น — ที่เดียวที่กำหนดชื่อช่อง ทั้งตัวสร้างไฟล์จริงและตัวอย่างในหน้า admin ใช้ร่วมกัน
public static class BankFileValues
{
    public static IReadOnlyDictionary<string, object?> ForFile(string companyName, Pay_BankFileFormat? format, DateOnly payDate, string period, string batchNo, int recordCount, decimal totalAmount)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new Dictionary<string, object?>
        {
            ["CompanyName"] = companyName,
            ["CompanyAccountNo"] = format?.CompanyAccountNo ?? "",
            ["CompanyBankCode"] = format?.CompanyBankCode ?? "",
            ["CompanyBranchCode"] = format?.CompanyBranchCode ?? "",
            ["PayDate"] = payDate,
            ["PayDateBE"] = BankFileTemplate.BuddhistEra(payDate),
            ["Period"] = period,
            ["RecordCount"] = recordCount,
            ["TotalAmount"] = totalAmount,
            ["TotalAmountCents"] = BankFileTemplate.Cents(totalAmount),
            ["BatchNo"] = batchNo,
            ["Today"] = today,
            ["TodayBE"] = BankFileTemplate.BuddhistEra(today),
        };
    }

    public static IReadOnlyDictionary<string, object?> ForLine(int seq, string? empNo, string? firstName, string? lastName, string? bankCode, string? branchCode, string? accountNo, decimal amount, string? idCard, string? email)
        => new Dictionary<string, object?>
        {
            ["Seq"] = seq,
            ["EmpNo"] = empNo ?? "",
            ["Name"] = $"{firstName} {lastName}".Trim(),
            ["NameCsv"] = $"{firstName} {lastName}".Trim().Replace("\"", "\"\""),   // สำหรับใส่ในเครื่องหมายคำพูดของ CSV
            ["FirstName"] = firstName ?? "",
            ["LastName"] = lastName ?? "",
            ["BankCode"] = bankCode ?? "",
            ["BranchCode"] = branchCode ?? "",
            ["AccountNo"] = accountNo ?? "",
            ["Amount"] = amount,
            ["AmountCents"] = BankFileTemplate.Cents(amount),
            ["IdCard"] = idCard ?? "",
            ["Email"] = email ?? "",
        };
}
