using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // หัวหน้า/ผู้อนุมัติของหน่วยงานผูกด้วย id ของพนักงาน (กติกา id/code — skill advance-data-discipline Part A0, CEO 21 ก.ย. 2569)
    // เดิม com_organization.approver_empid / boss_emp_id เก็บ EMP_NO แล้วทุกที่จับคู่ด้วยข้อความ ไม่มี FK
    //
    // ตรวจ hrm 22 ก.ย. 2569: ผู้อนุมัติ 161 หน่วยงาน จับคู่พนักงานในบริษัทได้ 157 · อีก 4 (CEO/sec/HR/TESTWF1 — EMP_NO 001-003)
    // ชี้ไปหา EMP_NO ที่ไม่มีทั้งใน HREMPLOYEE (ข้อมูลตั้งต้นจากยุค JSP ก่อนมีพนักงานจริง) — workflow หาคนไม่เจออยู่แล้วจึงไต่ข้าม
    // หน่วยงานนั้นไป · ล้างการอ้างอิงที่ลอยอยู่ทิ้ง = พฤติกรรมเดิมทุกประการ แต่ข้อมูลพูดความจริง (พิมพ์รายชื่อไว้)
    //   · จับคู่ในบริษัทเดียวกันก่อน (com_company.code = HREMPLOYEE.companyid) หน่วยงานที่ไม่มีบริษัทจับจาก EMP_NO อย่างเดียวเมื่อไม่กำกวม
    //   · หน่วยงาน 16 แถวที่ไม่มี companyid (กลุ่ม ADHOLD/ADDIGITAL/ADMOVIE) ได้บริษัทจาก comp_code ก่อน
    public partial class OrgHeadsById : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('com_organization', 'approver_hremployee_id') IS NULL ALTER TABLE com_organization ADD approver_hremployee_id bigint NULL;
IF COL_LENGTH('com_organization', 'boss_hremployee_id') IS NULL ALTER TABLE com_organization ADD boss_hremployee_id bigint NULL;");

            // คอลัมน์ใหม่เพิ่งถูกเพิ่มในคำสั่งก่อนหน้า — SQL Server compile ทั้ง batch ก่อนรัน (script จาก `dotnet ef migrations script`
            // รวมทุกอย่างเป็น batch เดียว) จึงห่อส่วนที่อ้างคอลัมน์ใหม่ไว้ใน EXEC ให้ compile ตอนรันจริง
            migrationBuilder.Sql("EXEC(N'" + HeadsBackfill.Replace("'", "''") + "')");
        }

        private const string HeadsBackfill = @"
-- หน่วยงานที่ไม่มีบริษัท: รับจาก comp_code
UPDATE o SET companyid = c.id FROM com_organization o JOIN com_company c ON c.code = o.comp_code WHERE o.companyid IS NULL;

-- ผู้อนุมัติ / หัวหน้า: EMP_NO -> id ของพนักงานในบริษัทเดียวกัน
UPDATE o SET approver_hremployee_id = e.id
FROM com_organization o JOIN com_company c ON c.id = o.companyid JOIN HREMPLOYEE e ON e.EMP_NO = o.approver_empid AND e.companyid = c.code
WHERE ISNULL(o.approver_empid, '') <> '';
UPDATE o SET approver_hremployee_id = (SELECT MIN(e.id) FROM HREMPLOYEE e WHERE e.EMP_NO = o.approver_empid)
FROM com_organization o
WHERE o.approver_hremployee_id IS NULL AND ISNULL(o.approver_empid, '') <> ''
  AND (SELECT COUNT(1) FROM HREMPLOYEE e WHERE e.EMP_NO = o.approver_empid) = 1;

UPDATE o SET boss_hremployee_id = e.id
FROM com_organization o JOIN com_company c ON c.id = o.companyid JOIN HREMPLOYEE e ON e.EMP_NO = o.boss_emp_id AND e.companyid = c.code
WHERE ISNULL(o.boss_emp_id, '') <> '';
UPDATE o SET boss_hremployee_id = (SELECT MIN(e.id) FROM HREMPLOYEE e WHERE e.EMP_NO = o.boss_emp_id)
FROM com_organization o
WHERE o.boss_hremployee_id IS NULL AND ISNULL(o.boss_emp_id, '') <> ''
  AND (SELECT COUNT(1) FROM HREMPLOYEE e WHERE e.EMP_NO = o.boss_emp_id) = 1;

-- อ้างอิงที่ลอยอยู่ (ไม่มีพนักงานคนนั้น) — พิมพ์ไว้แล้วล้าง
DECLARE @dangling nvarchar(max) = (
    SELECT STRING_AGG(CONCAT(code, '=', approver_empid), ', ') FROM com_organization
    WHERE ISNULL(approver_empid, '') <> '' AND approver_hremployee_id IS NULL);
IF @dangling IS NOT NULL PRINT CONCAT('approver_empid cleared (no such employee): ', LEFT(@dangling, 3500));
UPDATE com_organization SET approver_empid = NULL, approver_name = NULL, approver_userid = NULL
WHERE ISNULL(approver_empid, '') <> '' AND approver_hremployee_id IS NULL;
UPDATE com_organization SET boss_emp_id = NULL, boss_name = NULL
WHERE ISNULL(boss_emp_id, '') <> '' AND boss_hremployee_id IS NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_com_organization_approver_hremployee')
    ALTER TABLE com_organization ADD CONSTRAINT FK_com_organization_approver_hremployee FOREIGN KEY (approver_hremployee_id) REFERENCES HREMPLOYEE (id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_com_organization_boss_hremployee')
    ALTER TABLE com_organization ADD CONSTRAINT FK_com_organization_boss_hremployee FOREIGN KEY (boss_hremployee_id) REFERENCES HREMPLOYEE (id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_com_organization_approver_hremployee_id' AND object_id = OBJECT_ID('com_organization'))
    CREATE INDEX IX_com_organization_approver_hremployee_id ON com_organization (approver_hremployee_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_com_organization_boss_hremployee_id' AND object_id = OBJECT_ID('com_organization'))
    CREATE INDEX IX_com_organization_boss_hremployee_id ON com_organization (boss_hremployee_id);";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_com_organization_approver_hremployee') ALTER TABLE com_organization DROP CONSTRAINT FK_com_organization_approver_hremployee;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_com_organization_boss_hremployee') ALTER TABLE com_organization DROP CONSTRAINT FK_com_organization_boss_hremployee;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_com_organization_approver_hremployee_id' AND object_id = OBJECT_ID('com_organization')) DROP INDEX IX_com_organization_approver_hremployee_id ON com_organization;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_com_organization_boss_hremployee_id' AND object_id = OBJECT_ID('com_organization')) DROP INDEX IX_com_organization_boss_hremployee_id ON com_organization;
IF COL_LENGTH('com_organization', 'approver_hremployee_id') IS NOT NULL ALTER TABLE com_organization DROP COLUMN approver_hremployee_id;
IF COL_LENGTH('com_organization', 'boss_hremployee_id') IS NOT NULL ALTER TABLE com_organization DROP COLUMN boss_hremployee_id;
-- การอ้างอิงที่ลอยอยู่ซึ่งถูกล้าง ไม่ได้คืนค่า (ชี้ไปหาพนักงานที่ไม่มีอยู่จริง)");
        }
    }
}
