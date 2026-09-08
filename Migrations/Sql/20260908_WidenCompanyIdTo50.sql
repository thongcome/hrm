/*
  Widen every string CompanyId / companyid column to nvarchar(50).

  CEO standing rule (8 ก.ย. 2569): "ไม่ต้องประหยัด size ใน field — ถ้าใส่แค่ 6
  ไม่พอ เดือดร้อนกว่าเผื่อไว้ 50". EmpNo was widened the same day; CompanyId is
  the same family (a soft-link business code used across ~81 tables) but could
  not be altered in place because 13 objects pin its type.

  Also makes companyid the same width as com_company.code (already nvarchar(50)),
  which is a precondition for ever adding a real FK between them.

  The 13 objects are dropped and recreated EXACTLY as they were — verified from
  sys.indexes beforehand: no included columns, no filters, no DESC keys, one
  clustered PK (Att_CompanySetting) and five UNIQUE indexes.

  Safe to re-run: the ALTER block only touches columns still shorter than 50,
  and the index drops/creates are guarded by existence checks.
*/
SET XACT_ABORT ON;
BEGIN TRAN;

------------------------------------------------------------------ 1) drop
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Att_CompanySetting')
    ALTER TABLE [dbo].[Att_CompanySetting] DROP CONSTRAINT [PK_Att_CompanySetting];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Eng_PointsLedger_CompanyId' AND object_id=OBJECT_ID('dbo.Eng_PointsLedger'))
    DROP INDEX [IX_Eng_PointsLedger_CompanyId] ON [dbo].[Eng_PointsLedger];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Eng_PointsRule_CompanyId' AND object_id=OBJECT_ID('dbo.Eng_PointsRule'))
    DROP INDEX [IX_Eng_PointsRule_CompanyId] ON [dbo].[Eng_PointsRule];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Eng_RedeemItem_CompanyId' AND object_id=OBJECT_ID('dbo.Eng_RedeemItem'))
    DROP INDEX [IX_Eng_RedeemItem_CompanyId] ON [dbo].[Eng_RedeemItem];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Eng_RedeemRequest_CompanyId' AND object_id=OBJECT_ID('dbo.Eng_RedeemRequest'))
    DROP INDEX [IX_Eng_RedeemRequest_CompanyId] ON [dbo].[Eng_RedeemRequest];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Lve_CompanySetting_CompanyId' AND object_id=OBJECT_ID('dbo.Lve_CompanySetting'))
    DROP INDEX [IX_Lve_CompanySetting_CompanyId] ON [dbo].[Lve_CompanySetting];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Pay_AttendanceDeductionPolicy_CompanyId' AND object_id=OBJECT_ID('dbo.Pay_AttendanceDeductionPolicy'))
    DROP INDEX [IX_Pay_AttendanceDeductionPolicy_CompanyId] ON [dbo].[Pay_AttendanceDeductionPolicy];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType' AND object_id=OBJECT_ID('dbo.Pay_PayrollRun'))
    DROP INDEX [IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType] ON [dbo].[Pay_PayrollRun];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Pay_PayslipSettings_CompanyId' AND object_id=OBJECT_ID('dbo.Pay_PayslipSettings'))
    DROP INDEX [IX_Pay_PayslipSettings_CompanyId] ON [dbo].[Pay_PayslipSettings];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Pos_GradeChangeHistory_CompanyId' AND object_id=OBJECT_ID('dbo.Pos_GradeChangeHistory'))
    DROP INDEX [IX_Pos_GradeChangeHistory_CompanyId] ON [dbo].[Pos_GradeChangeHistory];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Wel_BenefitTypes_CompanyId_Code' AND object_id=OBJECT_ID('dbo.Wel_BenefitTypes'))
    DROP INDEX [IX_Wel_BenefitTypes_CompanyId_Code] ON [dbo].[Wel_BenefitTypes];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Wel_Claim_CompanyId_HremployeeId_BenefitTypeId' AND object_id=OBJECT_ID('dbo.Wel_Claim'))
    DROP INDEX [IX_Wel_Claim_CompanyId_HremployeeId_BenefitTypeId] ON [dbo].[Wel_Claim];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Wel_Entitlements_CompanyId_BenefitTypeId_IsActive' AND object_id=OBJECT_ID('dbo.Wel_Entitlements'))
    DROP INDEX [IX_Wel_Entitlements_CompanyId_BenefitTypeId_IsActive] ON [dbo].[Wel_Entitlements];

