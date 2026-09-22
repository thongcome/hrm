using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // ระดับตำแหน่ง / ประเภทพนักงานของ HREMPLOYEE ผูกด้วย id (กติกา id/code — skill advance-data-discipline Part A0)
    // เดิม POS_CODE → Pos_ExecType.Code และ EMPTYPE_CODE → Pos_EmployeeType.Code จับคู่ด้วยข้อความ ไม่มี FK
    // ทั้งที่ประเภทพนักงานตัดสินว่าใครได้เงินเดือนในรอบ (IsPaidByPayroll) และค่าจ้างรายวันแบบไหน
    //
    // ตรวจ 22 ก.ย. 2569 (hrm): พนักงานมีระดับตำแหน่ง 7000 คน ประเภท 7001 คน — จับคู่ในบริษัทเดียวกันได้ครบทุกคน
    //   · ขยาย POS_CODE (6) / EMPTYPE_CODE (4) เป็น 50 — กติกา size 50 ขั้นต่ำสำหรับคอลัมน์รหัส
    //   · รหัสที่หาไม่เจอในบริษัท: พิมพ์รายชื่อไว้ แต่ **ไม่ล้าง** — ประเภทพนักงานมีผลต่อเงินเดือน ต้องให้คนตัดสิน
    //     (hook จะปฏิเสธการบันทึกพนักงานคนนั้นจนกว่าจะแก้รหัสให้ถูก)
    //   · unique (CompanyId, Code) ของ Pos_ExecType — รหัสเป็น key ตอนนำเข้า ต้องไม่ซ้ำ (ข้ามถ้ามีข้อมูลซ้ำ แล้วพิมพ์บอก)
    public partial class EmployeeTypesById : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE HREMPLOYEE ALTER COLUMN POS_CODE nvarchar(50) NULL;
ALTER TABLE HREMPLOYEE ALTER COLUMN EMPTYPE_CODE nvarchar(50) NULL;
IF COL_LENGTH('HREMPLOYEE', 'PosExecTypeId') IS NULL ALTER TABLE HREMPLOYEE ADD PosExecTypeId bigint NULL;
IF COL_LENGTH('HREMPLOYEE', 'EmployeeTypeId') IS NULL ALTER TABLE HREMPLOYEE ADD EmployeeTypeId bigint NULL;");

            // คอลัมน์ใหม่เพิ่งถูกเพิ่ม — ห่อไว้ใน EXEC ให้ compile ตอนรันจริง (ดู 20260922100000_OrgHeadsById)
            migrationBuilder.Sql("EXEC(N'" + Backfill.Replace("'", "''") + "')");
        }

        private const string Backfill = @"
UPDATE e SET PosExecTypeId = (SELECT TOP 1 p.Id FROM Pos_ExecType p WHERE p.CompanyId = e.companyid AND p.Code = e.POS_CODE ORDER BY p.IsActive DESC, p.Id)
FROM HREMPLOYEE e WHERE ISNULL(e.POS_CODE, '') <> '';
UPDATE e SET EmployeeTypeId = (SELECT TOP 1 t.Id FROM Pos_EmployeeType t WHERE t.CompanyId = e.companyid AND t.Code = e.EMPTYPE_CODE ORDER BY t.IsActive DESC, t.Id)
FROM HREMPLOYEE e WHERE ISNULL(e.EMPTYPE_CODE, '') <> '';
UPDATE HREMPLOYEE SET POS_CODE = NULL WHERE POS_CODE = '';
UPDATE HREMPLOYEE SET EMPTYPE_CODE = NULL WHERE EMPTYPE_CODE = '';

DECLARE @missing nvarchar(max) = (
    SELECT STRING_AGG(CONCAT(companyid, '/', EMP_NO, ' pos=', POS_CODE), ', ') FROM HREMPLOYEE
    WHERE POS_CODE IS NOT NULL AND PosExecTypeId IS NULL);
