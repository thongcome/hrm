-- ตรวจความพร้อมของฐานลูกค้า ก่อนใช้งานจริง/คำนวณเงินเดือนรอบแรก
-- รันกับฐานของลูกค้า (อ่านอย่างเดียว ไม่แก้อะไร) — ทุกแถวที่ขึ้น "ต้องแก้" คือสิ่งที่ยังทำไม่เสร็จ
-- ต้องใส่ -f 65001 (UTF-8 codepage) เสมอ — ไฟล์นี้ใช้ภาษาไทยเป็นชื่อคอลัมน์จริงในโค้ด ไม่ใช่แค่ในคอมเมนต์
-- ถ้าไม่ใส่ sqlcmd จะถอดรหัสไฟล์ผิด codepage แล้วรายงาน "Incorrect syntax" ที่ตัวอักษรไทยทันที:
--   sqlcmd -S . -d <ชื่อฐานลูกค้า> -i 30_check_ready.sql -f 65001
SET NOCOUNT ON;

DECLARE @company nvarchar(50) = (SELECT TOP 1 code FROM com_company WHERE isActive = 1 ORDER BY id);
DECLARE @companyIdNum bigint = (SELECT TOP 1 id FROM com_company WHERE isActive = 1 ORDER BY id);
DECLARE @result TABLE (ลำดับ int, หัวข้อ nvarchar(100), สถานะ nvarchar(20), รายละเอียด nvarchar(400));

INSERT INTO @result VALUES
 (0, N'บริษัทที่ใช้งาน', CASE WHEN @company IS NULL THEN N'ต้องแก้' ELSE N'ผ่าน' END,
     ISNULL(N'รหัสบริษัท = ' + @company, N'ไม่มีบริษัทที่เปิดใช้งาน — ยังไม่ได้รัน 20_new_customer.sql'));

-- 1. ยังเหลือร่องรอยบริษัทเดโมไหม
INSERT INTO @result
SELECT 1, N'ล้างข้อมูลเดโม (ADVD)',
       CASE WHEN EXISTS (SELECT 1 FROM com_company WHERE code = 'ADVD')
                 OR EXISTS (SELECT 1 FROM HREMPLOYEE WHERE companyid = 'ADVD') THEN N'ต้องแก้' ELSE N'ผ่าน' END,
       CASE WHEN EXISTS (SELECT 1 FROM com_company WHERE code = 'ADVD')
                 OR EXISTS (SELECT 1 FROM HREMPLOYEE WHERE companyid = 'ADVD')
            THEN N'ยังพบรหัสบริษัท ADVD — ฐานนี้ไม่ได้มาจากแม่แบบสะอาด หรือยังไม่ได้เปลี่ยนชื่อบริษัท (20_new_customer.sql)'
            ELSE N'ไม่มีข้อมูลบริษัทเดโมหลงเหลือ' END;

-- 2. บัญชีผู้ดูแลระบบ (ต้องมี ไม่ลบ — ใช้ตั้งรหัสผ่านครั้งแรกผ่าน --init-admin)
INSERT INTO @result
SELECT 2, N'บัญชี advadmin',
       CASE WHEN EXISTS (SELECT 1 FROM sc_user WHERE loginname = 'advadmin' AND ISNULL(isdisable,0) = 0) THEN N'ผ่าน' ELSE N'ต้องแก้' END,
       CASE WHEN EXISTS (SELECT 1 FROM sc_user WHERE loginname = 'advadmin' AND ISNULL(isdisable,0) = 0)
            THEN N'มีอยู่และเปิดใช้งาน (ห้ามลบ) — ถ้ายังไม่เคยตั้งรหัสผ่าน ให้รัน: dotnet HRM.dll --init-admin'
            ELSE N'ไม่มีหรือถูกปิด — ฐานต้องมาจากแม่แบบ (10_make_clean_template.sql)' END;