------------------------------------------------------------------ 2) widen
DECLARE @sql nvarchar(max) = N'';
SELECT @sql = @sql + N'ALTER TABLE [dbo].[' + t.name + N'] ALTER COLUMN [' + c.name + N'] nvarchar(50) '
     + CASE WHEN c.is_nullable = 1 THEN N'NULL' ELSE N'NOT NULL' END + N';' + CHAR(10)
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE ty.name = 'nvarchar' AND c.name IN ('CompanyId','companyid')
  AND c.max_length/2 < 50 AND c.is_computed = 0;
EXEC sp_executesql @sql;

------------------------------------------------------------------ 3) recreate
ALTER TABLE [dbo].[Att_CompanySetting] ADD CONSTRAINT [PK_Att_CompanySetting] PRIMARY KEY CLUSTERED ([CompanyId]);
CREATE NONCLUSTERED INDEX [IX_Eng_PointsLedger_CompanyId] ON [dbo].[Eng_PointsLedger]([CompanyId]);
CREATE NONCLUSTERED INDEX [IX_Eng_PointsRule_CompanyId] ON [dbo].[Eng_PointsRule]([CompanyId]);
CREATE NONCLUSTERED INDEX [IX_Eng_RedeemItem_CompanyId] ON [dbo].[Eng_RedeemItem]([CompanyId]);
CREATE NONCLUSTERED INDEX [IX_Eng_RedeemRequest_CompanyId] ON [dbo].[Eng_RedeemRequest]([CompanyId]);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Lve_CompanySetting_CompanyId] ON [dbo].[Lve_CompanySetting]([CompanyId]);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Pay_AttendanceDeductionPolicy_CompanyId] ON [dbo].[Pay_AttendanceDeductionPolicy]([CompanyId]);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType] ON [dbo].[Pay_PayrollRun]([CompanyId],[PayrollPeriod],[RunType]);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Pay_PayslipSettings_CompanyId] ON [dbo].[Pay_PayslipSettings]([CompanyId]);
CREATE NONCLUSTERED INDEX [IX_Pos_GradeChangeHistory_CompanyId] ON [dbo].[Pos_GradeChangeHistory]([CompanyId]);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Wel_BenefitTypes_CompanyId_Code] ON [dbo].[Wel_BenefitTypes]([CompanyId],[Code]);
CREATE NONCLUSTERED INDEX [IX_Wel_Claim_CompanyId_HremployeeId_BenefitTypeId] ON [dbo].[Wel_Claim]([CompanyId],[HremployeeId],[BenefitTypeId]);
CREATE NONCLUSTERED INDEX [IX_Wel_Entitlements_CompanyId_BenefitTypeId_IsActive] ON [dbo].[Wel_Entitlements]([CompanyId],[BenefitTypeId],[IsActive]);

COMMIT;

------------------------------------------------------------------ 4) verify
SELECT 'columns still under 50' AS check_name, COUNT(*) AS should_be_zero
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE ty.name='nvarchar' AND c.name IN ('CompanyId','companyid') AND c.max_length/2 < 50;

SELECT 'indexes restored' AS check_name, COUNT(*) AS should_be_13
FROM sys.indexes i
WHERE i.name IN (
 'PK_Att_CompanySetting','IX_Eng_PointsLedger_CompanyId','IX_Eng_PointsRule_CompanyId',
 'IX_Eng_RedeemItem_CompanyId','IX_Eng_RedeemRequest_CompanyId','IX_Lve_CompanySetting_CompanyId',
 'IX_Pay_AttendanceDeductionPolicy_CompanyId','IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType',
 'IX_Pay_PayslipSettings_CompanyId','IX_Pos_GradeChangeHistory_CompanyId',
 'IX_Wel_BenefitTypes_CompanyId_Code','IX_Wel_Claim_CompanyId_HremployeeId_BenefitTypeId',
 'IX_Wel_Entitlements_CompanyId_BenefitTypeId_IsActive');