IF @missing IS NOT NULL PRINT CONCAT('POS_CODE not found in Pos_ExecType (left as is, fix by hand): ', LEFT(@missing, 3500));
SET @missing = (
    SELECT STRING_AGG(CONCAT(companyid, '/', EMP_NO, ' type=', EMPTYPE_CODE), ', ') FROM HREMPLOYEE
    WHERE EMPTYPE_CODE IS NOT NULL AND EmployeeTypeId IS NULL);
IF @missing IS NOT NULL PRINT CONCAT('EMPTYPE_CODE not found in Pos_EmployeeType (left as is, fix by hand): ', LEFT(@missing, 3500));

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HREMPLOYEE_Pos_ExecType')
    ALTER TABLE HREMPLOYEE ADD CONSTRAINT FK_HREMPLOYEE_Pos_ExecType FOREIGN KEY (PosExecTypeId) REFERENCES Pos_ExecType (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HREMPLOYEE_Pos_EmployeeType')
    ALTER TABLE HREMPLOYEE ADD CONSTRAINT FK_HREMPLOYEE_Pos_EmployeeType FOREIGN KEY (EmployeeTypeId) REFERENCES Pos_EmployeeType (Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HREMPLOYEE_PosExecTypeId' AND object_id = OBJECT_ID('HREMPLOYEE'))
    CREATE INDEX IX_HREMPLOYEE_PosExecTypeId ON HREMPLOYEE (PosExecTypeId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HREMPLOYEE_EmployeeTypeId' AND object_id = OBJECT_ID('HREMPLOYEE'))
    CREATE INDEX IX_HREMPLOYEE_EmployeeTypeId ON HREMPLOYEE (EmployeeTypeId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Pos_ExecType_CompanyId_Code' AND object_id = OBJECT_ID('Pos_ExecType'))
BEGIN
    IF EXISTS (SELECT 1 FROM Pos_ExecType WHERE Code IS NOT NULL GROUP BY CompanyId, Code HAVING COUNT(1) > 1)
        PRINT 'UX_Pos_ExecType_CompanyId_Code skipped: duplicate (CompanyId, Code) rows';
    ELSE
        CREATE UNIQUE INDEX UX_Pos_ExecType_CompanyId_Code ON Pos_ExecType (CompanyId, Code) WHERE Code IS NOT NULL;
END";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Pos_ExecType_CompanyId_Code' AND object_id = OBJECT_ID('Pos_ExecType')) DROP INDEX UX_Pos_ExecType_CompanyId_Code ON Pos_ExecType;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HREMPLOYEE_Pos_ExecType') ALTER TABLE HREMPLOYEE DROP CONSTRAINT FK_HREMPLOYEE_Pos_ExecType;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HREMPLOYEE_Pos_EmployeeType') ALTER TABLE HREMPLOYEE DROP CONSTRAINT FK_HREMPLOYEE_Pos_EmployeeType;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HREMPLOYEE_PosExecTypeId' AND object_id = OBJECT_ID('HREMPLOYEE')) DROP INDEX IX_HREMPLOYEE_PosExecTypeId ON HREMPLOYEE;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HREMPLOYEE_EmployeeTypeId' AND object_id = OBJECT_ID('HREMPLOYEE')) DROP INDEX IX_HREMPLOYEE_EmployeeTypeId ON HREMPLOYEE;
IF COL_LENGTH('HREMPLOYEE', 'PosExecTypeId') IS NOT NULL ALTER TABLE HREMPLOYEE DROP COLUMN PosExecTypeId;
IF COL_LENGTH('HREMPLOYEE', 'EmployeeTypeId') IS NOT NULL ALTER TABLE HREMPLOYEE DROP COLUMN EmployeeTypeId;
-- POS_CODE / EMPTYPE_CODE คงขนาด 50 ไว้ (ย่อกลับอาจตัดข้อมูล)");
        }
    }
}
