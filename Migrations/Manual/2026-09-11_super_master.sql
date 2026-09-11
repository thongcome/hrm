-- 11 ก.ย. 2569 — Super Master: ทะเบียนค่าตั้งต้นที่ต้องควบคุม (config ไม่ใช่ลิสต์ในโค้ด), เพดานลดหย่อนกองทุนย้ายเป็น config,
-- เมนูหน้า /admin/super-master ในกลุ่มจัดการระบบ (เห็นเฉพาะ role ที่เห็น SYS_ADMIN ก่อน — ใครแก้ได้ค่อยตัดสินทีหลัง)
-- รันซ้ำได้
SET NOCOUNT ON;
GO

IF OBJECT_ID('Sys_ProtectedSetting', 'U') IS NULL
BEGIN
    CREATE TABLE Sys_ProtectedSetting (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sys_ProtectedSetting PRIMARY KEY,
        SettingKey nvarchar(100) NOT NULL,
        Area nvarchar(50) NOT NULL,
        TableName nvarchar(100) NOT NULL,
        FieldName nvarchar(200) NOT NULL,
        DisplayName nvarchar(200) NOT NULL,
        Reason nvarchar(500) NULL,
        PageRoute nvarchar(200) NULL,
        EditableBy int NOT NULL CONSTRAINT DF_Sys_ProtectedSetting_EditableBy DEFAULT 0,
        SortOrder int NOT NULL CONSTRAINT DF_Sys_ProtectedSetting_SortOrder DEFAULT 0,
        IsActive bit NOT NULL CONSTRAINT DF_Sys_ProtectedSetting_IsActive DEFAULT 1,
        ModifiedByUserId bigint NULL,
        ModifiedDate datetime2 NOT NULL CONSTRAINT DF_Sys_ProtectedSetting_ModifiedDate DEFAULT SYSDATETIME()
    );
    CREATE UNIQUE INDEX UX_Sys_ProtectedSetting_SettingKey ON Sys_ProtectedSetting (SettingKey);
END;
GO

IF COL_LENGTH('Pay_TaxDeductionSetting', 'ProvidentFundDeductionCapPerYear') IS NULL
BEGIN
    ALTER TABLE Pay_TaxDeductionSetting ADD ProvidentFundDeductionCapPerYear decimal(15,2) NOT NULL
        CONSTRAINT DF_Pay_TaxDeductionSetting_PfCap DEFAULT 500000;
END;
GO

-- ── ทะเบียนชุดแรก (add-missing ตาม SettingKey — แถวที่มีอยู่ไม่ถูกแตะ คำตัดสิน "ใครแก้ได้" ต้องรอด) ──
IF OBJECT_ID('tempdb..#Seed') IS NOT NULL DROP TABLE #Seed;
CREATE TABLE #Seed (SettingKey nvarchar(100), Area nvarchar(50), TableName nvarchar(100), FieldName nvarchar(200),
                    DisplayName nvarchar(200), Reason nvarchar(500), PageRoute nvarchar(200), SortOrder int);
