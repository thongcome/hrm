-- 12 ก.ย. 2569 — ซ่อนเมนูหน้า Payroll ระบบเดิม (ยังไม่ลบหน้า ตามคำสั่ง "hold deletions until production")
--   /pay/runs คือระบบเงินเดือนตัวจริง หน้าเก่าเปิดตรงได้ถ้ารู้ URL และมีแบนเนอร์เตือน
-- รันซ้ำได้ (ตัว seeder ตั้ง isshow เฉพาะตอนสร้างแถวใหม่ จึงไม่กลับมาโชว์เอง)
SET NOCOUNT ON;
GO
UPDATE sc_menu SET isshow = 0, moddate = GETDATE(), modby = 'CEO 2026-09-12 legacy payroll hidden'
WHERE CAST(url AS nvarchar(400)) IN ('/hrpayrolldasboard', '/payrollprocess', '/AIpayrollprocess', '/AIpayrollprocess2', '/hrpayrolls', '/hrincome', '/payroll-calculate', '/hr-payroll-home', '/payroll-home-employee', '/payslip')
  AND isshow = 1;
GO
PRINT '--- legacy payroll menus';
SELECT CAST(menuid AS varchar) + ' ' + CAST(url AS nvarchar(100)) + ' show=' + CAST(isshow AS varchar) FROM sc_menu
WHERE CAST(url AS nvarchar(400)) IN ('/hrpayrolldasboard', '/payrollprocess', '/AIpayrollprocess', '/AIpayrollprocess2', '/hrpayrolls', '/hrincome', '/payroll-calculate', '/hr-payroll-home', '/payroll-home-employee', '/payslip');
GO