-- 3. ตั้งค่าบริษัท/สลิป (หัวเอกสารราชการ)
INSERT INTO @result
SELECT 3, N'ตั้งค่าสลิป/ข้อมูลบริษัท',
       CASE WHEN s.CompanyId IS NULL OR NULLIF(LTRIM(RTRIM(ISNULL(s.CompanyName,''))),'') IS NULL
                 OR NULLIF(LTRIM(RTRIM(ISNULL(s.CompanyTaxId,''))),'') IS NULL THEN N'ต้องแก้' ELSE N'ผ่าน' END,
       CASE WHEN s.CompanyId IS NULL THEN N'ยังไม่มีแถวตั้งค่าของบริษัทนี้ — เปิดหน้า /pay/admin/payslip-settings แล้วบันทึก'
            ELSE N'ชื่อบริษัท: ' + ISNULL(NULLIF(s.CompanyName,''), N'(ว่าง)')
               + N' · เลขผู้เสียภาษี: ' + ISNULL(NULLIF(s.CompanyTaxId,''), N'(ว่าง)')
               + N' · เลขบัญชีนายจ้าง สปส.: ' + ISNULL(NULLIF(s.SsoEmployerAccountNo,''), N'(ว่าง)')
               + N' · จังหวัด: ' + ISNULL(NULLIF(s.WorkProvince,''), N'(ว่าง)') END
FROM (SELECT TOP 1 * FROM Pay_PayslipSettings WHERE CompanyId = @company) s
RIGHT JOIN (SELECT 1 x) dummy ON 1 = 1;

-- 4. อัตราประกันสังคม
INSERT INTO @result
SELECT 4, N'อัตราประกันสังคม',
       CASE WHEN EXISTS (SELECT 1 FROM HRUCFSECURITY WHERE companyid = @company AND SECURITY_CODE = '01') THEN N'ผ่าน' ELSE N'ต้องแก้' END,
       ISNULL((SELECT TOP 1 N'ลูกจ้าง ' + CAST(PERCEN_SECURITY AS nvarchar(20)) + N'% เพดาน ' + CAST(SECURITY_MONEY AS nvarchar(20))
               FROM HRUCFSECURITY WHERE companyid = @company AND SECURITY_CODE = '01' ORDER BY ISNULL(EffectiveFrom, '1900-01-01') DESC),
              N'ไม่มีอัตราของบริษัทนี้ — คำนวณเงินเดือนจะล้มทันที');

-- 5. ตารางภาษีของปีนี้
INSERT INTO @result
SELECT 5, N'ตารางภาษีปี ' + CAST(YEAR(GETDATE()) AS nvarchar(4)),
       CASE WHEN EXISTS (SELECT 1 FROM Pay_TaxBracket WHERE EffectiveYear = YEAR(GETDATE()) AND IsActive = 1) THEN N'ผ่าน' ELSE N'ต้องแก้' END,
       CAST((SELECT COUNT(*) FROM Pay_TaxBracket WHERE EffectiveYear = YEAR(GETDATE()) AND IsActive = 1) AS nvarchar(10)) + N' ขั้น';

-- 6. รูปแบบไฟล์ธนาคาร
INSERT INTO @result
SELECT 6, N'รูปแบบไฟล์ธนาคาร',
       CASE WHEN EXISTS (SELECT 1 FROM Pay_BankFileFormat WHERE CompanyId = @company AND IsActive = 1) THEN N'ผ่าน' ELSE N'ต้องแก้' END,
       ISNULL((SELECT TOP 1 N'ใช้: ' + Name FROM Pay_BankFileFormat WHERE CompanyId = @company AND IsActive = 1 ORDER BY IsDefault DESC),
              N'ยังไม่มีของบริษัทนี้ — ตั้งที่ /pay/admin/bank-file-formats ตามสเปกธนาคารของลูกค้า (ทำไฟล์โอนเงินไม่ได้ถ้าไม่มี)');

