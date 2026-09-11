-- 12 ก.ย. 2569 — รูปแบบไฟล์ธนาคารเป็น config ต่อบริษัท (Pay_BankFileFormat): แม่แบบหัว/บรรทัด/ท้าย ตาม spec ที่ธนาคารให้
--   ไม่มีแถวค่าเริ่มต้น = CSV กลางแบบเดิม · หน้า admin /pay/admin/bank-file-formats (gate PAY_ADMIN, AD.CRUDManage seed ตอน start)
-- รันซ้ำได้
SET NOCOUNT ON;
GO
IF OBJECT_ID('Pay_BankFileFormat') IS NULL
BEGIN
    CREATE TABLE Pay_BankFileFormat (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pay_BankFileFormat PRIMARY KEY,
        CompanyId nvarchar(50) NOT NULL,
        Code nvarchar(30) NOT NULL,
        Name nvarchar(200) NOT NULL,
        CompanyBankCode nvarchar(50) NULL,
        CompanyBranchCode nvarchar(50) NULL,
        CompanyAccountNo nvarchar(50) NULL,
        HeaderTemplate nvarchar(2000) NULL,
        LineTemplate nvarchar(2000) NOT NULL,
        TrailerTemplate nvarchar(2000) NULL,
        Encoding nvarchar(20) NOT NULL CONSTRAINT DF_Pay_BankFileFormat_Encoding DEFAULT 'UTF8BOM',
        LineEnding nvarchar(10) NOT NULL CONSTRAINT DF_Pay_BankFileFormat_LineEnding DEFAULT 'CRLF',
        FileNamePattern nvarchar(100) NULL,
        IsDefault bit NOT NULL CONSTRAINT DF_Pay_BankFileFormat_IsDefault DEFAULT 0,
        IsActive bit NOT NULL CONSTRAINT DF_Pay_BankFileFormat_IsActive DEFAULT 1,
        Note nvarchar(1000) NULL,
        ModifiedDate datetime2 NOT NULL CONSTRAINT DF_Pay_BankFileFormat_ModifiedDate DEFAULT SYSDATETIME(),
        ModifiedByUserId bigint NULL
    );
    CREATE UNIQUE INDEX IX_Pay_BankFileFormat_CompanyId_Code ON Pay_BankFileFormat (CompanyId, Code);
END;
GO
-- เมนูในกลุ่มเงินเดือน (ตัว seeder ก็เพิ่มให้ตอน start จาก ScMenuNavCatalog; ทำที่นี่ด้วยให้ครบในครั้งเดียว)
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(400)) = '/pay/admin/bank-file-formats')
BEGIN
    DECLARE @grp bigint = (SELECT TOP 1 menugroupid FROM sc_menu WHERE uppermenucode = 'GRP_PAYROLL');
    DECLARE @id  bigint = (SELECT ISNULL(MAX(menuid), 0) + 1 FROM sc_menu);
    SET IDENTITY_INSERT sc_menu ON;
    INSERT INTO sc_menu (menuid, menuname, menuname_en, menulevel, isfinal, menuorder, menucode, uppermenucode, isshow, url, isactive, menugroupid, moddate, modby)
    VALUES (@id, N'รูปแบบไฟล์ธนาคาร', 'Bank File Formats', 2, 1, 103, 'PAY_ADMIN', 'GRP_PAYROLL', 1, '/pay/admin/bank-file-formats', 1, ISNULL(@grp, 1), GETDATE(), 'ScMenuNavSeeder');
    SET IDENTITY_INSERT sc_menu OFF;
    INSERT INTO sc_role_menu (roleid, menuid, isactive, moddate)
    SELECT DISTINCT r.roleid, @id, 1, GETDATE() FROM sc_role r WHERE r.rolecode IN ('admin', 'HR', 'ACCOUNTING') AND r.isactive = 1
      AND NOT EXISTS (SELECT 1 FROM sc_role_menu x WHERE x.roleid = r.roleid AND x.menuid = @id);
END;
GO
PRINT '--- Pay_BankFileFormat ready';
GO
