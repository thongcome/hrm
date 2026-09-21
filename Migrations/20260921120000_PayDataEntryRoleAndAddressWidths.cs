using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // CEO, 21 ก.ย. 2569 (ลูกค้า PST):
    //  1. "ต้องมี role ที่ไม่มีสิทธิ์เห็นเงินเดือน ทำพวกเงินรายเดือน เพื่อส่งต่อข้อมูลเงินได้-เงินหักระหว่างเดือน ให้อีก role (payroll cal) คำนวณ"
    //     → บทบาท PAY_DATA_ENTRY เห็นหน้าเดียวคือ "คีย์เงินได้/เงินหักรายงวด" (/pay/adhoc/key) ผ่านรหัสเมนูใหม่ PAY_ADHOC_KEY
    //       (เดิมหน้านี้ใช้รหัส PAY_ADHOC ร่วมกับ รายการเฉพาะกิจ/รายการประจำ/เงินเบิกล่วงหน้า ซึ่งเห็นค่าตอบแทนของทุกคน)
    //       ทุกอย่างที่คีย์เป็น "รออนุมัติ" — คนอนุมัติและคนคำนวณคือ PAYROLL_OFFICER ที่หน้า /pay/adhoc ซึ่งบทบาทนี้เปิดไม่ได้
    //     ทุกบทบาทที่เคยเปิดหน้านี้ได้ยังเปิดได้เหมือนเดิม (grant ผูกกับ menuid ไม่ใช่รหัส)
    //  2. "field text ไหนน้อยกว่า 50 ขยายเป็น 50" — ตาราง address (ตารางเดิมจากระบบ JSP pis.address): จังหวัด 20, ไปรษณีย์ 10,
    //     โทรสำนักงาน 18, แฟกซ์ 18 → กว้างขึ้น (ไม่มีคอลัมน์ไหนอยู่ใน index — ALTER ตรง ๆ ได้)
    public partial class PayDataEntryRoleAndAddressWidths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE address ALTER COLUMN province nvarchar(100) NULL;
ALTER TABLE address ALTER COLUMN postcode nvarchar(50) NULL;
ALTER TABLE address ALTER COLUMN officeno nvarchar(50) NULL;
ALTER TABLE address ALTER COLUMN fax nvarchar(50) NULL;");

            migrationBuilder.Sql(@"
-- หน้า /pay/adhoc/key ได้รหัสเมนูของตัวเอง
UPDATE sc_menu SET menucode = 'PAY_ADHOC_KEY', moddate = GETDATE(), modby = 'migration'
 WHERE CAST(url AS nvarchar(200)) = '/pay/adhoc/key' AND menucode = 'PAY_ADHOC';

DECLARE @companyId bigint = (SELECT TOP 1 company_id FROM sc_role WHERE name = N'Admin' ORDER BY roleid);
IF @companyId IS NULL SET @companyId = 1;

IF NOT EXISTS (SELECT 1 FROM sc_role WHERE rolecode = 'PAY_DATA_ENTRY')
    INSERT INTO sc_role (company_id, name, abbr, rolelevel, rolecode, isactive, isHeader, moddate, modby)
    VALUES (@companyId, N'เจ้าหน้าที่คีย์เงินได้-เงินหัก (ไม่เห็นเงินเดือน)', 'PAYKEY', 1, 'PAY_DATA_ENTRY', 1, 0, GETDATE(), 'migration');

DECLARE @entry bigint = (SELECT roleid FROM sc_role WHERE rolecode = 'PAY_DATA_ENTRY');

INSERT INTO sc_role_menu (menuid, roleid, isactive, canedit, startdate, moddate, modby)
SELECT m.menuid, @entry, 1, 1, GETDATE(), GETDATE(), 'migration'
FROM sc_menu m
WHERE m.isactive = 1 AND m.menucode = 'PAY_ADHOC_KEY'
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.menuid = m.menuid AND x.roleid = @entry);

-- สิทธิ์ต่อหน้า: หน้านี้ตรวจ path ของตัวเองแล้ว (เดิมยืมของ /pay/adhoc) — บทบาทที่แก้ /pay/adhoc ได้ ต้องแก้หน้านี้ได้เหมือนเดิม
UPDATE k SET k.cancreate = a.cancreate, k.canedit = a.canedit
FROM sc_program_role k JOIN sc_program_role a ON a.roleid = k.roleid AND a.progpath = '/pay/adhoc'
WHERE k.progpath = '/pay/adhoc/key' AND (a.cancreate = 1 OR a.canedit = 1);

-- ฐานที่แอปยังไม่เคย start หลังมีหน้านี้ (ยังไม่มีแถวของ /pay/adhoc/key): สร้างให้ตามสิทธิ์ของ /pay/adhoc ไม่รอ auto-seed ที่จะให้แค่อ่าน
INSERT INTO sc_program_role (roleid, progpath, cancreate, canread, canedit, candelete, isactive)
SELECT a.roleid, '/pay/adhoc/key', a.cancreate, a.canread, a.canedit, 0, 1
FROM sc_program_role a
WHERE a.progpath = '/pay/adhoc' AND a.roleid <> @entry
  AND NOT EXISTS (SELECT 1 FROM sc_program_role x WHERE x.roleid = a.roleid AND x.progpath = '/pay/adhoc/key');

IF EXISTS (SELECT 1 FROM sc_program_role WHERE roleid = @entry AND progpath = '/pay/adhoc/key')
    UPDATE sc_program_role SET cancreate = 1, canread = 1, canedit = 1, isactive = 1 WHERE roleid = @entry AND progpath = '/pay/adhoc/key';
ELSE
    INSERT INTO sc_program_role (roleid, progpath, cancreate, canread, canedit, candelete, isactive)
    VALUES (@entry, '/pay/adhoc/key', 1, 1, 1, 0, 1);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @entry bigint = (SELECT roleid FROM sc_role WHERE rolecode = 'PAY_DATA_ENTRY');
DELETE FROM sc_program_role WHERE roleid = @entry;
DELETE FROM sc_role_menu WHERE roleid = @entry;
UPDATE sc_user_role SET isactive = 0 WHERE roleid = @entry;
UPDATE sc_role SET isactive = 0 WHERE roleid = @entry;
UPDATE sc_menu SET menucode = 'PAY_ADHOC' WHERE CAST(url AS nvarchar(200)) = '/pay/adhoc/key' AND menucode = 'PAY_ADHOC_KEY';
-- ความกว้างคอลัมน์ address ไม่ย่อกลับ (ข้อมูลที่ยาวกว่าเดิมจะถูกตัด)");
        }
    }
}

