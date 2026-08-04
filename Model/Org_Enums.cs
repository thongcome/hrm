namespace HRM.Models;

public enum OrgOrganizationChangeType
{
    NewOrganization = 1, // สร้างหน่วยงานใหม่
    ChangeParent = 2,    // ย้ายสังกัด
    ChangeApprover = 3,  // เปลี่ยนหัวหน้า
}
