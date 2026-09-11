-- 12 ก.ย. 2569 — ร่องรอยการเปลี่ยนรอบจ่าย (CEO: "ตั้งรอบนี่ต้องแก้ยากหน่อย" ข้อ 1–3: ล็อกแถวที่ใช้แล้ว,
-- ไปข้างหน้าเท่านั้น, บังคับเหตุผล) — กติกาอยู่ใน PayScheduleGuard ตารางนี้เก็บเหตุผล/ใคร/เมื่อไร/อะไรเปลี่ยน
-- รันซ้ำได้
IF OBJECT_ID('Pay_PayScheduleChangeLog', 'U') IS NULL
BEGIN
    CREATE TABLE Pay_PayScheduleChangeLog (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pay_PayScheduleChangeLog PRIMARY KEY,
        CompanyId nvarchar(50) NOT NULL,
        PayScheduleId bigint NULL,
        OverrideId bigint NULL,
        Action nvarchar(30) NOT NULL,
        Reason nvarchar(500) NOT NULL,
        Detail nvarchar(1000) NULL,
        ChangedByUserId bigint NOT NULL,
        ChangedDate datetime2 NOT NULL CONSTRAINT DF_Pay_PayScheduleChangeLog_ChangedDate DEFAULT SYSDATETIME()
    );
    CREATE INDEX IX_Pay_PayScheduleChangeLog_Company_Date ON Pay_PayScheduleChangeLog (CompanyId, ChangedDate DESC);
END;
GO