-- 7. ค่าจ้างขั้นต่ำ (เตือน ไม่ใช่บังคับ)
INSERT INTO @result
SELECT 7, N'ค่าจ้างขั้นต่ำ',
       CASE WHEN EXISTS (SELECT 1 FROM Pay_MinimumWage WHERE IsActive = 1) THEN N'ผ่าน' ELSE N'ควรกรอก' END,
       ISNULL((SELECT TOP 1 N'ล่าสุด ' + CAST(DailyAmount AS nvarchar(20)) + N' บาท/วัน' + ISNULL(N' (' + ProvinceName + N')', N' (ทุกจังหวัด)')
               FROM Pay_MinimumWage WHERE IsActive = 1 ORDER BY EffectiveFrom DESC),
              N'ตารางว่าง = ระบบไม่ตรวจว่าจ่ายต่ำกว่าขั้นต่ำ — กรอกจากประกาศจริงที่ /pay/admin/minimum-wage');

-- 8. ผู้ใช้และบทบาทเงินเดือน (ต้องมีคนทำและคนอนุมัติคนละคน)
INSERT INTO @result
SELECT 8, N'ผู้ทำ/ผู้อนุมัติเงินเดือน',
       CASE WHEN (SELECT COUNT(DISTINCT ur.userid) FROM sc_user_role ur JOIN sc_role r ON r.roleid = ur.roleid
                  WHERE ur.isactive = 1 AND r.rolecode = 'PAYROLL_OFFICER') > 0
             AND (SELECT COUNT(DISTINCT ur.userid) FROM sc_user_role ur JOIN sc_role r ON r.roleid = ur.roleid
                  WHERE ur.isactive = 1 AND r.rolecode = 'PAYROLL_APPROVER') > 0 THEN N'ผ่าน' ELSE N'ต้องแก้' END,
       N'เจ้าหน้าที่เงินเดือน ' + CAST((SELECT COUNT(DISTINCT ur.userid) FROM sc_user_role ur JOIN sc_role r ON r.roleid = ur.roleid
                                        WHERE ur.isactive = 1 AND r.rolecode = 'PAYROLL_OFFICER') AS nvarchar(10))
       + N' คน · ผู้อนุมัติ ' + CAST((SELECT COUNT(DISTINCT ur.userid) FROM sc_user_role ur JOIN sc_role r ON r.roleid = ur.roleid
                                       WHERE ur.isactive = 1 AND r.rolecode = 'PAYROLL_APPROVER') AS nvarchar(10))
       + N' คน (ต้องคนละคน เพราะคนคำนวณอนุมัติรอบตัวเองไม่ได้)';

-- 9. workflow อนุมัติรอบเงินเดือน
INSERT INTO @result
SELECT 9, N'workflow อนุมัติรอบเงินเดือน',
       CASE WHEN EXISTS (SELECT 1 FROM wf_workflow WHERE workflowcode = 'PAYROLL_RUN_APPROVAL' AND isactive = 1) THEN N'ผ่าน' ELSE N'ปิดอยู่' END,
       CASE WHEN EXISTS (SELECT 1 FROM wf_workflow WHERE workflowcode = 'PAYROLL_RUN_APPROVAL' AND isactive = 1)
            THEN N'เปิดอยู่ — งานเข้ากล่องงานผู้อนุมัติ'
            ELSE N'ปิดอยู่ = ใช้ปุ่มอนุมัติขั้นเดียวในหน้ารอบ (ยอมรับได้ถ้าลูกค้าต้องการแบบนั้น)' END;

-- 10. พนักงาน
INSERT INTO @result
SELECT 10, N'ข้อมูลพนักงาน',
       CASE WHEN (SELECT COUNT(*) FROM HREMPLOYEE WHERE companyid = @company AND IsActive = 1) = 0 THEN N'ต้องแก้' ELSE N'ผ่าน' END,
       CAST((SELECT COUNT(*) FROM HREMPLOYEE WHERE companyid = @company AND IsActive = 1) AS nvarchar(10)) + N' คน'
       + N' · ไม่มีเลขบัญชี ' + CAST((SELECT COUNT(*) FROM HREMPLOYEE WHERE companyid = @company AND IsActive = 1
                                       AND NULLIF(LTRIM(RTRIM(ISNULL(SALEXP_ACCID,''))),'') IS NULL) AS nvarchar(10)) + N' คน'
       + N' · ไม่มีเงินเดือน/ค่าจ้าง ' + CAST((SELECT COUNT(*) FROM HREMPLOYEE WHERE companyid = @company AND IsActive = 1
                                       AND ISNULL(SALARY_AMT,0) <= 0 AND ISNULL(DAILY_WAGE,0) <= 0) AS nvarchar(10)) + N' คน';

