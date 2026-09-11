using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ใครแก้ค่านี้ได้ — ค่าเริ่มต้น "ยังไม่ตัดสิน" (CEO, 11 ก.ย. 2569: รวมรายการไว้ในหน้า Super Master ก่อน
// แล้วค่อยตัดสินใจว่าใครจะแก้ได้บ้าง) การบังคับสิทธิ์จริงจะอ่านจากคอลัมน์นี้เมื่อตัดสินแล้ว
public enum ProtectedSettingEditor
{
    Undecided = 0,
    Customer = 1,      // ผู้ดูแลระบบของลูกค้าแก้เองได้
    VendorOnly = 2,    // เจ้าของซอฟต์แวร์ (Advance Digital) เท่านั้น
}

// ทะเบียน "ค่าตั้งต้นของระบบที่ต้องควบคุม" — เป็น config ไม่ใช่ลิสต์ในโค้ด (CEO: "ทำเป็น config ไว้ แล้วไป look up
// ไม่ต้อง hard code เหมือนพวกจัดการงวด") หนึ่งแถว = หนึ่งค่า (ตาราง/ฟิลด์) พร้อมเหตุผลที่ต้องควบคุม หน้าที่ใช้แก้
// และคำตัดสินว่าใครแก้ได้ เพิ่ม/ลดรายการได้จากหน้า /admin/super-master
[Table("Sys_ProtectedSetting")]
public class Sys_ProtectedSetting
{
    [Key]
    public long Id { get; set; }

    // รหัสถาวรใช้ look up จากโค้ด เช่น PAY.SCHEDULE.PERIODS_PER_MONTH
    [Required, StringLength(100)]
    public string SettingKey { get; set; } = null!;

    [Required, StringLength(50)]
    public string Area { get; set; } = null!;          // Payroll / Workflow / System …

    [Required, StringLength(100)]
    public string TableName { get; set; } = null!;

    [Required, StringLength(200)]
    public string FieldName { get; set; } = null!;     // "*" = ทั้งตาราง

    [Required, StringLength(200)]
    public string DisplayName { get; set; } = null!;

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(200)]
    public string? PageRoute { get; set; }             // หน้าที่ใช้แก้ค่านี้

    public ProtectedSettingEditor EditableBy { get; set; } = ProtectedSettingEditor.Undecided;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public long? ModifiedByUserId { get; set; }
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
}