INSERT INTO #Seed VALUES
 (N'PAY.SCHEDULE.PERIODS_PER_MONTH', N'Payroll', N'Pay_PaySchedule', N'PeriodsPerMonth', N'รอบจ่าย: จำนวนงวดต่อเดือน', N'กระทบประมาณการภาษีหัก ณ ที่จ่ายทั้งปีของพนักงานทุกคนในกลุ่ม', N'/pay/admin/pay-schedules', 10),
 (N'PAY.SCHEDULE.SECOND_TERM_DAY', N'Payroll', N'Pay_PaySchedule', N'SecondTermStartDay', N'รอบจ่าย: งวดที่ 2 เริ่มวันที่', N'กำหนดว่างวดไหนเป็นครึ่งแรก/ครึ่งหลัง ผิดแล้วภาษีและประกันสังคมของเดือนนั้นเพี้ยน', N'/pay/admin/pay-schedules', 11),
 (N'PAY.SCHEDULE.EFFECTIVE_DATES', N'Payroll', N'Pay_PaySchedule', N'EffectiveFrom/EffectiveTo', N'รอบจ่าย: วันมีผลของแถวที่ถูกใช้แล้ว', N'แก้ย้อนหลังทำให้งวดที่อนุมัติไปแล้วอ้างอิงปฏิทินคนละชุดกับที่คำนวณจริง', N'/pay/admin/pay-schedules', 12),
 (N'PAY.PERIODS', N'Payroll', N'Pay_PayrollPeriod', N'*', N'จัดการงวดเงินเดือน', N'วันเริ่ม-สิ้นสุดงวดกำหนดการตัดรอบเงินได้และการนับสะสมทั้งปี', N'/pay/admin/periods', 20),
 (N'PAY.TAX.BRACKETS', N'Payroll', N'Pay_TaxBracket', N'*', N'ตารางอัตราภาษีขั้นบันได', N'ค่าตามประกาศกรมสรรพากร ผิดแล้วหักภาษีผิดทุกคน', N'/admin/super-master/tax-brackets', 30),
 (N'PAY.TAX.PERSONAL_ALLOWANCE', N'Payroll', N'Pay_TaxDeductionSetting', N'PersonalAllowancePerYear', N'ค่าลดหย่อนส่วนตัวต่อปี', N'ค่าตามกฎหมาย (60,000) ใช้กับทุกคน', N'/pay/admin/tax-deduction-config', 31),
 (N'PAY.TAX.EXPENSE_RATE', N'Payroll', N'Pay_TaxDeductionSetting', N'ExpenseDeductionRate', N'อัตราค่าใช้จ่ายเหมา', N'ค่าตามกฎหมาย (50%) ใช้กับทุกคน', N'/pay/admin/tax-deduction-config', 32),
 (N'PAY.TAX.EXPENSE_CAP', N'Payroll', N'Pay_TaxDeductionSetting', N'ExpenseDeductionCap', N'เพดานค่าใช้จ่ายเหมา', N'ค่าตามกฎหมาย (100,000) ใช้กับทุกคน', N'/pay/admin/tax-deduction-config', 33),
 (N'PAY.TAX.PF_DEDUCTION_CAP', N'Payroll', N'Pay_TaxDeductionSetting', N'ProvidentFundDeductionCapPerYear', N'เพดานลดหย่อนเงินสะสมกองทุนสำรองเลี้ยงชีพต่อปี', N'ค่าตามกฎหมาย (500,000) ใช้กับทุกคน', N'/pay/admin/tax-deduction-config', 34),
 (N'PAY.SSO.RATE', N'Payroll', N'Hrucfsecurity', N'PercenSecurity', N'อัตราประกันสังคม (%)', N'ค่าตามประกาศสำนักงานประกันสังคม ผิดแล้วนำส่งผิดทั้งบริษัท', N'/hrucfsecurities', 40),
 (N'PAY.SSO.WAGE_CAP', N'Payroll', N'Hrucfsecurity', N'SecurityMoney', N'เพดานค่าจ้างประกันสังคมต่อเดือน', N'ค่าตามประกาศสำนักงานประกันสังคม', N'/hrucfsecurities', 41),
 (N'PAY.ATT.DAYS_PER_MONTH_DIVISOR', N'Payroll', N'Pay_AttendanceDeductionPolicy', N'DaysPerMonthDivisor', N'ตัวหารวันต่อเดือน (ค่าจ้างต่อวัน)', N'กำหนดอัตราต่อวันที่ใช้ทั้งหักขาดงานและคิดสัดส่วนเข้า-ออก', N'/pay/admin/attendance-deduction', 50),
 (N'PAY.ATT.PRORATION_MODE', N'Payroll', N'Pay_AttendanceDeductionPolicy', N'ProrationMode', N'วิธีคิดสัดส่วนเงินเดือนคนเข้า/ออกกลางงวด', N'เปลี่ยนแล้วตัวเลขของคนเข้า-ออกกลางเดือนเปลี่ยนทั้งบริษัท', N'/pay/admin/attendance-deduction', 51),
 (N'PAY.ATT.DAILY_WAGE_MODE', N'Payroll', N'Pay_AttendanceDeductionPolicy', N'DailyWageMode', N'วิธีนับวันจ่ายค่าจ้างรายวัน', N'กำหนดว่าพนักงานรายวันได้ค่าจ้างวันไหนบ้าง', N'/pay/admin/attendance-deduction', 52),
 (N'PAY.ITEM.BASE_FLAGS', N'Payroll', N'Pay_PayItemType', N'IsTaxable/IsSsoWageBase/IsProvidentFundWageBase', N'ธงของรายการรับ-จ่าย (เสียภาษี/ฐาน SSO/ฐานกองทุน)', N'ธงผิดหนึ่งตัว = ฐานภาษี ประกันสังคม หรือกองทุนผิดทุกคนที่มีรายการนั้น', N'/pay/admin/pay-item-types', 60);

INSERT INTO Sys_ProtectedSetting (SettingKey, Area, TableName, FieldName, DisplayName, Reason, PageRoute, EditableBy, SortOrder, IsActive)
SELECT s.SettingKey, s.Area, s.TableName, s.FieldName, s.DisplayName, s.Reason, s.PageRoute, 0, s.SortOrder, 1
FROM #Seed s
WHERE NOT EXISTS (SELECT 1 FROM Sys_ProtectedSetting p WHERE p.SettingKey = s.SettingKey);
DROP TABLE #Seed;
GO

-- ── เมนู: /admin/super-master ในกลุ่มจัดการระบบ, gate SYS_ADMIN, grant ให้ role ที่เห็น /admin/system/menus ─────
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/admin/super-master' AND uppermenucode = 'GRP_SYS_ADMIN')
BEGIN
    DECLARE @grp bigint = (SELECT TOP 1 menugroupid FROM sc_menu WHERE uppermenucode = 'GRP_SYS_ADMIN');
    DECLARE @id  bigint = (SELECT ISNULL(MAX(menuid), 0) + 1 FROM sc_menu);
    DECLARE @like bigint = (SELECT TOP 1 menuid FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/admin/system/menus' AND uppermenucode = 'GRP_SYS_ADMIN');

    SET IDENTITY_INSERT sc_menu ON;
    INSERT INTO sc_menu (menuid, menuname, menuname_en, menulevel, isfinal, menuorder, menucode, uppermenucode, isshow, url, isactive, menugroupid, moddate, modby)
    VALUES (@id, N'Super Master (ค่าตั้งต้นระบบ)', 'Super Master', 2, 1, 5, 'SYS_ADMIN', 'GRP_SYS_ADMIN', 1, '/admin/super-master', 1, ISNULL(@grp, 1), GETDATE(), 'ScMenuNavSeeder');
    SET IDENTITY_INSERT sc_menu OFF;

    INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
    SELECT DISTINCT rm.roleid, @id, 1, GETDATE()
    FROM sc_role_menu rm
    WHERE rm.menuid = @like
      AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = rm.roleid AND x.menuid = @id);
END;
GO
