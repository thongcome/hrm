-- 12 ก.ย. 2569 — CEO: "บัญชี : บัญชี GL ฝั่งนายจ้าง" — บัญชี GL เป็นของฝ่ายบัญชี ไม่ใช่ HR
--   บทบาทใหม่ ACCOUNTING (ฝ่ายบัญชี) · หน้าบัญชี GL มี gate ของตัวเอง PAY_GL_ADMIN (ไม่ใช่ PAY_ADMIN ทั้งก้อน)
--   admin + ACCOUNTING เห็นและแก้ได้ · HR อ่านได้อย่างเดียว
-- รันซ้ำได้
SET NOCOUNT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sc_role WHERE rolecode = 'ACCOUNTING')
BEGIN
    INSERT INTO sc_role (company_id, name, abbr, rolelevel, upperrole, rolecode, isactive, moddate, isHeader)
    SELECT TOP 1 company_id, N'ฝ่ายบัญชี (Accounting)', N'ACC', rolelevel, NULL, 'ACCOUNTING', 1, GETDATE(), 0
    FROM sc_role WHERE rolecode = 'HR';
END;
GO
DECLARE @acc bigint = (SELECT TOP 1 roleid FROM sc_role WHERE rolecode = 'ACCOUNTING');
DECLARE @hr  bigint = (SELECT TOP 1 roleid FROM sc_role WHERE rolecode = 'HR');
DECLARE @adm bigint = (SELECT TOP 1 roleid FROM sc_role WHERE rolecode = 'admin');

-- gate ของหน้าบัญชี GL แยกออกมา
UPDATE sc_menu SET menucode = 'PAY_GL_ADMIN' WHERE CAST(url AS nvarchar(400)) = '/pay/admin/gl-account-mapping';

-- ใครเห็นเมนูบัญชี GL: admin + ACCOUNTING (HR ไม่เห็นเมนู เปิดผ่านการ์ดใน Super Master ได้ แต่แก้ไม่ได้)
DELETE rm FROM sc_role_menu rm JOIN sc_menu m ON m.menuid = rm.menuid
WHERE CAST(m.url AS nvarchar(400)) = '/pay/admin/gl-account-mapping' AND rm.roleid = @hr;
INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
SELECT r.roleid, m.menuid, 1, GETDATE()
FROM sc_menu m CROSS JOIN sc_role r
WHERE CAST(m.url AS nvarchar(400)) = '/pay/admin/gl-account-mapping' AND r.roleid IN (@acc, @adm)
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = r.roleid AND x.menuid = m.menuid);
-- กลุ่มเงินเดือน (แถวหัวกลุ่ม) ให้ ACCOUNTING เห็นลิงก์
INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
SELECT @acc, m.menuid, 1, GETDATE() FROM sc_menu m
WHERE m.menucode = 'GRP_PAYROLL' AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = @acc AND x.menuid = m.menuid);

-- สิทธิ์รายหน้า: ACCOUNTING สร้าง/แก้ · HR อ่านอย่างเดียว
UPDATE sc_program_role SET cancreate = 0, canedit = 0, candelete = 0, moddate = GETDATE(), modby = 'CEO 2026-09-12 GL=Accounting'
WHERE roleid = @hr AND progpath = '/pay/admin/gl-account-mapping';
IF EXISTS (SELECT 1 FROM sc_program_role WHERE roleid = @acc AND progpath = '/pay/admin/gl-account-mapping')
    UPDATE sc_program_role SET cancreate = 1, canread = 1, canedit = 1, moddate = GETDATE(), modby = 'CEO 2026-09-12 GL=Accounting'
    WHERE roleid = @acc AND progpath = '/pay/admin/gl-account-mapping';
ELSE
    INSERT INTO sc_program_role (roleid, progpath, cancreate, canread, canedit, candelete, isactive, moddate, modby)
    VALUES (@acc, '/pay/admin/gl-account-mapping', 1, 1, 1, 0, 1, GETDATE(), 'CEO 2026-09-12 GL=Accounting');

-- ทะเบียน Super Master: ระบุว่าเป็นของฝ่ายบัญชี
UPDATE Sys_ProtectedSetting SET DisplayName = N'บัญชี GL ของรายการฝั่งนายจ้าง/เงินเดือนค้างจ่าย (ฝ่ายบัญชี)', ModifiedDate = GETDATE()
WHERE SettingKey = N'PAY.GL.MAPPING';
GO
PRINT '--- GL mapping access now';
SELECT r.rolecode, p.cancreate, p.canedit FROM sc_program_role p JOIN sc_role r ON r.roleid = p.roleid WHERE p.progpath = '/pay/admin/gl-account-mapping' ORDER BY r.rolecode;
SELECT r.rolecode AS sees_menu FROM sc_role_menu rm JOIN sc_menu m ON m.menuid = rm.menuid JOIN sc_role r ON r.roleid = rm.roleid WHERE CAST(m.url AS nvarchar(400)) = '/pay/admin/gl-account-mapping';
GO
