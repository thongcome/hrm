-- 12 ก.ย. 2569 — CEO ตัดสิน "ใครแก้ได้" ใน Super Master:
--   HR (payroll)  : รอบจ่าย, งวด, ตารางภาษี, ค่าลดหย่อน, ประกันสังคม, นโยบายหักสาย/ขาด, ธงรายการรับ-จ่าย, บัญชี GL, กองทุนสำรองเลี้ยงชีพ
--   พนักงาน       : กองทุนสำรองเลี้ยงชีพ (ของตัวเอง — หน้า ESS ที่มีอยู่แล้ว)
--   System admin  : สิทธิ์การใช้งานรายหน้า, จัดการเมนู (เหมือนเดิม)
-- ทำ 3 อย่าง: (1) role HR เห็นเมนูกลุ่มเงินเดือนทั้งหมด (gate PAY_ADMIN) (2) role HR ได้สิทธิ์สร้าง/แก้บนหน้าค่าตั้งต้น
-- (3) ทะเบียน Super Master ทุกแถว = "ลูกค้าแก้เองได้" · เมนู Super Master เพิ่มในกลุ่มเงินเดือนให้ HR เห็นด้วย
-- รันซ้ำได้
SET NOCOUNT ON;
GO
DECLARE @hr bigint = (SELECT TOP 1 roleid FROM sc_role WHERE rolecode = 'HR' AND isactive = 1);
IF @hr IS NULL BEGIN PRINT 'role HR not found'; RETURN; END;

-- (1) เมนูทุกแถวที่ gate ด้วย PAY_ADMIN
INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
SELECT @hr, m.menuid, 1, GETDATE()
FROM sc_menu m
WHERE m.menucode = 'PAY_ADMIN' AND m.isactive = 1
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = @hr AND x.menuid = m.menuid);
-- กลุ่มเงินเดือน (แถวหัวกลุ่ม) ด้วย ไม่งั้นลิงก์ไม่มีที่อยู่
INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
SELECT @hr, m.menuid, 1, GETDATE()
FROM sc_menu m
WHERE m.menucode = 'GRP_PAYROLL'
  AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = @hr AND x.menuid = m.menuid);

-- Super Master ในกลุ่มเงินเดือน (gate PAY_ADMIN) — admin ยังมีแถวในกลุ่มจัดการระบบเหมือนเดิม
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/admin/super-master' AND uppermenucode = 'GRP_PAYROLL')
BEGIN
    DECLARE @grp bigint = (SELECT TOP 1 menugroupid FROM sc_menu WHERE uppermenucode = 'GRP_PAYROLL');
    DECLARE @id  bigint = (SELECT ISNULL(MAX(menuid), 0) + 1 FROM sc_menu);
    SET IDENTITY_INSERT sc_menu ON;
    INSERT INTO sc_menu (menuid, menuname, menuname_en, menulevel, isfinal, menuorder, menucode, uppermenucode, isshow, url, isactive, menugroupid, moddate, modby)
    VALUES (@id, N'Super Master (ค่าตั้งต้นเงินเดือน)', 'Super Master', 2, 1, 1, 'PAY_ADMIN', 'GRP_PAYROLL', 1, '/admin/super-master', 1, ISNULL(@grp, 1), GETDATE(), 'ScMenuNavSeeder');
    SET IDENTITY_INSERT sc_menu OFF;
    INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
    SELECT DISTINCT r.roleid, @id, 1, GETDATE() FROM sc_role r WHERE r.rolecode IN ('admin', 'HR') AND r.isactive = 1
      AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = r.roleid AND x.menuid = @id);
END;

-- (2) สิทธิ์สร้าง/แก้ (ไม่ให้ลบ) บนหน้าค่าตั้งต้นที่ CEO มอบให้ HR
UPDATE sc_program_role SET cancreate = 1, canread = 1, canedit = 1, moddate = GETDATE(), modby = 'CEO 2026-09-12 Super Master'
WHERE roleid = @hr AND progpath IN (
    '/admin/super-master', '/admin/super-master/tax-brackets',
    '/pay/admin/pay-schedules', '/pay/admin/periods', '/pay/admin/tax-deduction-config', '/hrucfsecurities',
    '/pay/admin/attendance-deduction', '/pay/admin/pay-item-types', '/pay/admin/gl-account-mapping',
    '/pay/admin/provident-fund-policy');
-- หน้าที่ยังไม่มีแถวของ HR (ระบบจะ seed ตอนเริ่ม แต่ seed เป็นอ่านอย่างเดียว) — สร้างให้เลย
INSERT INTO sc_program_role (roleid, progpath, cancreate, canread, canedit, candelete, isactive, moddate, modby)
SELECT @hr, p.path, 1, 1, 1, 0, 1, GETDATE(), 'CEO 2026-09-12 Super Master'
FROM (VALUES ('/admin/super-master'), ('/admin/super-master/tax-brackets'), ('/pay/admin/pay-schedules'), ('/pay/admin/periods'),
             ('/pay/admin/tax-deduction-config'), ('/hrucfsecurities'), ('/pay/admin/attendance-deduction'), ('/pay/admin/pay-item-types'),
             ('/pay/admin/gl-account-mapping'), ('/pay/admin/provident-fund-policy')) p(path)
WHERE NOT EXISTS (SELECT 1 FROM sc_program_role x WHERE x.roleid = @hr AND x.progpath = p.path);

-- (3) ทะเบียน: ทุกค่าที่มีอยู่ = ลูกค้า (HR) แก้เองได้
UPDATE Sys_ProtectedSetting SET EditableBy = 1, ModifiedDate = GETDATE() WHERE EditableBy = 0;
GO
PRINT '--- HR rights now';
SELECT p.progpath, p.cancreate, p.canedit, p.candelete FROM sc_program_role p JOIN sc_role r ON r.roleid = p.roleid WHERE r.rolecode = 'HR' AND p.progpath LIKE '/pay/admin/%' OR (r.rolecode = 'HR' AND p.progpath LIKE '/admin/super-master%') ORDER BY p.progpath;
GO
