using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประวัติไฟล์ที่นำเข้าผ่านหน้า "คีย์เงินได้/เงินหักรายงวด" (CEO, 20 ก.ย. 2569: "อีกหน่อยเอาไฟล์นี้มาวนซ้ำ
// หรือดูไม่ออกแล้วทำซ้ำ ทำผิดไฟล์") — เก็บ SHA-256 ของไฟล์ + งวดที่นำเข้า เพื่อให้หน้าอัปโหลดบอกได้ว่า
// "ไฟล์นี้นำเข้างวดนี้แล้วเมื่อ … โดย …" (กันซ้ำ) หรือ "ไฟล์นี้เคยเข้างวดอื่น" (กันผิดไฟล์) ก่อนจะบันทึก
[Table("Pay_AdhocImportBatch")]
public class Pay_AdhocImportBatch
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    // งวด/รอบที่ผู้ใช้เลือกบนหน้าจอตอนนำเข้า
    [Required, StringLength(6)]
    public string TargetPeriod { get; set; } = null!;
    public PayrollRunType TargetRunType { get; set; }
    public int? TargetTermNo { get; set; }

    // งวดที่ตราไว้ในไฟล์ (จากเทมเพลตของระบบ) — null = ไฟล์ของลูกค้าเองที่ไม่มีตรา
    [StringLength(6)]
    public string? FilePeriodStamp { get; set; }

    [StringLength(260)]
    public string? FileName { get; set; }

    [Required, StringLength(64)]
    public string FileSha256 { get; set; } = null!;

    public int RowCount { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }

    public long ImportedByUserId { get; set; }
    [StringLength(200)]
    public string? ImportedByName { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.Now;
}
