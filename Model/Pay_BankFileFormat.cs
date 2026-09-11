using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// รูปแบบไฟล์โอนเงินเดือนต่อธนาคาร — เป็น config ไม่ใช่โค้ด (12 ก.ย. 2569, CEO: "ทำให้ครบเลย")
// ธนาคารแต่ละแห่ง (และแต่ละบริการ เช่น KBank SMART Payroll, SCB Payroll, BBL BIZ) มี spec ไฟล์ของตัวเองซึ่งได้จากธนาคาร
// ตอนเปิดบริการ — ฝ่ายบัญชีจึงตั้ง "แม่แบบบรรทัด" ตาม spec นั้นได้เองที่ /pay/admin/bank-file-formats โดยไม่ต้องรอโปรแกรมเมอร์
// ไม่มีแถวที่เปิดใช้ = ใช้ CSV กลางแบบเดิม (BankFileTemplate.GenericCsv)
[Table("Pay_BankFileFormat")]
public class Pay_BankFileFormat
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    // รหัสที่ลงในไฟล์ธนาคารที่สร้าง (Pay_BankFileExportBatch.BankFormatCode) เช่น KBANK_SMART, SCB_PAYROLL
    [Required, StringLength(30)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    // บัญชีบริษัทที่โอนออก — ใส่ในหัวไฟล์ผ่าน {CompanyAccountNo} {CompanyBankCode} {CompanyBranchCode}
    [StringLength(50)]
    public string? CompanyBankCode { get; set; }
    [StringLength(50)]
    public string? CompanyBranchCode { get; set; }
    [StringLength(50)]
    public string? CompanyAccountNo { get; set; }

    // แม่แบบ (ดู BankFileTemplate สำหรับชื่อช่องและรูปแบบ {ช่อง:format,width,pad}) — ว่าง = ไม่มีบรรทัดนั้น
    [StringLength(2000)]
    public string? HeaderTemplate { get; set; }
    [Required, StringLength(2000)]
    public string LineTemplate { get; set; } = null!;
    [StringLength(2000)]
    public string? TrailerTemplate { get; set; }

    // UTF8 | UTF8BOM | TIS620
    [Required, StringLength(20)]
    public string Encoding { get; set; } = "UTF8BOM";

    // CRLF | LF | NONE (ไฟล์ความยาวคงที่บางธนาคารไม่มีตัวขึ้นบรรทัด)
    [Required, StringLength(10)]
    public string LineEnding { get; set; } = "CRLF";

    [StringLength(100)]
    public string? FileNamePattern { get; set; }   // เช่น PAYROLL_{Period}_{PayDate:yyyyMMdd}.txt

    // แม่แบบที่ใช้จริงของบริษัท — หนึ่งบริษัทเปิดได้หนึ่งแบบ (ธนาคารหลักที่จ่ายเงินเดือน)
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    [StringLength(1000)]
    public string? Note { get; set; }

    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public long? ModifiedByUserId { get; set; }
}
