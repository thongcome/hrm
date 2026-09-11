-- 11 ก.ย. 2569 — เก็บ "เงินได้ที่ต้องเสียภาษี" ต่องวดไว้บนแถวพนักงาน (audit M3)
--
-- เดิม 50 ทวิ / ภ.ง.ด.1 ย้อนคำนวณจาก GrossEarnings − รายการเฉพาะกิจที่ไม่เสียภาษี ซึ่งยังรวม
-- สวัสดิการที่ไม่เสียภาษีและไม่ได้หักขาด/สายที่ engine หักออกจากฐานภาษีจริง ทำให้เอกสารพิมพ์
-- เงินได้สูงกว่าที่ถูกหักภาษี ตอนนี้ engine เขียนค่าที่ใช้จริงลง TaxableIncome ทุกครั้งที่คำนวณ
-- รันซ้ำได้: เพิ่มคอลัมน์ถ้ายังไม่มี แล้ว backfill แถวเก่าด้วยสูตรเดิม (ค่าที่ดีที่สุดที่มีสำหรับรอบที่ post แล้ว)
IF COL_LENGTH('Pay_PayrollEmployee', 'TaxableIncome') IS NULL
BEGIN
    ALTER TABLE Pay_PayrollEmployee ADD TaxableIncome decimal(15,2) NOT NULL CONSTRAINT DF_Pay_PayrollEmployee_TaxableIncome DEFAULT 0;
END;
GO

-- backfill เฉพาะแถวที่ยังเป็น 0 และมีเงินได้ (แถวที่ engine ใหม่เขียนแล้วจะไม่ถูกแตะ)
-- trigger กันแก้แถวของรอบที่ post แล้ว — ปิดชั่วคราวเฉพาะ backfill คอลัมน์ derived นี้ ไม่ได้แตะยอดเงิน
DISABLE TRIGGER trg_PayrollEmployee_Immutable ON Pay_PayrollEmployee;
UPDATE pe
SET TaxableIncome = pe.GrossEarnings - ISNULL(nt.NonTaxable, 0)
FROM Pay_PayrollEmployee pe
OUTER APPLY (
    SELECT SUM(li.Amount) AS NonTaxable
    FROM Pay_PayrollLineItem li
    JOIN Pay_AdhocPayItem a ON a.Id = li.SourceRefId
    WHERE li.PayrollEmployeeId = pe.Id AND li.SourceRefTable = 'Pay_AdhocPayItem' AND li.SignFlag > 0 AND a.IsTaxable = 0
) nt
WHERE pe.TaxableIncome = 0 AND pe.GrossEarnings <> 0;
ENABLE TRIGGER trg_PayrollEmployee_Immutable ON Pay_PayrollEmployee;
