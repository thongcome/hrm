-- 12 ก.ย. 2569 — ไฟล์ e-Filing (ภ.ง.ด.1/1ก สำหรับ RD Prep, สปส.1-10 สำหรับ e-Service ประกันสังคม)
--   หัวไฟล์ สปส.1-10 ต้องมีเลขที่บัญชีนายจ้าง (10 หลัก) และลำดับที่สาขา (6 หลัก) — เก็บในตั้งค่าบริษัท/สลิป
-- รันซ้ำได้
SET NOCOUNT ON;
GO
IF COL_LENGTH('Pay_PayslipSettings', 'SsoEmployerAccountNo') IS NULL
    ALTER TABLE Pay_PayslipSettings ADD SsoEmployerAccountNo nvarchar(50) NULL;
IF COL_LENGTH('Pay_PayslipSettings', 'SsoBranchSeq') IS NULL
    ALTER TABLE Pay_PayslipSettings ADD SsoBranchSeq nvarchar(50) NULL;
GO
PRINT '--- Pay_PayslipSettings SSO employer account columns ready';
GO
