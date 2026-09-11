-- 11 ก.ย. 2569 — ค่าจ้างรายวันนับวันทำงานจริง (audit M6) และอัตราต่อวันสูตรเดียว (audit M7)
--
-- Pay_AttendanceDeductionPolicy.ProrationMode (int): 0 = สัดส่วนตามจำนวนวันจริงของงวด (พฤติกรรมเดิม),
--   1 = เงินเดือน ÷ DaysPerMonthDivisor × วันที่ทำงาน (สูตรเดียวกับหักขาดงาน) — ค่าเริ่มต้น 1
-- DailyWageMode ค่าใหม่ 2 = วันทำงานของบริษัท (ไม่นับวันหยุดประจำสัปดาห์/วันหยุดบริษัท) — แถวเดิมที่ยังเป็น 0 (วันตามปฏิทิน)
--   ย้ายเป็น 2 เพราะ 0 จ่ายเกินให้พนักงานรายวัน (นับเสาร์-อาทิตย์) และ 0 ไม่เคยเป็นค่าที่ HR ตั้งใจเลือก (เป็น default เดิม)
-- รันซ้ำได้
IF COL_LENGTH('Pay_AttendanceDeductionPolicy', 'ProrationMode') IS NULL
BEGIN
    ALTER TABLE Pay_AttendanceDeductionPolicy ADD ProrationMode int NOT NULL CONSTRAINT DF_Pay_AttendanceDeductionPolicy_ProrationMode DEFAULT 1;
END;
GO
UPDATE Pay_AttendanceDeductionPolicy SET DailyWageMode = 2 WHERE DailyWageMode = 0;
GO
