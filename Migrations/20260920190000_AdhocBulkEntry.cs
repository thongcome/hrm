using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // คีย์เงินได้/เงินหักรายงวดทีละหลายคน + นำเข้า Excel (พอร์ตจาก Advance.Payroll ข้อ 3 — รวม 4 migration ของฝั่งนั้นเป็นอันเดียว):
    //  · ประเภทรายการ COMMISSION (+ เลขบัญชี GL 5035-COMMISSION) · เมนู /pay/adhoc/key คัดลอกสิทธิ์จาก /pay/adhoc
    //  · ItemDate / ReferenceNo บนรายการเฉพาะกิจ · ตารางประวัติไฟล์นำเข้า (SHA-256 กันไฟล์ซ้ำ/ผิดไฟล์)
    // Raw SQL ทั้งหมด จึงใช้ Designer แบบสั้นได้ (ดู CLAUDE.md หัวข้อ Build time)
    public partial class AdhocBulkEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Pay_PayItemType WHERE Code = 'COMMISSION')
    INSERT INTO Pay_PayItemType (Code, NameTh, NameEn, Category, DefaultSignFlag, IsSystemReserved, GLAccountCode, IsTaxable, IsSsoWageBase, IsProvidentFundWageBase, IsProrated, IsActive, SortOrder)
    VALUES ('COMMISSION', N'ค่าคอมมิชชั่น', 'Commission', 0, 1, 0, '5035-COMMISSION', 1, 1, 0, 0, 1, 10);
UPDATE Pay_PayItemType SET GLAccountCode = '5035-COMMISSION'
 WHERE Code = 'COMMISSION' AND (GLAccountCode IS NULL OR GLAccountCode = '');

IF COL_LENGTH('Pay_AdhocPayItem', 'ItemDate') IS NULL ALTER TABLE Pay_AdhocPayItem ADD ItemDate date NULL;
IF COL_LENGTH('Pay_AdhocPayItem', 'ReferenceNo') IS NULL ALTER TABLE Pay_AdhocPayItem ADD ReferenceNo nvarchar(100) NULL;

IF OBJECT_ID('Pay_AdhocImportBatch') IS NULL
BEGIN
    CREATE TABLE Pay_AdhocImportBatch (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pay_AdhocImportBatch PRIMARY KEY,
        CompanyId nvarchar(50) NOT NULL,
        TargetPeriod nvarchar(6) NOT NULL,
        TargetRunType int NOT NULL,
        TargetTermNo int NULL,
        FilePeriodStamp nvarchar(6) NULL,
        FileName nvarchar(260) NULL,
        FileSha256 nvarchar(64) NOT NULL,
        [RowCount] int NOT NULL,
        [Created] int NOT NULL,
        [Updated] int NOT NULL,
        [Skipped] int NOT NULL,
        ImportedByUserId bigint NOT NULL,
        ImportedByName nvarchar(200) NULL,
        ImportedAt datetime2 NOT NULL
    );
    CREATE INDEX IX_Pay_AdhocImportBatch_CompanyId_FileSha256 ON Pay_AdhocImportBatch (CompanyId, FileSha256);
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(200)) = '/pay/adhoc/key')
BEGIN
    INSERT INTO sc_menu (menuname, menulevel, isfinal, menuorder, menucode, isshow, icon, url, moddate, modby, uppermenucode, menugroupid, isactive, menuname_en)
    SELECT N'คีย์เงินได้-เงินหักรายงวด (หลายคน)', menulevel, isfinal, menuorder + 1, menucode, isshow, N'Icons.Material.Filled.TableChart', '/pay/adhoc/key', GETDATE(), 'migration', uppermenucode, menugroupid, 1, N'Bulk Pay Item Entry'
    FROM sc_menu WHERE CAST(url AS nvarchar(200)) = '/pay/adhoc';
END

INSERT INTO sc_role_menu (menuid, roleid, isactive, canedit, startdate, moddate, modby)
SELECT nm.menuid, rm.roleid, rm.isactive, rm.canedit, GETDATE(), GETDATE(), 'migration'
FROM sc_menu nm
JOIN sc_menu om ON CAST(om.url AS nvarchar(200)) = '/pay/adhoc'
JOIN sc_role_menu rm ON rm.menuid = om.menuid
WHERE CAST(nm.url AS nvarchar(200)) = '/pay/adhoc/key'
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.menuid = nm.menuid AND x.roleid = rm.roleid);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM sc_role_menu WHERE menuid IN (SELECT menuid FROM sc_menu WHERE CAST(url AS nvarchar(200)) = '/pay/adhoc/key');
DELETE FROM sc_menu WHERE CAST(url AS nvarchar(200)) = '/pay/adhoc/key';
IF OBJECT_ID('Pay_AdhocImportBatch') IS NOT NULL DROP TABLE Pay_AdhocImportBatch;
IF COL_LENGTH('Pay_AdhocPayItem', 'ReferenceNo') IS NOT NULL ALTER TABLE Pay_AdhocPayItem DROP COLUMN ReferenceNo;
IF COL_LENGTH('Pay_AdhocPayItem', 'ItemDate') IS NOT NULL ALTER TABLE Pay_AdhocPayItem DROP COLUMN ItemDate;");
        }
    }
}
