using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advance.Payroll.Domain;

// รหัสรายการที่ไฟล์บัญชี (GL) ต้องรู้ว่าลงบัญชีไหน — นอกเหนือจาก Pay_PayItemType.GLAccountCode ของรายการรับ-จ่ายรายตัว
// (audit M10, 11 ก.ย. 2569): ฝั่งนายจ้างไม่ใช่รายการรับ-จ่ายของพนักงาน จึงต้องมีที่ตั้งบัญชีของตัวเอง
public static class GLMappingKeys
{
    public const string NetPayable = "NET_PAYABLE";                 // เงินเดือนค้างจ่าย (เครดิต)
    public const string EmployerSso = "EMPLOYER_SSO";               // ประกันสังคมส่วนนายจ้าง (เดบิตค่าใช้จ่าย / เครดิตเจ้าหนี้)
    public const string EmployerProvidentFund = "EMPLOYER_PF";      // เงินสมทบกองทุนสำรองเลี้ยงชีพ
    public const string EmployerInsurance = "EMPLOYER_INSURANCE";   // เบี้ยประกันกลุ่มส่วนบริษัท
    public const string EmployerWelfareFund = "EMPLOYER_WELFAREFUND"; // กองทุนสงเคราะห์ลูกจ้างส่วนนายจ้าง

    public static readonly (string Key, string Label)[] All =
    {
        (NetPayable, "เงินเดือนค้างจ่าย"),
        (EmployerSso, "ประกันสังคมส่วนนายจ้าง"),
        (EmployerProvidentFund, "เงินสมทบกองทุนสำรองเลี้ยงชีพ (นายจ้าง)"),
        (EmployerInsurance, "เบี้ยประกันกลุ่มส่วนบริษัท"),
        (EmployerWelfareFund, "กองทุนสงเคราะห์ลูกจ้างส่วนนายจ้าง"),
    };
}

[Table("Pay_GLAccountMapping")]
public class Pay_GLAccountMapping
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(50)]
    public string MappingKey { get; set; } = null!;

    [Required, StringLength(200)]
    public string DisplayName { get; set; } = null!;

    // ฝั่งค่าใช้จ่าย (เดบิต) — ไม่ใช้กับ NET_PAYABLE
    [StringLength(50)]
    public string? DebitAccountCode { get; set; }

    // ฝั่งเจ้าหนี้/ค้างจ่าย (เครดิต)
    [StringLength(50)]
    public string? CreditAccountCode { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public long? ModifiedByUserId { get; set; }
}
