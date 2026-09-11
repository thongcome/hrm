-- 11 ก.ย. 2569 — ฝั่งนายจ้างในไฟล์บัญชี (audit M10): ประกันสังคมส่วนนายจ้างคำนวณและเก็บต่อคน,
-- ตารางตั้งบัญชี GL ของรายการฝั่งนายจ้าง/เงินเดือนค้างจ่าย, อัตรานายจ้างเป็น config (ว่าง = เท่าลูกจ้าง)
-- รันซ้ำได้
IF OBJECT_ID('Pay_GLAccountMapping', 'U') IS NULL
BEGIN
    CREATE TABLE Pay_GLAccountMapping (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pay_GLAccountMapping PRIMARY KEY,
        CompanyId nvarchar(50) NOT NULL,
        MappingKey nvarchar(50) NOT NULL,
        DisplayName nvarchar(200) NOT NULL,
        DebitAccountCode nvarchar(50) NULL,
        CreditAccountCode nvarchar(50) NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Pay_GLAccountMapping_IsActive DEFAULT 1,
        ModifiedDate datetime2 NOT NULL CONSTRAINT DF_Pay_GLAccountMapping_ModifiedDate DEFAULT SYSDATETIME(),
        ModifiedByUserId bigint NULL
    );
    CREATE UNIQUE INDEX UX_Pay_GLAccountMapping_Company_Key ON Pay_GLAccountMapping (CompanyId, MappingKey);
END;
GO
IF COL_LENGTH('Pay_PayrollEmployee', 'SocialSecurityCompanyAmount') IS NULL
BEGIN
    ALTER TABLE Pay_PayrollEmployee ADD SocialSecurityCompanyAmount decimal(15,2) NOT NULL
        CONSTRAINT DF_Pay_PayrollEmployee_SsoCompany DEFAULT 0;
END;
GO
IF COL_LENGTH('Hrucfsecurity', 'EmployerPercenSecurity') IS NULL
BEGIN
    ALTER TABLE Hrucfsecurity ADD EmployerPercenSecurity decimal(18,2) NULL;   -- NULL = ใช้อัตราเดียวกับลูกจ้าง
END;
GO
-- ทะเบียน Super Master: ค่าที่เพิ่มในรอบนี้
IF OBJECT_ID('Sys_ProtectedSetting', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Sys_ProtectedSetting WHERE SettingKey = N'PAY.SSO.EMPLOYER_RATE')
        INSERT INTO Sys_ProtectedSetting (SettingKey, Area, TableName, FieldName, DisplayName, Reason, PageRoute, EditableBy, SortOrder, IsActive)
        VALUES (N'PAY.SSO.EMPLOYER_RATE', N'Payroll', N'Hrucfsecurity', N'EmployerPercenSecurity', N'อัตราประกันสังคมส่วนนายจ้าง (%)', N'ค่าตามประกาศสำนักงานประกันสังคม ว่าง = เท่าฝั่งลูกจ้าง', N'/hrucfsecurities', 0, 42, 1);
    IF NOT EXISTS (SELECT 1 FROM Sys_ProtectedSetting WHERE SettingKey = N'PAY.GL.MAPPING')
        INSERT INTO Sys_ProtectedSetting (SettingKey, Area, TableName, FieldName, DisplayName, Reason, PageRoute, EditableBy, SortOrder, IsActive)
        VALUES (N'PAY.GL.MAPPING', N'Payroll', N'Pay_GLAccountMapping', N'*', N'บัญชี GL ของรายการฝั่งนายจ้าง/เงินเดือนค้างจ่าย', N'ผิดแล้วไฟล์บัญชีลงผิดบัญชีทั้งงวด', N'/pay/admin/gl-account-mapping', 0, 70, 1);
END;
GO
-- เมนูในกลุ่มเงินเดือน (grant ตามแถวจัดการงวด)
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/pay/admin/gl-account-mapping' AND uppermenucode = 'GRP_PAYROLL')
BEGIN
    DECLARE @grp bigint = (SELECT TOP 1 menugroupid FROM sc_menu WHERE uppermenucode = 'GRP_PAYROLL');
    DECLARE @id  bigint = (SELECT ISNULL(MAX(menuid), 0) + 1 FROM sc_menu);
    DECLARE @like bigint = (SELECT TOP 1 menuid FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/pay/admin/periods' AND uppermenucode = 'GRP_PAYROLL');
    SET IDENTITY_INSERT sc_menu ON;
    INSERT INTO sc_menu (menuid, menuname, menuname_en, menulevel, isfinal, menuorder, menucode, uppermenucode, isshow, url, isactive, menugroupid, moddate, modby)
    VALUES (@id, N'บัญชี GL ฝั่งนายจ้าง', 'GL Account Mapping', 2, 1, 102, 'PAY_ADMIN', 'GRP_PAYROLL', 1, '/pay/admin/gl-account-mapping', 1, ISNULL(@grp, 1), GETDATE(), 'ScMenuNavSeeder');
    SET IDENTITY_INSERT sc_menu OFF;
    INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
    SELECT DISTINCT rm.roleid, @id, 1, GETDATE() FROM sc_role_menu rm
    WHERE rm.menuid = @like AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = rm.roleid AND x.menuid = @id);
END;
GO
