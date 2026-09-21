using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// เวลาเข้า-ออกงานของสาขา/หน่วยงาน (CEO, 21 ก.ย. 2569 — PST มี 2 สาขาเวลาไม่เหมือนกัน: "แยกเวลาเข้าระหว่างสาขาได้ก็พอ")
// ตั้งที่หน่วยงานระดับไหนก็ได้ หน่วยงานลูกใช้ตามแม่จนกว่าจะตั้งของตัวเอง · ไม่มีแถว = ใช้เวลาของบริษัท
// (Att_CompanySetting.DefaultWorkStart/End) · พนักงานที่ถูกมอบกะรายวัน (Att_ShiftAssignment) ใช้กะนั้นก่อนเสมอ
[Table("Att_OrgWorkTime")]
public class Att_OrgWorkTime
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    public long OrganizationId { get; set; }   // com_organization.id

    public TimeOnly WorkStart { get; set; }
    public TimeOnly WorkEnd { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public long? ModifiedByUserId { get; set; }
}
