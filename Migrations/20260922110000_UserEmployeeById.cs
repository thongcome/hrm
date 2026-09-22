using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // บัญชีผู้ใช้ผูกกับพนักงานด้วย id (กติกา id/code — skill advance-data-discipline Part A0, CEO 21 ก.ย. 2569)
    // เดิม sc_user.empid เก็บ EMP_NO แล้ว login/ESS/MSS/ชื่อผู้อนุมัติ/บทบาทอัตโนมัติ จับคู่กับ HREMPLOYEE ด้วยข้อความ ไม่มี FK
    //
    // ตรวจ 22 ก.ย. 2569: hrm ผู้ใช้ที่มี empid จับคู่พนักงานในบริษัทของบัญชีได้ครบ ไม่มีที่ลอยหรือกำกวม
    //   · จับคู่ในบริษัทของบัญชี (sc_user.company_id → com_company.code = HREMPLOYEE.companyid) ก่อน
    //     บัญชีที่ไม่มีบริษัทจับจาก EMP_NO อย่างเดียวเมื่อไม่กำกวม
    //   · empid ที่ไม่มีพนักงานคนนั้น: พิมพ์รายชื่อแล้วล้าง (ESS ของบัญชีนั้นเปิดไม่ได้อยู่แล้วเพราะหาพนักงานไม่เจอ)
    //   · sc_user_role.empid เป็นสำเนาของ empid ของผู้ใช้ — ทำให้ตรงกัน
    public partial class UserEmployeeById : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('sc_user', 'hremployee_id') IS NULL ALTER TABLE sc_user ADD hremployee_id bigint NULL;");

            // คอลัมน์ใหม่เพิ่งถูกเพิ่มในคำสั่งก่อนหน้า — ห่อไว้ใน EXEC ให้ compile ตอนรันจริง (ดู 20260922100000_OrgHeadsById)
            migrationBuilder.Sql("EXEC(N'" + Backfill.Replace("'", "''") + "')");
        }

        private const string Backfill = @"
UPDATE u SET hremployee_id = e.id
FROM sc_user u JOIN com_company c ON c.id = u.company_id JOIN HREMPLOYEE e ON e.EMP_NO = u.empid AND e.companyid = c.code
WHERE ISNULL(u.empid, '') <> '';
UPDATE u SET hremployee_id = (SELECT MIN(e.id) FROM HREMPLOYEE e WHERE e.EMP_NO = u.empid)
FROM sc_user u
WHERE u.hremployee_id IS NULL AND ISNULL(u.empid, '') <> ''
  AND (SELECT COUNT(1) FROM HREMPLOYEE e WHERE e.EMP_NO = u.empid) = 1;

DECLARE @dangling nvarchar(max) = (
    SELECT STRING_AGG(CONCAT(loginname, '=', empid), ', ') FROM sc_user
    WHERE ISNULL(empid, '') <> '' AND hremployee_id IS NULL);
IF @dangling IS NOT NULL PRINT CONCAT('sc_user.empid cleared (no such employee): ', LEFT(@dangling, 3500));
UPDATE sc_user SET empid = NULL WHERE ISNULL(empid, '') <> '' AND hremployee_id IS NULL;
UPDATE sc_user SET empid = NULL WHERE empid = '';

UPDATE ur SET empid = u.empid
FROM sc_user_role ur JOIN sc_user u ON u.userid = ur.userid
WHERE ISNULL(ur.empid, '') <> ISNULL(u.empid, '');

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_sc_user_hremployee')
    ALTER TABLE sc_user ADD CONSTRAINT FK_sc_user_hremployee FOREIGN KEY (hremployee_id) REFERENCES HREMPLOYEE (id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sc_user_hremployee_id' AND object_id = OBJECT_ID('sc_user'))
    CREATE INDEX IX_sc_user_hremployee_id ON sc_user (hremployee_id);";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_sc_user_hremployee') ALTER TABLE sc_user DROP CONSTRAINT FK_sc_user_hremployee;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sc_user_hremployee_id' AND object_id = OBJECT_ID('sc_user')) DROP INDEX IX_sc_user_hremployee_id ON sc_user;
IF COL_LENGTH('sc_user', 'hremployee_id') IS NOT NULL ALTER TABLE sc_user DROP COLUMN hremployee_id;
-- empid ที่ลอยอยู่ซึ่งถูกล้าง ไม่ได้คืนค่า (ชี้ไปหาพนักงานที่ไม่มีอยู่จริง)");
        }
    }
}
