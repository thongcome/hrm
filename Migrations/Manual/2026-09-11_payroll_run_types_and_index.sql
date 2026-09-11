SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
-- 11 ก.ย. 2569 — รอบเงินเดือน: ชนิด "กลับรายการ" แยกจากรอบปรับปรุง + index ไม่นับรอบที่ยกเลิก
--
-- (1) รอบกลับรายการเดิมถูกเก็บเป็น RunType = 1 (Adjustment) + Remark 'REVERSAL...'
--     ตอนนี้มี PayrollRunType.Reversal = 3 เพื่อให้ engine ห้าม "คำนวณใหม่" (audit C2)
--     และไม่ชน unique index กับรอบปรับปรุงของงวดเดียวกัน (audit H1)
-- (2) unique (CompanyId, PayrollPeriod, RunType) กรอง Status <> 9 (Cancelled) —
--     เดิมรอบที่ยกเลิกล็อกงวดนั้นถาวร
-- รันซ้ำได้
UPDATE Pay_PayrollRun
SET RunType = 3
WHERE RunType = 1 AND AdjustmentOfRunId IS NOT NULL AND Remark LIKE 'REVERSAL%';

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType'
           AND object_id = OBJECT_ID('Pay_PayrollRun') AND has_filter = 0)
    DROP INDEX IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType ON Pay_PayrollRun;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType'
               AND object_id = OBJECT_ID('Pay_PayrollRun'))
    CREATE UNIQUE INDEX IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType
        ON Pay_PayrollRun (CompanyId, PayrollPeriod, RunType)
        WHERE [Status] <> 9;
