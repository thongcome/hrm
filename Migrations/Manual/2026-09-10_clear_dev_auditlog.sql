-- ============================================================================
--  ล้าง AuditLog ของเครื่อง dev
--
--  CEO, 10 ก.ย. 2569: "AuditLog -> ลบไปเลย เพราะเป็น dev mode"
--
--  ตอนลบ workflow สาธิต ผมเว้น AuditLog ไว้โดยอ้างกฎเก็บ ≥90 วันตาม
--  พ.ร.บ.คอมพิวเตอร์ CEO ยืนยันกลับมาว่าฐานนี้เป็นเครื่องพัฒนา ไม่ใช่ระบบจริง
--  จึงล้างได้ — บันทึกไว้ตรงนี้ว่าเป็นการตัดสินใจของเจ้าของระบบ ไม่ใช่การ
--  ตีความเอง
--
--  ข้อมูลที่อยู่ในนั้นสะท้อนว่าเป็น dev จริง: 234,905 แถว โดยกว่า 187,000 แถว
--  มาจากการ seed ชุดทดสอบ 7,000 คน (Pay_PayrollLineItem / Pay_PayrollAnomaly /
--  Pay_PayrollEmployee) และ 28,360 แถวเป็นการ audit ตาราง audit ของ payroll เอง
--  ซึ่งเป็นพฤติกรรมที่แก้ไปแล้วใน f409027 แต่แถวเก่ายังค้างอยู่
--  ส่วนที่เกี่ยวกับ workflow จริง ๆ มีราว 830 แถวเท่านั้น
--
--  ขอบเขต: ล้างข้อมูลเท่านั้น ไม่แตะโค้ดที่เขียน audit
--  HRMContext.Audit.cs ยังบันทึกทุก Create/Update/Delete เหมือนเดิม และหน้าที่
--  แสดงข้อมูลอ่อนไหวยังเรียก LogAccessAsync เหมือนเดิม กฎ ≥90 วันยังมีผลกับ
--  ระบบจริง — ห้ามเอาสคริปต์นี้ไปตั้งเป็นงานล้างประจำบนเครื่อง production
--
--  รันซ้ำได้
-- ============================================================================

SET NOCOUNT ON;
GO

PRINT '--- ก่อนล้าง ---';
SELECT COUNT(*) AS rows_before FROM AuditLog;
GO

-- TRUNCATE เร็วกว่าและคืนค่า identity ให้เริ่มใหม่ ใช้ได้เมื่อไม่มี FK ชี้เข้ามา
-- ถ้ามีเมื่อไหร่ค่อยถอยไป DELETE ซึ่งช้ากว่าแต่ทำงานได้เหมือนกัน
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID('AuditLog'))
BEGIN
    TRUNCATE TABLE AuditLog;
    PRINT 'ล้างด้วย TRUNCATE';
END
ELSE
BEGIN
    DELETE FROM AuditLog;
    PRINT 'มี FK ชี้เข้ามา จึงล้างด้วย DELETE';
END
GO

PRINT '--- หลังล้าง ---';
SELECT COUNT(*) AS rows_after FROM AuditLog;
GO
