using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // ผังองค์กรต่อกันด้วย id (CEO, 21 ก.ย. 2569: "ปกติก็ควรใช้ id … ยังไม่มี production จริง เปลี่ยนเป็น id เลย")
    // เดิม com_organization มีทั้ง parent_code และ parentID แต่ไม่มีอะไรบังคับให้ตรงกัน — parentID มีค่า 11 จาก 189 แถว (hrm, 21 ก.ย. 2569) — อาการเดียวกับ Advance.Payroll ที่ทำไปก่อนใน 20260921120000 ของ repo นั้น
    // ทุกหน้าจออ่าน parent_code จึงถือ parent_code เป็นข้อมูลจริงของวันนี้ แล้วเติม parentID ตามนั้น (รวมแถวที่สองค่าขัดกัน)
    //   · จับคู่ภายในบริษัทเดียวกัน (companyid ว่างจับกับว่าง) รหัสซ้ำในบริษัทเลือก id น้อยสุด
    //   · แถวที่ companyid ว่างแต่แม่มีบริษัท → รับบริษัทของแม่
    //   · FK ชี้ตัวเอง + index: ฐานข้อมูลไม่ยอมให้ชี้ไปหาแม่ที่ไม่มี และลบแม่ที่ยังมีลูกไม่ได้
    // parent_code ยังอยู่ เป็นสำเนาให้คนอ่าน/นำเข้า — HRMContext.OrgParent.cs ทำให้มันตาม parentID เสมอ
    public partial class OrgParentById : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE c SET parentID = (
        SELECT MIN(p.id) FROM com_organization p
        WHERE p.code = c.parent_code AND p.id <> c.id
          AND (p.companyid = c.companyid OR (p.companyid IS NULL AND c.companyid IS NULL)))
FROM com_organization c
WHERE ISNULL(c.parent_code, '') <> '';

-- หาแม่ในบริษัทเดียวกันไม่เจอ: ลองจากรหัสอย่างเดียว (หน่วยงานที่ถูกสร้างโดยลืมใส่บริษัท)
UPDATE c SET parentID = (SELECT MIN(p.id) FROM com_organization p WHERE p.code = c.parent_code AND p.id <> c.id)
FROM com_organization c
WHERE ISNULL(c.parent_code, '') <> '' AND c.parentID IS NULL;

-- ไม่มีรหัสแม่ = ไม่มีแม่ (ล้าง parentID เก่าที่ค้าง) · มีแต่ parentID = เติมรหัสให้
UPDATE c SET parent_code = p.code FROM com_organization c JOIN com_organization p ON p.id = c.parentID WHERE ISNULL(c.parent_code, '') = '';
UPDATE com_organization SET parentID = NULL WHERE parentID IS NOT NULL AND (parentID = id OR NOT EXISTS (SELECT 1 FROM com_organization p WHERE p.id = com_organization.parentID));

UPDATE c SET companyid = p.companyid FROM com_organization c JOIN com_organization p ON p.id = c.parentID WHERE c.companyid IS NULL AND p.companyid IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_com_organization_parentID' AND object_id = OBJECT_ID('com_organization'))
    CREATE INDEX IX_com_organization_parentID ON com_organization (parentID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_com_organization_parent')
    ALTER TABLE com_organization ADD CONSTRAINT FK_com_organization_parent FOREIGN KEY (parentID) REFERENCES com_organization (id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_com_organization_parent') ALTER TABLE com_organization DROP CONSTRAINT FK_com_organization_parent;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_com_organization_parentID' AND object_id = OBJECT_ID('com_organization')) DROP INDEX IX_com_organization_parentID ON com_organization;");
        }
    }
}
