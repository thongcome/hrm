-- 11 ก.ย. 2569 — รอบจ่ายเงินเดือนเป็น config (audit M8) + ฐานกองทุนสำรองเลี้ยงชีพตามธงในแค็ตตาล็อก (audit M2)
--
-- Pay_PaySchedule: จ่ายกี่งวดต่อเดือน ต่อบริษัท ต่อกลุ่มพนักงาน (0 รายเดือน / 1 รายวัน / 2 ทุกคน) มีผลตั้งแต่-ถึง
-- Pay_EmployeePayScheduleOverride: ทับรายคน
-- Pay_PayItemType.IsProvidentFundWageBase: รายการไหนนับเป็น "ค่าจ้าง" ฐานกองทุน (ค่าเริ่มต้น: BASE เท่านั้น
--   — เดิมคิดจากเงินได้รวม OT/สวัสดิการ ซึ่งหักเงินสะสมพนักงานและสมทบบริษัทเกินในเดือนที่มี OT)
-- รันซ้ำได้
IF OBJECT_ID('Pay_PaySchedule', 'U') IS NULL
BEGIN
    CREATE TABLE Pay_PaySchedule (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pay_PaySchedule PRIMARY KEY,
        CompanyId nvarchar(50) NOT NULL,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(100) NOT NULL,
        PeriodsPerMonth int NOT NULL CONSTRAINT DF_Pay_PaySchedule_PeriodsPerMonth DEFAULT 1,
        SecondTermStartDay int NOT NULL CONSTRAINT DF_Pay_PaySchedule_SecondTermStartDay DEFAULT 16,
        AppliesTo int NOT NULL CONSTRAINT DF_Pay_PaySchedule_AppliesTo DEFAULT 2,
        EffectiveFrom date NOT NULL,
        EffectiveTo date NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Pay_PaySchedule_IsActive DEFAULT 1,
        Note nvarchar(500) NULL
    );
    CREATE INDEX IX_Pay_PaySchedule_CompanyId_EffectiveFrom ON Pay_PaySchedule (CompanyId, EffectiveFrom);
END;
GO
IF OBJECT_ID('Pay_EmployeePayScheduleOverride', 'U') IS NULL
BEGIN
    CREATE TABLE Pay_EmployeePayScheduleOverride (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pay_EmployeePayScheduleOverride PRIMARY KEY,
        HremployeeId bigint NOT NULL,
        PayScheduleId bigint NOT NULL,
        EffectiveFrom date NOT NULL,
        EffectiveTo date NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Pay_EmployeePayScheduleOverride_IsActive DEFAULT 1,
        Note nvarchar(500) NULL,
        EnteredByUserId bigint NOT NULL,
        EnteredDate datetime2 NOT NULL CONSTRAINT DF_Pay_EmployeePayScheduleOverride_EnteredDate DEFAULT SYSDATETIME(),
        CONSTRAINT FK_Pay_EmployeePayScheduleOverride_Hremployee FOREIGN KEY (HremployeeId) REFERENCES HREMPLOYEE (id),
        CONSTRAINT FK_Pay_EmployeePayScheduleOverride_Pay_PaySchedule FOREIGN KEY (PayScheduleId) REFERENCES Pay_PaySchedule (Id)
    );
    CREATE INDEX IX_Pay_EmployeePayScheduleOverride_HremployeeId ON Pay_EmployeePayScheduleOverride (HremployeeId);
END;
GO
IF COL_LENGTH('Pay_PayItemType', 'IsProvidentFundWageBase') IS NULL
BEGIN
    ALTER TABLE Pay_PayItemType ADD IsProvidentFundWageBase bit NOT NULL CONSTRAINT DF_Pay_PayItemType_IsProvidentFundWageBase DEFAULT 0;
END;
GO
UPDATE Pay_PayItemType SET IsProvidentFundWageBase = 1 WHERE Code = 'BASE';
GO
