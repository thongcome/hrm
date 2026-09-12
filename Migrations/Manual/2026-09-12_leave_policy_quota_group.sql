-- 12 ก.ย. 2569 — QuotaGroupCode บน Lve_LeavePolicy: ให้ประเภทลาหลายแบบใช้โควต้าร่วมกันได้
--   (เช่น ลากิจ + ลาป่วยเกินสิทธิ ใช้วันรวมกัน 6 วัน/ปี) — ตั้งค่าที่ /leave-requests/policy
-- รันซ้ำได้
SET NOCOUNT ON;
GO
IF COL_LENGTH('Lve_LeavePolicy', 'QuotaGroupCode') IS NULL
    ALTER TABLE Lve_LeavePolicy ADD QuotaGroupCode nvarchar(30) NULL;
GO
PRINT '--- Lve_LeavePolicy.QuotaGroupCode ready';
GO
