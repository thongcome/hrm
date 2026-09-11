-- 12 ก.ย. 2569 — งวดที่ของเดือน (TermNo) บน Pay_PayrollRun (พบจากเทสเงินเดือนทั้งปี 2568):
--   บริษัทที่ตั้งปฏิทินจ่าย 2 งวด/เดือน (Pay_PaySchedule.PeriodsPerMonth = 2) สร้างรอบปกติงวดที่ 2 ไม่ได้
--   เพราะ unique index ล็อก (บริษัท, งวด YYYYMM, ชนิดรอบ) — เพิ่ม TermNo เข้า index; PayrollPeriod ยังเป็น YYYYMM
--   ทั้งสองงวด เพื่อให้ ภ.ง.ด.1 / เพดานประกันสังคมรายเดือน / ยอดสะสม รวมสองงวดเป็นเดือนเดียว
-- รันซ้ำได้
SET NOCOUNT ON;
GO
IF COL_LENGTH('Pay_PayrollRun', 'TermNo') IS NULL
    ALTER TABLE Pay_PayrollRun ADD TermNo int NOT NULL CONSTRAINT DF_Pay_PayrollRun_TermNo DEFAULT 1;
GO
IF EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = OBJECT_ID('Pay_PayrollRun') AND i.name = 'IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType'
           AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                           WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND c.name = 'TermNo'))
BEGIN
    DROP INDEX IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType ON Pay_PayrollRun;
    CREATE UNIQUE INDEX IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType
        ON Pay_PayrollRun (CompanyId, PayrollPeriod, TermNo, RunType) WHERE [Status] <> 9;
END;
GO
PRINT '--- Pay_PayrollRun.TermNo ready';
SELECT COUNT(*) AS runs, SUM(CASE WHEN TermNo = 2 THEN 1 ELSE 0 END) AS term2 FROM Pay_PayrollRun;
GO
