-- 12 ก.ย. 2569 — สองข้อจากเทสเงินเดือนทั้งปี 2568 (CEO: "ทำให้ครบเลย ทำไปเรื่อยๆ")
--   1) Pay_AdhocPayItem.TargetRunType / TargetTermNo: รายการเฉพาะกิจระบุได้ว่าจ่ายในรอบปกติหรือรอบโบนัส (และงวดที่ของเดือน)
--      เดิมรอบปกติหยิบทุกรายการของงวดไปก่อน โบนัสที่อนุมัติไว้ก่อนจึงไม่เหลือให้รอบโบนัส
--   2) Pay_BankFileExportBatch.DeltaOfPayrollRunId / Remark: ไฟล์ธนาคารของรอบปรับปรุงเป็น "ส่วนต่าง" จากที่โอนไปแล้ว
-- รันซ้ำได้
SET NOCOUNT ON;
GO
IF COL_LENGTH('Pay_AdhocPayItem', 'TargetRunType') IS NULL
    ALTER TABLE Pay_AdhocPayItem ADD TargetRunType int NOT NULL CONSTRAINT DF_Pay_AdhocPayItem_TargetRunType DEFAULT 0;
IF COL_LENGTH('Pay_AdhocPayItem', 'TargetTermNo') IS NULL
    ALTER TABLE Pay_AdhocPayItem ADD TargetTermNo int NULL;
IF COL_LENGTH('Pay_BankFileExportBatch', 'DeltaOfPayrollRunId') IS NULL
    ALTER TABLE Pay_BankFileExportBatch ADD DeltaOfPayrollRunId bigint NULL;
IF COL_LENGTH('Pay_BankFileExportBatch', 'Remark') IS NULL
    ALTER TABLE Pay_BankFileExportBatch ADD Remark nvarchar(2000) NULL;
GO
PRINT '--- adhoc target run + bank delta columns ready';
GO