-- 11. วันหยุดประจำปีของปีนี้ (ใช้คิดค่าจ้างรายวัน/วันทำงาน/สิทธิลา)
INSERT INTO @result
SELECT 11, N'วันหยุดประจำปี ' + CAST(YEAR(GETDATE()) AS nvarchar(4)),
       CASE WHEN (SELECT COUNT(*) FROM Lve_CompanyHoliday WHERE CompanyId = @company AND IsActive = 1
                   AND YEAR(HolidayDate) = YEAR(GETDATE())) = 0 THEN N'ควรกรอก' ELSE N'ผ่าน' END,
       CAST((SELECT COUNT(*) FROM Lve_CompanyHoliday WHERE CompanyId = @company AND IsActive = 1
              AND YEAR(HolidayDate) = YEAR(GETDATE())) AS nvarchar(10)) + N' วัน';

-- 12. เงินได้จากนายจ้างเดิม (ถ้าขึ้นระบบกลางปีและพนักงานย้ายมาจากที่อื่น — ไม่งั้นภาษีสะสม/50 ทวิ ปลายปีขาด)
INSERT INTO @result
SELECT 12, N'เงินได้สะสมจากนายจ้างเดิม (ขึ้นระบบกลางปี)',
       CASE WHEN MONTH(GETDATE()) = 1 THEN N'ไม่ต้องใช้'
            WHEN (SELECT COUNT(*) FROM Pay_EmployeePriorEmployerIncome WHERE IsActive = 1
                   AND TaxYear = YEAR(GETDATE())) = 0
                 AND EXISTS (SELECT 1 FROM HREMPLOYEE WHERE companyid = @company AND IsActive = 1) THEN N'ควรกรอก'
            ELSE N'ผ่าน' END,
       CASE WHEN MONTH(GETDATE()) = 1 THEN N'ขึ้นระบบต้นปี ไม่ต้องมียอดยกมา'
            ELSE CAST((SELECT COUNT(*) FROM Pay_EmployeePriorEmployerIncome WHERE IsActive = 1
                        AND TaxYear = YEAR(GETDATE())) AS nvarchar(10))
                 + N' คน — กรอกจากหนังสือรับรองหัก ณ ที่จ่าย (50 ทวิ) ของนายจ้างเดิมที่ /pay/admin/prior-employer-income'
                 + N' มิฉะนั้นภาษีหัก ณ ที่จ่ายรายเดือนจะต่ำกว่าที่ควร' END;

-- 13. สิทธิ์หน้าจอ (AD.CRUDManage) เดินสายแล้วหรือยัง
INSERT INTO @result
SELECT 13, N'สิทธิ์หน้าจอ (AD.CRUDManage)',
       CASE WHEN (SELECT COUNT(*) FROM sc_program_role) = 0 THEN N'ต้องแก้' ELSE N'ผ่าน' END,
       CAST((SELECT COUNT(*) FROM sc_program_role) AS nvarchar(10)) + N' แถว'
       + N' — ถ้าเป็น 0 ให้เปิดแอปขึ้นครั้งหนึ่งก่อน (ProgramRoleService.SeedAsync เดินตอน startup) แล้วมอบสิทธิ์แก้ไขที่ /admin/system/program-rights';

SELECT ลำดับ, หัวข้อ, สถานะ, รายละเอียด FROM @result ORDER BY ลำดับ;

SELECT N'สรุป' AS สรุป,
       CAST(SUM(CASE WHEN สถานะ = N'ต้องแก้' THEN 1 ELSE 0 END) AS nvarchar(10)) + N' รายการที่ต้องแก้ก่อนใช้งานจริง · '
     + CAST(SUM(CASE WHEN สถานะ IN (N'ควรกรอก', N'ปิดอยู่') THEN 1 ELSE 0 END) AS nvarchar(10)) + N' รายการที่ควรตรวจ' AS ผล
FROM @result;
